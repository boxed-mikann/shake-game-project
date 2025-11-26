# 実装仕様書: ハイスコアシステム & エフェクトシステム

## 📋 実装概要

2つの新機能を実装します：
1. **ハイスコアシステム** - PlayerPrefsによる永続化、UI表示、新記録強調
2. **エフェクトシステム** - Object Poolによる音符破棄エフェクト

---

## 🎯 Phase 1: ハイスコアシステム

### 1-1. GameConstants.cs への追加

**ファイル**: `Assets/Scripts/Managers/GameConstants.cs`

```csharp
// ハイスコア関連
public const string HIGH_SCORE_KEY = "HighScore";
```

### 1-2. HighScoreManager.cs （新規作成）

**ファイル**: `Assets/Scripts/Managers/HighScoreManager.cs`

**責務**: ハイスコアの保存・読み込み・更新

**実装内容**:
```csharp
using UnityEngine;
using UnityEngine.Events;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }
    public static UnityEvent<int> OnHighScoreUpdated = new UnityEvent<int>();
    
    private int _currentHighScore = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _currentHighScore = PlayerPrefs.GetInt(GameConstants.HIGH_SCORE_KEY, 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        GameManager.OnGameOver.AddListener(CheckAndUpdateHighScore);
    }
    
    void OnDisable()
    {
        GameManager.OnGameOver.RemoveListener(CheckAndUpdateHighScore);
    }
    
    void CheckAndUpdateHighScore()
    {
        int currentScore = ScoreManager.Instance.GetScore();
        
        if (currentScore > _currentHighScore)
        {
            _currentHighScore = currentScore;
            PlayerPrefs.SetInt(GameConstants.HIGH_SCORE_KEY, _currentHighScore);
            PlayerPrefs.Save();
            
            OnHighScoreUpdated.Invoke(_currentHighScore);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[HighScoreManager] New high score: {_currentHighScore}");
        }
    }
    
    public int GetHighScore()
    {
        return _currentHighScore;
    }
    
    public bool IsNewHighScore(int score)
    {
        return score > _currentHighScore;
    }
    
#if UNITY_EDITOR
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(GameConstants.HIGH_SCORE_KEY);
        _currentHighScore = 0;
        Debug.Log("[HighScoreManager] High score reset");
    }
#endif
}
```

### 1-3. HighScoreDisplay.cs （新規作成）

**ファイル**: `Assets/Scripts/UI/HighScoreDisplay.cs`

**責務**: タイトル画面・ゲーム中のハイスコア表示

**実装内容**:
```csharp
using UnityEngine;
using TMPro;
using System.Text;

public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private string _prefix = "High Score: ";
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        UpdateDisplay(HighScoreManager.Instance.GetHighScore());
        HighScoreManager.OnHighScoreUpdated.AddListener(UpdateDisplay);
    }
    
    void OnDestroy()
    {
        HighScoreManager.OnHighScoreUpdated.RemoveListener(UpdateDisplay);
    }
    
    void UpdateDisplay(int highScore)
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);
        _stringBuilder.Append(highScore);
        _highScoreText.text = _stringBuilder.ToString();
    }
}
```

**Inspector設定**:
- `_highScoreText`: TextMeshProUGUI コンポーネントを参照
- `_prefix`: "High Score: " （デフォルト）

### 1-4. ResultScoreDisplay.cs （既存修正）

**ファイル**: `Assets/Scripts/UI/ResultScoreDisplay.cs`

**追加内容**: 新記録時の強調表示

**追加フィールド**:
```csharp
[Header("New Record Display")]
[SerializeField] private Color _highlightColor = Color.yellow;
[SerializeField] private TextMeshProUGUI _newRecordText;  // オプション
```

**OnGameOver() メソッドに追加**:
```csharp
private void OnGameOver()
{
    // ... 既存のスコア表示処理 ...
    
    int finalScore = ScoreManager.Instance.GetScore();
    
    // 新記録チェック
    if (HighScoreManager.Instance.IsNewHighScore(finalScore))
    {
        ShowNewRecordEffect();
    }
}

private void ShowNewRecordEffect()
{
    // 色変更
    if (_highlightColor != Color.clear)
        _finalScoreText.color = _highlightColor;
    
    // 追加テキスト表示（オプション）
    if (_newRecordText != null)
        _newRecordText.gameObject.SetActive(true);
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[ResultScoreDisplay] New record displayed!");
}
```

### 1-5. シーン設定

1. **HighScoreManager**:
   - 空のGameObjectを作成し、HighScoreManagerコンポーネントをアタッチ
   - DontDestroyOnLoadで永続化されるため、どのシーンにも配置可能

2. **HighScoreDisplay**:
   - タイトル画面またはゲーム画面のUIに配置
   - TextMeshProUGUIを作成し、HighScoreDisplayにアタッチ

3. **ResultScoreDisplay**:
   - 既存のResultScoreDisplayに新しいフィールドを追加
   - オプションで「NEW RECORD!」用のTextMeshProUGUIを作成

---

## 🎨 Phase 2: エフェクトシステム

### 2-1. GameConstants.cs への追加

**ファイル**: `Assets/Scripts/Managers/GameConstants.cs`

```csharp
// エフェクトプール関連
public const int EFFECT_POOL_INITIAL_SIZE = 50;
```

### 2-2. EffectPool.cs （新規作成）

**ファイル**: `Assets/Scripts/Gameplay/EffectPool.cs`

**責務**: エフェクトのObject Pool管理

**実装内容**:
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CartoonFX;

public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }
    
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = GameConstants.EFFECT_POOL_INITIAL_SIZE;
    
    private List<GameObject> _allEffects = new List<GameObject>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject effect = Instantiate(effectPrefab, poolContainer);
            
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null)
            {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
            }
            
            effect.SetActive(false);
            _allEffects.Add(effect);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Initialized with {initialPoolSize} effects");
    }
    
    public void PlayEffect(Vector3 position, Quaternion rotation)
    {
        GameObject effect = _allEffects.Find(e => !e.activeInHierarchy);
        
        if (effect == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning("[EffectPool] Pool exhausted, creating new effect");
            
            effect = Instantiate(effectPrefab, poolContainer);
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null)
            {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
            }
            _allEffects.Add(effect);
        }
        
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        
        var cfxr = effect.GetComponent<CFXR_Effect>();
        if (cfxr != null)
        {
            cfxr.ResetState();
        }
        
        effect.SetActive(true);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Effect played at {position}");
    }
}
```

**重要**: 
- `using CartoonFX;` を忘れずに追加
- CFXR_Effectの `clearBehavior` を `Disable` に設定することで、エフェクト終了時に自動で `SetActive(false)` される
- プール側は再生時に非アクティブなエフェクトを探すだけでOK

### 2-3. NoteShakeHandler.cs （既存修正）

**ファイル**: `Assets/Scripts/Handlers/NoteShakeHandler.cs`

**修正内容**: エフェクト再生処理を追加

**HandleShake() メソッドの修正**:
```csharp
public void HandleShake(string data, double timestamp)
{
    // 1. 効果音
    if (AudioManager.Instance != null)
        AudioManager.Instance.PlaySFX("hit");

    // 2. 最古Note取得
    if (NoteManager.Instance == null)
    {
        Debug.LogWarning("[NoteShakeHandler] NoteManager instance not found!");
        return;
    }
    
    Note oldest = NoteManager.Instance.GetOldestNote();
    if (oldest == null)
    {
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NoteShakeHandler] No notes to destroy");
        return;
    }
    
    // 3. 位置を記録（破棄前に取得）
    Vector3 notePosition = oldest.transform.position;
    
    // 4. 最古Note破棄
    NoteManager.Instance.DestroyOldestNote();
    
    // 5. エフェクト再生（新規追加）
    if (EffectPool.Instance != null)
        EffectPool.Instance.PlayEffect(notePosition, Quaternion.identity);
    
    // 6. スコア加算
    if (ScoreManager.Instance != null)
        ScoreManager.Instance.AddScore(_scoreValue);
    
    if (GameConstants.DEBUG_MODE)
        Debug.Log($"[NoteShakeHandler] Note destroyed with effect, score +{_scoreValue}");
}
```

**重要**: 
- 音符破棄前に位置を記録する
- エフェクト再生はNull安全に実行

### 2-4. エフェクトPrefab設定

**推奨Prefab**: `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab`

**必須設定手順**:
1. Prefabを選択（またはシーンに配置）
2. `CFXR_Effect` コンポーネントを確認
3. **`Clear Behavior` を `Disable` に変更**（デフォルトは `Destroy`）
4. Prefabを保存

**確認事項**:
- `Play On Awake`: 1（有効）← 既に設定済み
- `Looping`: 0（無効）← 既に設定済み
- Duration: 0.5～1.0秒程度 ← 既に設定済み

### 2-5. シーン設定

1. **EffectPool GameObject**:
   - 空のGameObjectを作成（名前: "EffectPool"）
   - EffectPoolコンポーネントをアタッチ
   
2. **Inspector設定**:
   - `Effect Prefab`: CFXR Magic Poof Prefabを参照
   - `Pool Container`: 自分自身のTransformを参照（エフェクトの親になる）
   - `Initial Pool Size`: 50（デフォルト）

3. **NoteShakeHandler**:
   - 既存のGameObjectに配置されているため、コードのみ修正

---

## 📋 実装チェックリスト

### Phase 1: ハイスコア
- [ ] GameConstants.csに`HIGH_SCORE_KEY`を追加
- [ ] HighScoreManager.csを作成
- [ ] HighScoreDisplay.csを作成
- [ ] ResultScoreDisplay.csを修正（新記録表示）
- [ ] シーンにHighScoreManagerを配置
- [ ] UIにHighScoreDisplayを配置・設定
- [ ] 動作テスト（新記録達成、PlayerPrefs保存確認）

### Phase 2: エフェクト
- [ ] GameConstants.csに`EFFECT_POOL_INITIAL_SIZE`を追加
- [ ] EffectPool.csを作成（`using CartoonFX;`を含む）
- [ ] NoteShakeHandler.csを修正（エフェクト再生追加）
- [ ] CFXR Magic Poof Prefabの`clearBehavior`を`Disable`に変更
- [ ] シーンにEffectPoolを配置・設定
- [ ] 動作テスト（エフェクト再生、プール再利用、60fps維持確認）

---

## 🔧 デバッグ・テスト

### ハイスコアシステム
1. **初回起動**: ハイスコア0表示を確認
2. **プレイ後**: スコアが保存されているか確認（PlayerPrefs）
3. **新記録**: 新記録時に強調表示されるか確認
4. **リセット**: HighScoreManagerの`[ContextMenu]`でリセット可能

### エフェクトシステム
1. **初回再生**: エフェクトが正しい位置に表示されるか
2. **連続再生**: 100回連続シェイクで60fps維持を確認
3. **プール監視**: Inspectorで`_allEffects`のサイズを監視
4. **自動Disable**: エフェクト終了後にSetActive(false)になるか確認

---

## ⚠️ 重要な注意事項

### CFXR_Effect について
- **必ずPrefabの`clearBehavior`を`Disable`に設定**（これがないとプールが機能しない）
- エフェクト終了時、CFXR_Effectが自動で`SetActive(false)`を実行する（20フレームごとにチェック）
- プール側は返却処理不要、`List.Find`で非アクティブを探すだけ
- `ResetState()`は再利用時に必ず呼び出す（前回の状態をクリア）

### パフォーマンス
- プールサイズ50で通常は十分（不足時は自動拡張）
- エフェクト再生コスト: < 1ms
- `List.Find`のコスト: O(n)だが通常は最初の数個で見つかる

### Null安全性
- すべてのManagerアクセスでNull チェック実施済み
- 音符がない場合も安全に処理

---

## 📝 完成後の動作

1. **ゲーム起動**: タイトル画面にハイスコア表示
2. **ゲームプレイ**: 音符破棄時にエフェクト表示
3. **ゲーム終了**: 新記録なら強調表示、PlayerPrefsに保存
4. **次回起動**: 前回のハイスコアが表示される
