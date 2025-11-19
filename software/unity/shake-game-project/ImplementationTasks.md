# 実装タスク一覧（2025-11-19）

## 概要
CodeArchitecture.mdの設計に基づき、以下5つの修正を実装してください。
各修正は独立しているため、順番に実装可能です。

---

## 修正1: 休符モードで生成された音符が休符画像になっていない

### 問題
RestPhaseで生成された音符が、生成直後は音符画像のまま表示される。

### 修正内容
**ファイル**: `Assets/Scripts/Gameplay/NoteSpawner.cs`

1. フィールド追加（クラスの先頭付近）:
```csharp
private Phase _currentPhase = Phase.NotePhase;
```

2. `OnPhaseChanged(PhaseChangeData phaseData)`メソッドの**先頭行**に追加:
```csharp
_currentPhase = phaseData.phaseType;
```

3. `SpawnOneNote()`メソッド内、`note.SetSpriteID(randomID)`の直後に追加:
```csharp
// 現在のフェーズを設定（生成時に正しい画像を表示）
note.SetPhase(_currentPhase);
```

### テスト方法
デバッグモードでRestPhase中に音符が生成された際、即座に休符画像が表示されることを確認。

---

## 修正2: ラストスパートでもフリーズを有効にする

### 問題
LastSprintPhase中はフリーズが無効化されている。

### 修正内容
**ファイル**: `Assets/Scripts/Managers/FreezeManager.cs`

1. `StartFreeze(float duration)`メソッド内の以下のブロックを削除（約107-114行目）:
```csharp
// LastSprintPhase 中は凍結しない（無効化）
if (PhaseManager.Instance != null && 
    PhaseManager.Instance.GetCurrentPhase() == Phase.LastSprintPhase)
{
    if (GameConstants.DEBUG_MODE)
        Debug.Log("[FreezeManager] LastSprintPhase detected, freeze disabled");
    return;
}
```

2. クラスドキュメント（約13行目）の「LastSprintPhase 中は無効」記述を削除

### テスト方法
LastSprintPhase中に休符をシェイクした際、フリーズが発動することを確認。

---

## 修正3: ゲーム全体のタイマー表示（TextMeshPro）

### 問題
ゲーム全体の残り時間を表示するUIが存在しない。

### 修正内容
**新規ファイル**: `Assets/Scripts/UI/TimerDisplay.cs`

**テンプレート**: `Assets/Scripts/UI/ScoreDisplay.cs`を参考に作成

**実装仕様**:
```csharp
using UnityEngine;
using TMPro;
using System.Text;

public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;
    
    private float _remainingTime = 0f;
    private bool _isRunning = false;
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        GameManager.OnGameStart.AddListener(OnGameStart);
        GameManager.OnShowTitle.AddListener(OnShowTitle);
    }
    
    private void OnGameStart()
    {
        _remainingTime = GameConstants.GAME_DURATION;
        _isRunning = true;
    }
    
    private void OnShowTitle()
    {
        _isRunning = false;
        _remainingTime = 0f;
    }
    
    void Update()
    {
        if (!_isRunning || _timerText == null) return;
        
        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(0f, _remainingTime);
        
        // 表示更新
        _stringBuilder.Clear();
        _stringBuilder.Append(Mathf.CeilToInt(_remainingTime));
        _stringBuilder.Append("s");
        _timerText.text = _stringBuilder.ToString();
    }
    
    void OnDestroy()
    {
        if (GameManager.OnGameStart != null)
            GameManager.OnGameStart.RemoveListener(OnGameStart);
        if (GameManager.OnShowTitle != null)
            GameManager.OnShowTitle.RemoveListener(OnShowTitle);
    }
}
```

**重要**: ゲーム終了判定は行わない（PhaseManagerが担当）

### Unity Editor作業
1. PlayパネルにTextMeshProコンポーネントを配置
2. TimerDisplayスクリプトをアタッチ
3. InspectorでTextMeshProを`_timerText`に割り当て

---

## 修正4: フェーズ表示（TextMeshPro）

### 問題
現在のフェーズ名を表示するUIが存在しない。

### 修正内容
**新規ファイル**: `Assets/Scripts/UI/PhaseDisplay.cs`

**テンプレート**: `Assets/Scripts/UI/ScoreDisplay.cs`を参考に作成

**実装仕様**:
```csharp
using UnityEngine;
using TMPro;
using System.Text;

public class PhaseDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _phaseText;
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        if (PhaseManager.Instance != null)
        {
            PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
    }
    
    private void OnPhaseChanged(PhaseChangeData data)
    {
        if (_phaseText == null) return;
        
        _stringBuilder.Clear();
        _stringBuilder.Append(GetPhaseName(data.phaseType));
        _phaseText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[PhaseDisplay] Phase changed to: {data.phaseType}");
    }
    
    private string GetPhaseName(Phase phase)
    {
        switch (phase)
        {
            case Phase.NotePhase: return "♪ 音符フェーズ";
            case Phase.RestPhase: return "💤 休符フェーズ";
            case Phase.LastSprintPhase: return "🔥 ラストスパート";
            default: return "不明";
        }
    }
    
    void OnDestroy()
    {
        if (PhaseManager.OnPhaseChanged != null)
        {
            PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
    }
}
```

### Unity Editor作業
1. PlayパネルにTextMeshProコンポーネントを配置
2. PhaseDisplayスクリプトをアタッチ
3. InspectorでTextMeshProを`_phaseText`に割り当て

---

## 修正5: 最終スコア表示（TextMeshPro）

### 問題
リザルトパネルに最終スコアを表示するUIが存在しない。

### 修正内容
**新規ファイル**: `Assets/Scripts/UI/ResultScoreDisplay.cs`

**テンプレート**: `Assets/Scripts/UI/ScoreDisplay.cs`を参考に作成

**実装仕様**:
```csharp
using UnityEngine;
using TMPro;
using System.Text;

public class ResultScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private string _prefix = "Final Score: ";
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        GameManager.OnGameOver.AddListener(OnGameOver);
    }
    
    private void OnGameOver()
    {
        if (_finalScoreText == null || ScoreManager.Instance == null) return;
        
        int finalScore = ScoreManager.Instance.GetScore();
        
        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);
        _stringBuilder.Append(finalScore);
        _finalScoreText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ResultScoreDisplay] Final score displayed: {finalScore}");
    }
    
    void OnDestroy()
    {
        if (GameManager.OnGameOver != null)
        {
            GameManager.OnGameOver.RemoveListener(OnGameOver);
        }
    }
}
```

### Unity Editor作業
1. ResultパネルにTextMeshProコンポーネントを配置
2. ResultScoreDisplayスクリプトをアタッチ
3. InspectorでTextMeshProを`_finalScoreText`に割り当て

---

## 実装優先順位

1. **修正1** - 最優先（ゲームプレイの正確性）
2. **修正2** - 高優先（ゲームバランス）
3. **修正3** - 中優先（ユーザビリティ）
4. **修正4** - 中優先（ユーザビリティ）
5. **修正5** - 低優先（機能完全性）

---

## 設計原則（参照: CodeArchitecture.md）

すべての修正は以下の原則に準拠:
- **イベント駆動設計**: GameManager/PhaseManagerのイベントを購読
- **責務分離**: 各クラスは単一の責務を持つ
- **疎結合**: シングルトン参照を最小化
- **メモリリーク防止**: OnDestroy()で必ずイベント購読解除
- **GC削減**: StringBuilderを再利用

---

## テスト項目

### 全体テスト
1. タイトル画面 → ゲーム開始 → ゲーム終了 → タイトル復帰の一連の流れ
2. 各フェーズの切り替えが正常に動作
3. メモリリークがないこと（複数回プレイ）

### 個別テスト
- **修正1**: RestPhaseで休符画像が即座に表示
- **修正2**: LastSprintPhaseでもフリーズが発動
- **修正3**: タイマーが正確にカウントダウン
- **修正4**: フェーズ名が正しく表示・切り替え
- **修正5**: リザルトパネルに最終スコアが表示
