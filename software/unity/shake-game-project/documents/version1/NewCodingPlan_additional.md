## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## ✅ 微小修正項目（完了 - 2025-11-19）
- ~~スライダは減っていくようにする。フェーズの種類によって色を変える。~~ → **完了**: 進行度を1→0に反転、フェーズごとに色分け（青/オレンジ/赤）
- ~~音符の生成範囲を画面内に。~~ → **完了**: カメラのorthographicSizeから動的計算、画面サイズの90%以内に生成

---

## 重要検討項目
1. ランキング、ハイスコア、最高記録表示など
2. 音符がはじけるときのエフェクト
  - エフェクトアセット(JMOAssets)をアセットストアから持ってきた。
  - CFXR Magic Poofを使用する。
  - これもpoolを利用して低遅延で処理したい。

## 足りない機能・検討項目

余裕が出来たら追加する。

- **エラーハンドリング/ロギングシステム** - ログ出力・デバッグ用ロギングマネージャー（コンソール出力、ファイル保存等）
- **設定管理** - ゲーム難度、ポート番号、キー設定などをJSONまたはScriptableObjectで管理
- **リソース管理・プリロード** - ゲーム開始時にAudioClip、Sprite等を全てメモリ上に確保するPreloaderマネージャー
- **パフォーマンス監視** - フレームレート、メモリ使用量の表示・監視機構（デバッグ用UI）
- **セーブ・ロード機構** - ハイスコア、プレイ履歴等の永続化（PlayerPrefs or ファイルIO）
- **ネットワーク同期（将来対応）** - オンラインランキング、マルチプレイ検討時の基盤設計
- **入力イベント検証** - 受け取ったシェイクデータ（文字列）のバリデーション・パース機能
- **タイミング同期の微調整** - オーディオDSP時刻とゲーム時間のズレ吸収メカニズム
- **ポーズ/ポーズ解除機能** - GameManager側でポーズ状態を持ち、全マネージャーが購読
- **トランジション効果** - フェーズ間・画面間の切り替えアニメーション統一管理（TransitionManager）

- **もっとラグを少なくしたい**


## コード外修正項目
- フェーズ表示の文字化け

---

## 🎯 重要検討項目の実装計画（2025-11-20）

### 1. ランキング、ハイスコア、最高記録表示システム

#### 概要
- **目的**: プレイヤーのモチベーション向上、やり込み要素の提供
- **アーキテクチャ原則**: CodeArchitecture.mdに則った疎結合設計、イベント駆動、永続化

#### 実装方針

##### 1-1. データ永続化層（新規クラス）
**HighScoreManager.cs** - `Assets/Scripts/Managers/HighScoreManager.cs`
- **責務**: ハイスコアの保存・読み込み・更新管理
- **機能**:
  - `SaveHighScore(int score)` - 現在スコアがハイスコアを超えた場合のみ更新
  - `GetHighScore()` - ハイスコア取得
  - `IsNewHighScore(int score)` - 現在スコアが新記録かチェック
  - PlayerPrefs使用（`PlayerPrefs.SetInt(GameConstants.HIGH_SCORE_KEY, score)`）
  - 将来的にJSON/ファイル保存に拡張可能な設計
- **実装方式**: シングルトンパターン
- **イベント**:
  - `public static UnityEvent<int> OnHighScoreUpdated;` - 新記録達成時に発行（新ハイスコアを引数）
- **イベント購読**:
  - `GameManager.OnGameOver` → 現在スコアをチェック、必要なら更新
  - ⚠️ **重要**: `OnGameOver`時点ではまだScoreManagerがリセットされていないため、スコア取得が可能
  - ScoreManagerは`OnGameStart`と`OnShowTitle`でリセットされる（CodeArchitecture.md参照）
- **データ構造**:
  ```csharp
  private int _currentHighScore = 0;  // メモリ上のキャッシュ
  ```
- **デバッグ機能**:
  ```csharp
  #if UNITY_EDITOR
  [ContextMenu("Reset High Score")]
  public void ResetHighScore() {
      PlayerPrefs.DeleteKey(GameConstants.HIGH_SCORE_KEY);
      _currentHighScore = 0;
      Debug.Log("[HighScoreManager] High score reset");
  }
  #endif
  ```
- **実装例**:
  ```csharp
  void Awake() {
      // Singletonセットアップ...
      // PlayerPrefsから読み込み
      _currentHighScore = PlayerPrefs.GetInt(GameConstants.HIGH_SCORE_KEY, 0);
  }
  
  void OnEnable() {
      GameManager.OnGameOver.AddListener(CheckAndUpdateHighScore);
  }
  
  void CheckAndUpdateHighScore() {
      int currentScore = ScoreManager.Instance.GetScore();
      
      if (currentScore > _currentHighScore) {
          _currentHighScore = currentScore;
          PlayerPrefs.SetInt(GameConstants.HIGH_SCORE_KEY, _currentHighScore);
          PlayerPrefs.Save();  // 即座に保存
          
          OnHighScoreUpdated.Invoke(_currentHighScore);
          
          if (GameConstants.DEBUG_MODE)
              Debug.Log($"[HighScoreManager] New high score: {_currentHighScore}");
      }
  }
  ```

##### 1-2. UI表示層（新規・既存修正）
**HighScoreDisplay.cs** - `Assets/Scripts/UI/HighScoreDisplay.cs`（新規）
- **責務**: タイトル画面・ゲーム中にハイスコア表示
- **機能**:
  - 初回表示: `Start()`で現在のハイスコアを表示
  - `HighScoreManager.OnHighScoreUpdated`を購読して更新
  - TextMeshProで表示（例: "High Score: 1500"）
  - StringBuilderでGC削減
- **配置**: タイトル画面、ゲーム中UI（オプション）
- **Inspector設定**:
  - `_highScoreText`: TextMeshProUGUI参照
  - `_prefix`: 表示プレフィックス（デフォルト: "High Score: "）
  - `_highlightColor`: 新記録達成時の強調色（オプション）
- **実装例**:
  ```csharp
  void Start() {
      // 初期表示（既存のハイスコア）
      UpdateDisplay(HighScoreManager.Instance.GetHighScore());
      
      // イベント購読（新記録更新時）
      HighScoreManager.OnHighScoreUpdated.AddListener(UpdateDisplay);
  }
  
  void UpdateDisplay(int highScore) {
      _stringBuilder.Clear();
      _stringBuilder.Append(_prefix);
      _stringBuilder.Append(highScore);
      _highScoreText.text = _stringBuilder.ToString();
  }
  ```

**ResultScoreDisplay.cs**（既存修正）
- **追加機能**: 
  - ハイスコア更新時に特別表示（"NEW RECORD!"等）
  - `HighScoreManager.IsNewHighScore()`でチェック
  - 色変更・テキスト追加で視覚的フィードバック
- **Inspector追加設定**:
  - `_highlightColor`: 新記録時の文字色
  - `_newRecordText`: "NEW RECORD!"表示用TextMeshProUGUI（オプション）
- **修正実装例**:
  ```csharp
  private void OnGameOver() {
      // ... 既存のスコア表示処理 ...
      
      int finalScore = ScoreManager.Instance.GetScore();
      
      // 新記録チェック
      if (HighScoreManager.Instance.IsNewHighScore(finalScore)) {
          ShowNewRecordEffect();
      }
  }
  
  private void ShowNewRecordEffect() {
      // 方法1: 色変更
      if (_highlightColor != Color.clear)
          _finalScoreText.color = _highlightColor;
      
      // 方法2: 追加テキスト表示
      if (_newRecordText != null)
          _newRecordText.gameObject.SetActive(true);
      
      if (GameConstants.DEBUG_MODE)
          Debug.Log("[ResultScoreDisplay] New record displayed!");
  }
  ```

##### 1-3. 統合フロー
1. **ゲーム開始時**: HighScoreManagerがPlayerPrefsから読み込み
2. **ゲーム中**: HighScoreDisplayが常に表示
3. **ゲーム終了時**: 
   - GameManager.OnGameOver発行
   - HighScoreManagerが現在スコアをチェック
   - 新記録ならOnHighScoreUpdated発行＋保存
   - ResultScoreDisplayが新記録を強調表示

##### 1-4. 将来拡張案（オプション）
- **ランキング機能**: TOP 5までの記録を保存（配列/List）
- **プレイ統計**: 総プレイ回数、平均スコア、最高連続プレイ等
- **オンラインランキング**: 外部API連携（ネットワーク層追加時）

---

### 2. 音符破棄時のエフェクトシステム

#### 概要
- **目的**: ゲーム体験の向上、視覚的フィードバック強化
- **使用アセット**: CFXR (Cartoon FX Remaster) - JMO Assets
- **アーキテクチャ原則**: Object Poolパターン、低遅延、疎結合

#### 実装方針

##### 2-1. エフェクト管理層（新規クラス）
**EffectPool.cs** - `Assets/Scripts/Gameplay/EffectPool.cs`
- **責務**: エフェクトPrefabのプール管理（CFXR_Effect完全活用版）
- **機能**:
  - 事前にエフェクトインスタンスをプール（初期サイズ: GameConstants.EFFECT_POOL_INITIAL_SIZE）
  - `PlayEffect(Vector3 position, Quaternion rotation)` - エフェクト再生
  - **超シンプル設計: CFXR_Effectが自動で`SetActive(false)`→プール側は`List.Find`で再利用**
  - 不足時の自動拡張（インスタンス追加）
- **実装方式**: シングルトンパターン
- **特徴**: 
  - ❌ **EffectAutoReturn不要**
  - ❌ **ReturnEffect()メソッド不要**
  - ❌ **OnDisableフック不要**
  - ✅ **追加コンポーネントなし**
  - ✅ **コード量 ~60行（超軽量）**
- **Prefab参照**:
  - Inspector設定またはResources.Load
  - 推奨Prefab: `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR4 Bouncing Glows Bubble (Blue Purple).prefab`
  - 代替案: `CFXR4 Falling Stars.prefab`（より華やか）

---

#### 🔍 **重要発見: CFXR_Effect.csの自動管理機能**

CFXRアセットの`CFXR_Effect.cs`には以下の機能が実装されています：

```csharp
public enum ClearBehavior {
    None,      // 何もしない
    Disable,   // ★ SetActive(false) - プールに最適！
    Destroy    // GameObject.Destroy()
}

[Tooltip("Defines an action to execute when the Particle System has completely finished playing and emitting particles.")]
public ClearBehavior clearBehavior = ClearBehavior.Destroy;
```

**動作メカニズム（CFXR_Effect.cs内）:**
```csharp
void Update() {
    if (clearBehavior != ClearBehavior.None) {
        // 20フレームごとにチェック（パフォーマンス最適化済み）
        if ((Time.renderedFrameCount + startFrameOffset) % CHECK_EVERY_N_FRAME == 0) {
            if (!rootParticleSystem.IsAlive(true)) {
                if (clearBehavior == ClearBehavior.Destroy) {
                    GameObject.Destroy(this.gameObject);
                } else {
                    this.gameObject.SetActive(false);  // ★ Disable時
                }
            }
        }
    }
}
```

**利点:**
- ✅ エフェクト側で自動的に`SetActive(false)`を実行
- ✅ 20フレームごとのチェックでパフォーマンス最適化済み
- ✅ `IsAlive(true)`で子ParticleSystemも含めて完全終了を確認
- ✅ フレームオフセットで複数エフェクトのチェック分散

---

#### **最適実装: CFXR_Effectの完全活用（超シンプル版）**

**🎯 重要な発見: プール側の処理は実質不要！**

CFXR_Effectが自動で`SetActive(false)`するため、**プール側は再生時に非アクティブなエフェクトを探すだけ**。返却処理は一切不要です。

**方法D: CFXR_Effectの自動Disable + シンプル検索（最もシンプル）**

```csharp
// EffectPool.cs - 完全版
using UnityEngine;
using System.Collections.Generic;
using System.Linq;  // Count()用
using CartoonFX;  // ★ 必須

public class EffectPool : MonoBehaviour {
    public static EffectPool Instance { get; private set; }
    
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = GameConstants.EFFECT_POOL_INITIAL_SIZE;
    
    private List<GameObject> _allEffects = new List<GameObject>();
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        } else {
            Destroy(gameObject);
        }
    }
    
    void InitializePool() {
        for (int i = 0; i < initialPoolSize; i++) {
            GameObject effect = Instantiate(effectPrefab, poolContainer);
            
            // CFXR_Effectの設定（clearBehaviorのみ、ResetStateは不要）
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null) {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
                // ★ ResetState()不要: OnDisableで自動呼び出しされる
            }
            
            effect.SetActive(false);
            _allEffects.Add(effect);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Initialized with {initialPoolSize} effects");
    }
    
    public void PlayEffect(Vector3 position, Quaternion rotation) {
        // ★ 非アクティブなエフェクトを探す（CFXR_Effectが自動でfalseにしたもの）
        GameObject effect = _allEffects.Find(e => !e.activeInHierarchy);
        
        if (effect == null) {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning("[EffectPool] Pool exhausted, creating new effect");
            
            effect = Instantiate(effectPrefab, poolContainer);
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null) {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
            }
            _allEffects.Add(effect);
        }
        
        // 位置・回転設定
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        
        // ★ CFXR_Effectの状態リセット（重要: 前回の再生状態をクリア）
        // GetComponentは毎回呼ぶ（拡張時に追加されたエフェクトも対応）
        var cfxr = effect.GetComponent<CFXR_Effect>();
        if (cfxr != null) {
            cfxr.ResetState();  // time, isFadingOut, animatedLights等をリセット
        }
        
        // ★ SetActive(true)でアクティブ化（AnimatedLight等が有効化、ParticleSystemも自動再生）
        effect.SetActive(true);
        // ※ ParticleSystemは「Play On Awake = true」のため自動再生される
        
        // → エフェクト終了後、CFXR_EffectがSetActive(false)を自動実行
        // → OnDisable()が呼ばれてResetState()も自動実行
        // → 次回PlayEffect()で再利用可能
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Effect played at {position}");
    }
}
```

**🎉 驚異的なシンプルさ:**
- **EffectAutoReturn.cs 不要！**
- **ReturnEffect() 不要！**
- **OnDisableフック 不要！**
- **Queue管理 不要！**
- **コード量: ~60行（従来の半分以下）**

**動作フロー:**
1. `PlayEffect()` → 非アクティブなエフェクトを`List.Find`で検索
2. `ResetState()` → 前回の再生状態をクリア
3. `SetActive(true)` → OnEnableでAnimatedLight有効化、ParticleSystem自動再生（Play On Awake）
4. （エフェクト再生中 - プール側は何もしない）
5. CFXR_Effectが20フレームごとに`IsAlive(true)`チェック
6. エフェクト終了 → CFXR_Effectが`SetActive(false)`自動実行
7. 次回`PlayEffect()`時に再び検索対象になる

**プール側の処理:**
- ✅ 再生時の検索のみ（`List.Find`）
- ❌ 返却処理なし
- ❌ Update/Coroutineなし
- ❌ イベント購読なし

**パフォーマンス:**
- 再生: O(n)（List.Find、通常は最初の数個で見つかるため実質O(1)に近い）
- 返却: なし（CFXR_Effect側で完全自動）
- メモリ: 追加コンポーネント不要

---

#### **実装手順（超簡略版）:**

1. **エフェクトPrefabの設定**
   - Inspector上で`CFXR_Effect`の`clearBehavior`を`Disable`に変更
   - **それだけ！** 追加コンポーネント不要

2. **EffectPool.cs実装**
   - 上記コードをそのまま使用
   - `using CartoonFX;`を忘れずに

---

#### **他の方法との比較:**

| 方法 | メリット | デメリット | 推奨度 |
|------|---------|-----------|--------|
| **A: Coroutine** | 独立制御、確実 | Coroutineオーバーヘッド、管理複雑 | ⭐⭐⭐ |
| **B: Update()ポーリング** | シンプル | 毎フレームチェック、重複確認 | ⭐⭐ |
| **C: CFXR自動Disable + OnDisableフック** | アセット機能活用 | 追加コンポーネント必要、やや複雑 | ⭐⭐⭐⭐ |
| **D: CFXR自動Disable + List.Find** | **超シンプル、追加コンポーネント不要** | 小規模プール向き | **⭐⭐⭐⭐⭐** |

---

#### **最終推奨: 方法D採用（最もシンプル）**

**理由:**
1. **CFXR_Effect側で既に最適化されたチェック**（20フレームごと、オフセット分散）
2. **プール側のコード量が最小**（~60行、追加クラス不要）
3. **確実な終了検出**（`IsAlive(true)`で子ParticleSystemも確認）
4. **追加コンポーネント不要**（Prefab設定だけ）
5. **直感的な設計**（activeInHierarchyで状態管理）

**データ構造（超シンプル）:**
```csharp
[SerializeField] private GameObject effectPrefab;           // エフェクトPrefab
[SerializeField] private Transform poolContainer;           // プールコンテナ
[SerializeField] private int initialPoolSize = GameConstants.EFFECT_POOL_INITIAL_SIZE;

private List<GameObject> _allEffects = new List<GameObject>();
// ★ Queue不要、EffectAutoReturn不要、_activeEffects不要
// ★ GetComponentキャッシュも不要（毎回取得でOK、拡張時の安全性確保）
```

**パフォーマンス特性:**
- エフェクト再生: O(n)（List.Find、プールサイズ50なら最大50回の比較、通常は数回で発見）
- エフェクト終了: プール側コスト0（CFXR_Effectが全処理）
- チェックコスト: 0（CFXR_Effectが20フレームごとに自動チェック）
- メモリオーバーヘッド: なし（追加コンポーネント不要）

##### 2-2. EffectManager.cs（オプション・シンプル化案）
- **責務**: エフェクト再生の統一インターフェース
- **機能**:
  - `PlayNoteDestroyEffect(Vector3 position)` - 音符破棄エフェクト再生
  - 将来的に複数種類のエフェクトを管理（爆発、ヒット、ミス等）
  - EffectPoolに委譲
- **実装方式**: シングルトンまたはstaticクラス

##### 2-3. 統合ポイント
**NoteShakeHandler.cs**（既存修正）
- **修正箇所**: `HandleShake()`メソッド内、音符破棄処理
- **重要**: Null安全性を確保（現在の実装にも存在するチェックを維持）
- **追加処理**:
  ```csharp
  public void HandleShake(string data, double timestamp) {
      // 1. 最古Note取得
      if (NoteManager.Instance == null) {
          Debug.LogWarning("[NoteShakeHandler] NoteManager instance not found!");
          return;
      }
      
      Note oldest = NoteManager.Instance.GetOldestNote();
      
      // ★ Nullチェック（重要: 音符がない場合の安全性）
      if (oldest == null) {
          if (GameConstants.DEBUG_MODE)
              Debug.Log("[NoteShakeHandler] No notes to destroy");
          return;
      }
      
      // 2. 位置を記録（破棄前に取得）
      Vector3 notePosition = oldest.transform.position;
      
      // 3. 最古Note破棄
      NoteManager.Instance.DestroyOldestNote();
      
      // ★ 4. エフェクト再生（新規追加）
      if (EffectPool.Instance != null)
          EffectPool.Instance.PlayEffect(notePosition, Quaternion.identity);
      
      // 5. 効果音
      if (AudioManager.Instance != null)
          AudioManager.Instance.PlaySFX("hit");
      
      // 6. スコア加算
      if (ScoreManager.Instance != null)
          ScoreManager.Instance.AddScore(_scoreValue);
      
      if (GameConstants.DEBUG_MODE)
          Debug.Log($"[NoteShakeHandler] Note destroyed with effect, score +{_scoreValue}");
  }
  ```

**修正のポイント:**
- Null参照例外を防ぐため、`oldest`のチェック後に`transform.position`を取得
- エフェクト再生は音符破棄直後（位置記録後）に実行
- 処理順序: 取得→Nullチェック→位置記録→破棄→エフェクト→音→スコア

##### 2-4. エフェクトPrefab設定

**推奨エフェクト:**
- `CFXR4 Bouncing Glows Bubble (Blue Purple)` - 軽量、カラフル、短時間（推奨）
- `CFXR4 Falling Stars` - より華やか（代替案）

**Prefab配置:**
- `Assets/Resources/Prefabs/Effects/NoteDestroyEffect.prefab` にコピー
- または直接Inspector参照

**⚠️ 必須設定（CFXR_Effect.cs）:**
```
clearBehavior: Disable  （★ 重要: Destroyではなく必ずDisableに設定）
```

**Inspector上での設定手順（超簡単）:**
1. 推奨Prefabをシーンまたはプロジェクトで開く
2. `CFXR_Effect`コンポーネントを選択
3. `Clear Behavior`を`Disable`に変更（デフォルトは`Destroy`）
4. **完了！** 追加コンポーネント不要

**ParticleSystem設定（既にPrefabに設定済み）:**
- Duration: 0.5～1.0秒程度
- Looping: OFF（自動停止）
- ※ CFXR_Effectが`IsAlive(true)`で完全終了を自動検出

##### 2-5. パフォーマンス最適化
- **Object Pool**: 頻繁な生成・破棄を回避（初期化時に一括生成）
- **自動Disable**: CFXR_Effectが終了時に自動で`SetActive(false)`
- **バッチング**: ParticleSystemのMaterial共有でドローコール削減
- **レイテンシ目標**: 音符破棄からエフェクト再生まで < 1ms
- **プール側処理**: 再生時の`List.Find`のみ（返却処理なし）

##### 2-6. デバッグ・検証
- **DEBUG_MODE**: エフェクト再生時にログ出力
- **Inspector監視**: 
  - アクティブエフェクト数（`_allEffects.Count(e => e.activeInHierarchy)`）
  - 総プールサイズ（`_allEffects.Count`）
  - 利用可能エフェクト数（`_allEffects.Count(e => !e.activeInHierarchy)`）
- **テスト項目**:
  - 大量シェイク時のパフォーマンス（60fps維持）
  - プール不足時の自動拡張
  - CFXR_Effectの自動Disable動作確認

---

### 実装優先順位

#### Phase 1: ハイスコアシステム（必須）
1. HighScoreManager.cs作成
2. HighScoreDisplay.cs作成
3. ResultScoreDisplay.cs修正（新記録表示）
4. GameManagerとの統合

#### Phase 2: エフェクトシステム（視覚的改善）
1. **GameConstants.cs修正**（HIGH_SCORE_KEY, EFFECT_POOL_INITIAL_SIZE追加）
2. **EffectPool.cs作成**（プール本体、~65行の超軽量実装）
3. **エフェクトPrefab設定**
   - CFXR_Effectの`clearBehavior`を`Disable`に変更
   - **完了！**（追加コンポーネント不要）
4. **NoteShakeHandler.cs修正**（エフェクト統合）
5. **パフォーマンステスト**

#### Phase 3: 拡張・最適化（オプション）
- ランキングTOP5機能
- 複数種類のエフェクト（ミス時、フリーズ時等）
- プレイ統計表示
- エフェクトの色をフェーズに応じて変更

---

### アーキテクチャ原則の遵守

✅ **疎結合**: 各マネージャーはイベント経由で通信  
✅ **責務分離**: HighScoreManager（永続化）、EffectPool（プール管理）、UI（表示）が独立  
✅ **イベント駆動**: GameManager.OnGameOverをトリガーに自動処理  
✅ **Object Pool**: NotePoolと同じパターンでEffectPoolを実装  
✅ **パフォーマンス**: プール再利用、StringBuilder使用、CFXR_Effect自動Disableで最適化  
✅ **拡張性**: 将来的なランキング・オンライン連携を見据えた設計

---

### GameConstants.cs への追加項目

実装前に以下の定数を`GameConstants.cs`に追加する必要があります：

```csharp
// ハイスコア関連
public const string HIGH_SCORE_KEY = "HighScore";

// エフェクトプール関連
public const int EFFECT_POOL_INITIAL_SIZE = 50;  // 初期プールサイズ（通常これで十分）
// EFFECT_POOL_EXPAND_SIZEは不要（動的拡張は1個ずつで十分）
```

---

### 実装時の注意事項

#### 1. イベント購読順序
- `GameManager.OnGameOver`は複数のマネージャーが購読
- 購読順序は保証されないため、各マネージャーは独立して動作する設計
- HighScoreManagerは`OnGameOver`時点でScoreManagerからスコアを取得（リセット前）

#### 2. エフェクトPrefabの設定確認
- **必須**: `CFXR_Effect.clearBehavior`を`Disable`に変更（Inspector上）
- Duration、Looping設定の確認（通常は既に適切に設定済み）
- テストシーンでエフェクト再生→自動Disable動作を確認（SetActive(false)になるか）

#### 3. パフォーマンステスト項目
- 連続シェイク時の60fps維持
- エフェクトプール不足時の自動拡張
- メモリ使用量の監視（Inspector上でプールサイズ確認）

#### 4. デバッグ機能
- HighScoreManagerに`[ContextMenu]`でリセット機能を実装
- EffectPoolのプール状態をInspector上で可視化
- DEBUG_MODEでの詳細ログ出力

#### 5. CFXR_Effect特有の注意点
- **ResetState()の重要性**: エフェクト再利用時に必ず呼び出す
  - 内部タイマー（time）のリセット
  - フェードアウト状態のクリア
  - AnimatedLight/CameraShakeのリセット
- **OnEnable/OnDisableのタイミング**: 
  - OnEnable: AnimatedLight有効化のみ（ParticleSystemは「Play On Awake」で自動再生）
  - OnDisable: ResetState()自動呼び出し（内部実装済み）
  - ⚠️ **重要**: `SetActive(false)`→OnDisable→ResetState()の順で自動実行される
  - ⚠️ **注意**: Prefabの「Play On Awake」が有効な場合、`SetActive(true)`で自動再生
- **clearBehavior = Disable**: エフェクト終了時に自動で`SetActive(false)`
  - プール側は`activeInHierarchy`で再利用可能なエフェクトを判定
  - **追加の返却処理不要！**
- **namespace**: `CartoonFX.CFXR_Effect`への参照が必要
  ```csharp
  using CartoonFX;  // ★ 必須
  ```

---

### 📋 実装時のチェックリスト

#### EffectPool実装（超簡潔版）
- [ ] `using CartoonFX;`と`using System.Linq;`を追加
- [ ] 初期化時に`CFXR_Effect.clearBehavior = Disable`設定
- [ ] ~~初期化時に`ResetState()`呼び出し~~ ← **不要**（OnDisableで自動）
- [ ] 再生時に`List.Find(e => !e.activeInHierarchy)`で再利用
- [ ] 再生時に`ResetState()`呼び出し（**必須**：前回状態クリア）
- [ ] 再生時に`SetActive(true)`で再生（Play On Awakeが有効なため自動再生）
- [ ] デバッグログで動作確認（アクティブ数、利用可能数）

#### Prefab設定（最小限）
- [ ] CFXR_Effectの`clearBehavior`を`Disable`に変更
- [ ] **以上！**（追加コンポーネント不要）
- [ ] テストシーンで再生→自動Disable確認

#### パフォーマンステスト
- [ ] 連続100回シェイクで60fps維持確認
- [ ] プール枯渇時の自動拡張動作確認
- [ ] メモリリーク確認（Profilerで監視）
- [ ] CFXR_Effectの自動Disable確認（エフェクト終了後にactiveInHierarchy=falseになるか）
- [ ] List.Findの検索時間計測（プールサイズ50で問題ないか確認）

---

## コード外修正項目
- フェーズ表示の文字化け