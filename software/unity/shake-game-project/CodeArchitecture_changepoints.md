# アーキテクチャ変更点

## 2025-11-19: インターフェースとイベントシステムの修正

### 問題点
1. **IInputSource インターフェース**: `event UnityAction` として定義されていたが、MonoBehaviour での実装が困難
2. **静的イベントアクセス**: Singleton パターンの Manager クラスで静的イベントに Instance 経由でアクセスしていた
3. **Update メソッド競合**: IInputSource に `Update()` が定義されていたが、MonoBehaviour の Update と競合

### 修正内容

#### 1. IInputSource インターフェース変更
**変更前:**
```csharp
event UnityAction OnShakeDetected;
void Update();
```

**変更後:**
```csharp
UnityEvent OnShakeDetected { get; }
// Update() メソッドを削除
```

**理由:**
- `event` キーワードは C# のイベント専用で、`UnityEvent` は `+=`/`-=` 演算子のみ使用可能
- `UnityEvent` をプロパティとして公開することで、`AddListener`/`RemoveListener` が使用可能に
- MonoBehaviour の `Update()` と競合するため、インターフェースから削除

#### 2. 実装クラスでの OnShakeDetected 実装変更
**KeyboardInputReader / SerialInputReader 変更:**
```csharp
// 変更前
public UnityEvent OnShakeDetected { get; private set; } = new UnityEvent();

// 変更後
private UnityEvent _onShakeDetected = new UnityEvent();
public UnityEvent OnShakeDetected => _onShakeDetected;
```

**理由:**
- プロパティの初期化子（`{ get; private set; } = new UnityEvent()`）はインターフェースの getter-only プロパティと互換性なし
- backing field を使用した実装に変更

#### 3. 静的イベントアクセスの修正
**変更対象:** ShakeResolver, FreezeEffectUI, PhaseProgressBar, ScoreDisplay

**変更内容:**
```csharp
// 変更前（誤り）
PhaseManager.Instance.OnPhaseChanged.AddListener(...)
FreezeManager.Instance.OnFreezeChanged.AddListener(...)
ScoreManager.Instance.OnScoreChanged.AddListener(...)

// 変更後（正しい）
PhaseManager.OnPhaseChanged.AddListener(...)
FreezeManager.OnFreezeChanged.AddListener(...)
ScoreManager.OnScoreChanged.AddListener(...)
```

**理由:**
- Manager クラスの `OnXxxChanged` イベントは `static` として定義されている
- 静的メンバーはクラス名で直接アクセスする必要がある
- Instance 経由でのアクセスはコンパイルエラーとなる

### 影響範囲
- **Input層**: KeyboardInputReader, SerialInputReader
- **Data層**: IInputSource インターフェース
- **Input層**: ShakeResolver
- **UI層**: FreezeEffectUI, PhaseProgressBar, ScoreDisplay

### 設計原則
- **インターフェース設計**: C# のイベントと Unity の UnityEvent の違いを理解した実装
- **静的メンバーアクセス**: Singleton パターンでも静的イベントはクラス名で直接アクセス
- **MonoBehaviour 統合**: Unity のライフサイクルメソッドとインターフェース定義の競合回避
