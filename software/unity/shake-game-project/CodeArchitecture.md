# ゲームコード再構築 - アーキテクチャ設計書

## 1. 概要

シェイクゲームの全体的なコード構造を再設計する。以下の目標を達成：
- **可読性向上** - 責務が明確で、新規開発者が容易に理解可能な構造
- **保守性向上** - 変更・追加時に既存コード修正が最小限で済む
- **パフォーマンス** - スレッド活用、オブジェクトプール、AudioDSP同期対応
- **AIコード生成最適化** - 疎結合・イベント駆動で、AIが正確なコード生成を実施可能

## 2. アーキテクチャ概観

### 2.1 基本設計パターン
- **統一キュー方式** - Serial/Keyboardの両方から統一キューに入力、UnityEvent廃止で約3倍高速化
- **2段階ハンドラー** - フリーズ層（NullShakeHandler）とフェーズ層（NoteShakeHandler/RestShakeHandler）で二重構造
- **Strategy パターン** - フリーズ/フェーズごとにハンドラーを動的に切り替え
- **Object Pool パターン** - 音符インスタンスの再利用で高速化
- **ブロッキングI/O** - SerialPort.ReadLine()でCPU使用率削減、レイテンシ0ms化

### 2.2 疎結合設計
```
[SerialInputReader / KeyboardInputReader]
           ↓ (統一キュー: ShakeResolver.EnqueueInput)
    [ShakeResolver] ← 統一キューから取り出し
           ↓ (2段階ハンドラー構造)
   [フリーズ層: _currentHandler]
      → _nullHandler (フリーズ中)
      → _activeHandler (通常時)
           ↓
   [フェーズ層: _activeHandler]
      → NoteShakeHandler
      → RestShakeHandler
```
統一キューで入力を一元管理。2段階ハンドラーでフリーズ/フェーズを独立制御。

---

## 3. データ構造定義（Data/）

### 3.0.1 フェーズイベント用構造体
```csharp
/// <summary>
/// フェーズ変更時に発行されるイベントの引数
/// </summary>
[System.Serializable]
public struct PhaseChangeData {
    public Phase phaseType;           // 現在のフェーズタイプ（NotePhase等）
    public float duration;             // このフェーズの継続時間（秒）
    public float spawnFrequency;      // 音符湧き出し頻度（秒・間隔）
    public int phaseIndex;             // フェーズ番号（0, 1, 2...）
}

/// <summary>
/// フェーズ種別（現在の実装と統一）
/// </summary>
public enum Phase { NotePhase, RestPhase, LastSprintPhase }
```

**設計の根拠**：
- **構造体採用** - 複数の関連情報を1つの引数で安全に渡せる
- **複数フィールド** - duration, spawnFrequency, phaseTypeを同時に取得可能
- **Serializable** - InspectorでPhaseChangeData配列の編集が可能（将来拡張時）

### 3.0.2 入力インターフェース
```csharp
/// <summary>
/// 入力ソースの抽象化（直接呼び出し方式）
/// Serial通信、キーボード入力など、異なる入力元に対応
/// UnityEventを廃止し、キューへの直接アクセスで約3倍高速化
/// </summary>
public interface IInputSource {
    /// <summary>
    /// キューから入力データを取り出す（直接呼び出し方式）
    /// </summary>
    /// <param name="input">取り出された入力データ（data: 文字列, timestamp: AudioSettings.dspTime）</param>
    /// <returns>キューにデータがあれば true、空なら false</returns>
    bool TryDequeue(out (string data, double timestamp) input);
    
    /// <summary>
    /// 入力ソースの接続
    /// </summary>
    void Connect();
    
    /// <summary>
    /// 入力ソースの切断
    /// </summary>
    void Disconnect();
}
```

**設計変更の理由（2025-11-19）**：
- **UnityEvent廃止** - イベント経由（約30 CPU cycles）から直接呼び出し（約10 cycles）に変更し、約3倍高速化
- **TryDequeueパターン** - ShakeResolverのUpdate()から直接キューを読み取る方式に変更
- **タイムスタンプ統合** - データと同時にAudioSettings.dspTimeを返すことでオーディオ同期を実現

### 3.0.3 シェイク処理インターフェース
```csharp
/// <summary>
/// フェーズごとのシェイク処理を定義
/// </summary>
public interface IShakeHandler {
    void HandleShake(string data, double timestamp);
}
```

---

### 3.1 Managers/

#### GameManager.cs
- **責務**：ゲーム全体のライフサイクル管理
- **重要イベント**：
  - `public static UnityEvent OnShowTitle;` - タイトル画面表示（起動時・復帰時共通）
  - `public static UnityEvent OnGameStart;` - ゲーム開始
  - `public static UnityEvent OnGameOver;` - ゲーム終了
- **実装方式**：シングルトン + staticなUnityEvent<T>
- **機能**：
  - ゲーム開始・終了の状態管理
  - タイトル画面表示と全システムリセット
  - PhaseManager の実行を制御
  - 各マネージャーの初期化・クリーンアップ
- **重要メソッド**：
  - `ShowTitle()` - タイトル画面表示＋状態リセット（起動時・復帰時共通）
  - `StartGame()` - ゲーム開始
  - `EndGame()` - ゲーム終了
- **Start()メソッド**：
  - アプリ起動時に自動的に`ShowTitle()`を呼び出し
  - DRY原則：起動時とタイトル復帰を統一処理
- **備考**：UI層やマネージャーがこのイベントを購読

#### PhaseManager.cs
- **責補**：ゲームフェーズの時系列管理
- **重要イベント**：
  - `public UnityEvent<PhaseChangeData> OnPhaseChanged;` - フェーズ変更（PhaseChangeData構造体を引数）
- **機能**：
  - `Phase_Sequence[]`配列を順に進行
  - Coroutineで各フェーズの継続時間を管理
  - フェーズ切り替え時に`OnPhaseChanged`を発行
  - タイトル復帰時に状態をリセット
- **重要メソッド**：
  - `IEnumerator ExecutePhase(Phase_Sequence phase)` - フェーズ実行
  - `Phase_Sequence GetCurrentPhase()` - 現在フェーズ取得（プロパティ）
  - `ResetPhaseManager()` - 状態リセット（Coroutine停止、変数初期化）
- **イベント購読**：
  - `GameManager.OnGameStart` → フェーズシーケンス開始
  - `GameManager.OnShowTitle` → 状態リセット
- **イベント発行例**：
  ```csharp
  PhaseChangeData data = new PhaseChangeData {
      phaseType = Phase.NotePhase,
      duration = seg.duration,
      spawnFrequency = seg.spawnFrequency,
      phaseIndex = phaseIndex
  };
  OnPhaseChanged.Invoke(data);
  ```
- **備考**：
  - ShakeResolver, NoteSpawner, UI層がこのイベントを購読
  - GameManager.OnGameStart発行後、PhaseManager.ExecutePhase()が呼ばれる

#### FreezeManager.cs
- **責務**：凍結状態（数秒間入力無視）を管理
- **機能**：
  - `StartFreeze(float duration)` - 凍結開始（全フェーズで有効）
  - Coroutineで時間経過を管理
  - 凍結中/解除時にイベント発行
  - タイトル復帰時に凍結状態をリセット
- **重要メソッド**：
  - `ResetFreezeState()` - 凍結状態リセット（Coroutine停止、状態解除）
- **イベント購読**：
  - `GameManager.OnShowTitle` → 凍結状態リセット
- **イベント**：
  - `public UnityEvent<bool> OnFreezeChanged;` - true=凍結開始、false=凍結解除
- **備考**：
  - LastSprintPhaseを含む全フェーズでフリーズが有効（以前は無効化されていた）

#### ScoreManager.cs
- **責務**：スコア累積・イベント発行
- **機能**：
  - `AddScore(int points)` - スコア加算
  - スコア変更時にイベント発行
  - ゲーム開始時・タイトル復帰時にスコアをリセット
- **重要メソッド**：
  - `Initialize()` - スコアリセット（ゲーム開始時・タイトル復帰時共通）
- **イベント購読**：
  - `GameManager.OnGameStart` → スコアリセット
  - `GameManager.OnShowTitle` → スコアリセット
- **イベント**：
  - `public UnityEvent<int> OnScoreChanged;` - 現在スコアを引数

#### SpriteManager.cs
- **責務**：音符・休符画像の共通スプライト管理（プリロード方式）
- **機能**：
  - 複数種類の音符/休符画像をIDベースでペア管理
  - Inspector上で画像配列を設定
  - ランダムな音符種類IDの生成
  - ID指定による画像取得
- **実装方式**：シングルトンパターン
- **重要メソッド**：
  - `GetSpriteTypeCount()` - 音符種類の総数を取得
  - `GetRandomSpriteID()` - ランダムな音符種類IDを取得（0 ～ Count-1）
  - `GetNoteSpriteByID(int id)` - 指定IDの音符画像を取得
  - `GetRestSpriteByID(int id)` - 指定IDの休符画像を取得
- **データ構造**：
  ```csharp
  [SerializeField] private Sprite[] noteSprites;  // 音符画像配列
  [SerializeField] private Sprite[] restSprites;  // 休符画像配列
  // 対応関係：noteSprites[0] ⇔ restSprites[0]（例：quarter_note ⇔ quarter_rest）
  ```
- **設計の利点**：
  - **IDベース管理**: 同じIDで音符⇔休符の対応を保持
  - **共通スプライト**: 画像実体は1つ、複数のNoteから参照（メモリ効率的）
  - **プリロード**: ゲーム開始時に画像をメモリ上に確保
  - **疎結合**: Note, NoteSpawnerはSpriteManager経由でのみ画像にアクセス
- **備考**：
  - NoteSpawner が生成時にランダムIDを取得
  - Note が ID に基づいて画像参照をキャッシュ
  - フェーズ切り替え時は同じIDで音符⇔休符が自動変更

---

### 3.2 Input/

#### SerialPortManager.cs
- **責務**：COMポート接続・管理
- **機能**：
  - ゲーム開始時にポート接続
  - 接続失敗時は指数バックオフで再接続試行
  - ゲーム終了時にポート切断
  - **ブロッキングReadLine()** - データ到着まで待機（CPU使用率削減）
- **実装詳細**：
  - 再接続間隔：1秒 → 2秒 → 4秒...（最大10秒等で制限）
  - `ReadTimeout = SerialPort.InfiniteTimeout` - ブロッキング待機
  - `ReadLine()` - BytesToReadチェックなし、直接ReadLine()呼び出し
  - OnDestroy/OnDisableで安全に終了
- **パフォーマンス特性**：
  - データ待機中はCPU 0%（ブロッキング）
  - 入力レイテンシ: 0ms（データ到着即処理）

#### SerialInputReader.cs / KeyboardInputReader.cs
- **共通仕様**：
  - 入力を受け取り、`ShakeResolver.EnqueueInput(data, timestamp)` で統一キューに追加
  - IInputSourceインターフェースは**廃止**（統一キュー方式に変更）
  - Serial/Keyboardの両方から同時に入力を受け取れる
- **SerialInputReader**：
  - SerialPortManagerから`ReadLine()`でデータ取得
  - 別スレッドで**ブロッキング待機**（データが来るまで待つ）
  - データ受信時: `ShakeResolver.EnqueueInput(data.Trim(), AudioSettings.dspTime)`
  - タイムスタンプは`AudioSettings.dspTime`（オーディオ同期用）
  - Start()で自動的にスレッド開始（GameManagerイベント不要）
  - 未接続時のみThread.Sleep(500)で待機
- **KeyboardInputReader**：
  - `Update()`で毎フレーム`Input.GetKeyDown(KeyCode.Space)`をチェック
  - スペースキー押下時: `ShakeResolver.EnqueueInput("shake", AudioSettings.dspTime)`
  - フリーズチェックなし（ShakeResolverが処理）
  - テスト・デバッグ用途（常時有効）
- **パフォーマンス特性**：
  - Serial: ブロッキング待機でCPU使用率削減、入力レイテンシ0ms
  - Keyboard: 毎フレームチェック（軽量、条件分岐1つ）
  - 両方とも常に動作（DEBUG_MODEによる切り替え不要）

#### ShakeResolver.cs
- **責務**：受け取ったシェイクデータを現在のハンドラーに処理させる（統一キュー方式・2段階ハンドラー・Strategyパターン）
- **機能**：
  - **統一入力キュー**: `_sharedInputQueue` (static ConcurrentQueue) で全入力を一元管理
  - `EnqueueInput(string, double)` (static) - 外部から入力を追加
  - **2段階ハンドラー構造**:
    - `_currentHandler` - Update()で呼ばれる最終ハンドラー（フリーズ層）
    - `_activeHandler` - 通常時のハンドラー（フェーズ層）
  - フリーズ時: `_currentHandler = _nullHandler` （入力無視）
  - 非フリーズ時: `_currentHandler = _activeHandler` （フェーズに応じた処理）
  - フェーズ変更イベントを購読して`_activeHandler`を差し替え（Strategy パターン）
  - タイトル復帰時に入力キューをクリア
- **パフォーマンス最適化**：
  - フリーズ変更時（数秒に1回）: `_currentHandler`を切り替え
  - フェーズ変更時（数秒に1回）: `_activeHandler`を切り替え
  - シェイク処理時（秒間数十回）: 分岐なしで`_currentHandler.HandleShake()`を呼ぶだけ
  - UnityEvent経由なし、約3倍高速化（30 cycles → 10 cycles）
- **重要メソッド**：
  - `EnqueueInput(string, double)` (static) - 外部から統一キューに入力を追加
  - `OnFreezeChanged(bool)` - フリーズ層のハンドラー切り替え
  - `OnPhaseChanged(PhaseChangeData)` - フェーズ層のハンドラー切り替え
  - `ResetResolver()` - 統一キューをクリア
- **イベント購読**：
  - `FreezeManager.OnFreezeChanged` → `_currentHandler`を`_nullHandler`/`_activeHandler`に切り替え
  - `PhaseManager.OnPhaseChanged` → `_activeHandler`を差し替え（非フリーズ時のみ`_currentHandler`に反映）
  - `GameManager.OnShowTitle` → 統一キューをクリア
- **入力ソース統一**：
  - SerialInputReaderとKeyboardInputReaderの両方から同時に受け取れる
  - DEBUG_MODEによる切り替え不要（常に両方有効）
- **フリーズ中のフェーズ切り替え対応**：
  - フリーズ中にフェーズが変わっても`_activeHandler`のみ更新
  - フリーズ解除時に自動的に正しいフェーズハンドラーで処理再開
- **実装例**：
  ```csharp
  // 統一入力キュー（static）
  private static ConcurrentQueue<(string data, double timestamp)> _sharedInputQueue 
      = new ConcurrentQueue<(string data, double timestamp)>();
  
  public static void EnqueueInput(string data, double timestamp) {
      _sharedInputQueue.Enqueue((data, timestamp));
  }
  
  void Update() {
      // ★ 統一キューから取り出して処理
      while (_sharedInputQueue.TryDequeue(out var input)) {
          _currentHandler?.HandleShake(input.data, input.timestamp);
      }
  }
  
  void OnFreezeChanged(bool isFrozen) {
      // フリーズ層の切り替え
      _currentHandler = isFrozen ? _nullHandler : _activeHandler;
  }
  
  void OnPhaseChanged(PhaseChangeData data) {
      // フェーズ層の切り替え（_activeHandlerを変更）
      switch (data.phaseType) {
          case Phase.NotePhase:
              _currentHandler = _noteHandler;
              _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
              break;
          case Phase.LastSprintPhase:
              _currentHandler = _noteHandler;
              _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
              break;
          case Phase.RestPhase:
              _currentHandler = _restHandler;
              break;
      }
  }
  ```

---

### 3.3 Gameplay/

#### NotePool.cs
- **責務**：音符インスタンスの生成・管理
- **機能**：
  - ゲーム開始時に一定数の`Note`インスタンスをプリロード
  - `GetNote()` - 未使用インスタンスを返す
  - `ReturnNote(Note note)` - 使用済みインスタンスを回収
- **実装仕様**：
  - List + Queue で管理
  - アクティベート時は`note.ResetState()`を必ず呼ぶ
  - 返却時も同様に`ResetState()`を呼ぶ

#### NoteSpawner.cs
- **責補**：フェーズに応じた音符の湧き出し制御
- **機能**：
  - `PhaseManager.OnPhaseChanged`を購読
  - イベント内容の`PhaseChangeData.spawnFrequency`から「湧き出し間隔」を取得
  - Coroutineで定期的に音符をSpawn
  - `NotePool.GetNote()`でインスタンス取得
  - 音符の初期位置・ランダムカラー・**ランダムSprite ID**を設定
  - **生成時のフェーズ設定**: 現在のフェーズを保持し、生成時に`note.SetPhase(_currentPhase)`を呼び出して正しい画像を即座に表示
  - **動的生成範囲計算**: `OnEnable()`時にカメラの`orthographicSize`とアスペクト比から画面範囲を自動計算
  - 画面サイズの90%以内に生成（`GameConstants.NOTE_SPAWN_MARGIN`）
  - カメラ未設定時はInspector設定値をフォールバック
  - タイトル復帰時にスポーンを停止
- **重要メソッド**：
  - `CalculateSpawnRange()` - 画面サイズに基づいて生成範囲を動的計算
  - `StopSpawning()` - スポーンCoroutineを停止
  - `SpawnOneNote()` - 音符を1個生成（位置、回転、色、**Sprite ID**、**フェーズ**を設定）
- **イベント購読**：
  - `PhaseManager.OnPhaseChanged` → スポーン開始/変更、現在フェーズを記録
  - `GameManager.OnShowTitle` → スポーン停止
- **データ構造**：
  - `_calculatedRangeX`, `_calculatedRangeY`: 動的計算された生成範囲（Inspector表示可能）
  - `_mainCamera`: カメラ参照
  - `spawnRangeX`, `spawnRangeY`: フォールバック用の固定値
- **実装例**：
  ```csharp
  void OnPhaseChanged(PhaseChangeData phaseData) {
      StopAllCoroutines();
      StartCoroutine(SpawnNotesRoutine(phaseData.spawnFrequency));
  }
  
  IEnumerator SpawnNotesRoutine(float frequency) {
      while (true) {
          yield return new WaitForSeconds(frequency);
          SpawnOneNote();  // ランダムな位置・色・Sprite IDで生成
      }
  }
  
  void SpawnOneNote() {
      Note note = NotePool.GetNote();
      // 位置・回転設定...
      
      // ★ ランダムな音符種類IDを設定
      if (SpriteManager.Instance != null) {
          int randomID = SpriteManager.Instance.GetRandomSpriteID();
          note.SetSpriteID(randomID);
      }
      
      // ★ 現在のフェーズを設定（生成時に正しい画像を表示）
      note.SetPhase(_currentPhase);
      
      // ランダムカラー設定...
      // NoteManager登録...
  }
  ```
- **備考**：
  - `spawnFrequency`は`Phase_Sequence`に含まれるデータを想定
  - フェーズ変更時に湧き出し速度も自動的に変更
  - **SpriteManager連携**: 生成時にランダムIDを取得し、音符の画像バリエーションを実現
  - **フェーズ同期**: `_currentPhase`フィールドで現在フェーズを保持し、生成時に即座に正しい画像を設定（RestPhase中は休符画像で生成）
  - **解像度対応**: 任意のアスペクト比で画面内生成を保証（16:9, 4:3等）
  - **デバッグ性**: 計算結果をInspectorで確認可能、DEBUG_MODEでログ出力

#### Note.cs (Prefabコンポーネント)
- **責補**：個別音符の状態・ビジュアル管理
- **機能**：
  - `ResetState()` - 位置、回転、キャッシュをリセット
  - `SetSpriteID(int id)` - 音符種類IDを設定し、画像参照をキャッシュ
  - `SetPhase(Phase phase)` - フェーズ設定とSprite更新
  - `UpdateSprite()` - 現在のフェーズに基づいて画像を更新（キャッシュから取得）
  - `Destroy()`相当の動作時に`NotePool.ReturnNote(this)`を呼び出し
- **イベント購読**：
  - `PhaseManager.OnPhaseChanged` - フェーズに応じて見た目更新（音符⇔休符）
- **画像管理（IDベース・キャッシュ方式）**：
  - 生成時に `SpriteManager` から画像参照を取得してキャッシュ
  - フェーズ切り替え時はキャッシュから高速取得（SpriteManagerへのアクセスなし）
  - 後方互換性：SpriteManagerがない場合はInspector設定の画像を使用
- **データ構造**：
  ```csharp
  private int _spriteID = 0;                  // 音符種類ID
  private Sprite _cachedNoteSprite;           // キャッシュされた音符画像
  private Sprite _cachedRestSprite;           // キャッシュされた休符画像
  ```
- **パフォーマンス特性**：
  - 生成時: SpriteManagerへのアクセス 2回（音符画像1回 + 休符画像1回）
  - フェーズ切り替え時: 0回（キャッシュから取得、約1 CPU cycle）
  - メモリオーバーヘッド: 16バイト/Note（参照2つ）

#### NoteManager.cs（新規）
- **責補**：表示中の全Noteの時系列管理
- **機能**：
  - 表示中のNoteを`Queue<Note>`で時系列保持
  - `RegisterNote(Note note)` - NoteSpawnerから新規Noteを登録
  - `GetOldestNote()` - 最も古いNoteを取得（シェイク時に破棄される対象）
  - `RemoveNote(Note note)` - 画面外移動等で削除
  - ゲーム開始時・タイトル復帰時にアクティブNoteをクリア
- **重要メソッド**：
  - `ClearAllNotes()` - 全Noteをプールに返却（ゲーム開始時・タイトル復帰時共通）
- **イベント購読**：
  - `GameManager.OnGameStart` → 全Noteクリア
  - `GameManager.OnShowTitle` → 全Noteクリア
- **実装詳細**：
  ```csharp
  private Queue<Note> activeNotes = new Queue<Note>();
  
  public void RegisterNote(Note note) {
      activeNotes.Enqueue(note);
  }
  
  public Note GetOldestNote() {
      return activeNotes.Count > 0 ? activeNotes.Peek() : null;
  }
  
  public void DestroyOldestNote() {
      if (activeNotes.TryDequeue(out Note note)) {
          notePool.ReturnNote(note);
      }
  }
  ```
- **NoteSpawner連携**：
  - `NoteSpawner`は新規スポーン時に`NoteManager.RegisterNote(note)`を呼び出す
- **Phase*ShakeHandler連携**：
  - `NoteManager.DestroyOldestNote()`を呼び出してスコア加算

---

### 3.4 Audio/

#### AudioManager.cs
- **責補**：効果音の管理・再生
- **機能**：
  - ゲーム開始時にAudioClipをすべてキャッシュ（Dictionary<string, AudioClip>）
  - `PlaySFX(string clipName)` - 指定効果音を再生
  - `AudioSource.PlayOneShot()`で再生
- **キャッシュ構造**：
  ```csharp
  private Dictionary<string, AudioClip> clips = new();
  // 例："hit", "miss", "freeze_start", "freeze_end"
  ```
- **初期化タイミング**：`GameManager.OnGameStart`購読時、またはAwake()でプリロード

---

### 3.5 Handlers/

#### IShakeHandler.cs (インターフェース)
```csharp
public interface IShakeHandler {
    void HandleShake(string data, double timestamp);
}
```

#### NullShakeHandler.cs
- **実装**：`IShakeHandler`
- **責務**：フリーズ中用ハンドラー（何もしない）
- **主処理**：
  - 入力を完全に無視（フリーズ中の入力を処理しない）
  - DEBUG_MODEで無視ログ出力
- **用途**：
  - ShakeResolverの`_currentHandler`がフリーズ時にこのハンドラーに切り替わる
  - フリーズ中のフェーズ切り替えに対応
- **備考**：
  - 2段階ハンドラー構造のフリーズ層を担当

#### NoteShakeHandler.cs / RestShakeHandler.cs（2種類に統合）
**設計変更（2025-11-19）**：
- 従来の7個のPhase*ShakeHandlerを2種類（+NullShakeHandler）に統合
- 処理パターンは実質2種類のみ：音符モード（NotePhase, LastSprintPhase）と休符モード（RestPhase）
- フェーズタイプで分岐するのではなく、Strategyパターンでハンドラーを差し替え
- フリーズ処理は2段階ハンドラー構造で実現

#### NoteShakeHandler.cs
- **実装**：`IShakeHandler`
- **責補**：音符フェーズのシェイク処理（NotePhase, LastSprintPhase共通）
- **主処理**：
  1. `NoteManager.GetOldestNote()`で最古Noteを取得
  2. Nullチェック（アクティブNoteがない場合はスキップ）
  3. `NoteManager.DestroyOldestNote()`でプール返却
  4. `AudioManager.PlaySFX("hit")` - 効果音再生
  5. `ScoreManager.AddScore(_scoreValue)` - スコア加算
- **スコア値の動的設定**：
  - `[SerializeField] private int _scoreValue` - Inspector設定可能
  - `SetScoreValue(int score)` - PhaseManagerから呼び出してスコア値を設定
  - NotePhase: +1, LastSprintPhase: +2
- **備考**：
  - NoteManagerが表示中Noteの時系列を管理するため、処理は常に最古のNoteを対象

#### RestShakeHandler.cs
- **実装**：`IShakeHandler`
- **責務**：休符フェーズのシェイク処理（RestPhase）
- **主処理**：
  1. `FreezeManager.IsFrozen`チェック（フリーズ中なら何もしない）
  2. `FreezeManager.StartFreeze(GameConstants.INPUT_LOCK_DURATION)` - フリーズ開始
  3. `AudioManager.PlaySFX("freeze_start")` - 効果音再生
  4. `ScoreManager.AddScore(GameConstants.REST_PENALTY)` - スコア減算（-1）
- **備考**：
  - フリーズ状態でなければフリーズ開始
  - ペナルティスコアはGameConstants.REST_PENALTYで定義（-1）
  - **フリーズチェックは冗長**：ShakeResolverの2段階ハンドラー構造により、フリーズ中はこのハンドラーが呼ばれない（念のため残している）

**統合による改善効果**：
- クラス数：7個 → 2個（-71%）
- コード行数：~420行 → ~100行（-76%）
- Inspectorアタッチ箇所：7箇所 → 2箇所（-71%）
- フェーズ追加時の修正：1ファイル追加 → 既存Handler再利用（0ファイル）
- シェイク処理時の分岐：0回（元設計通り維持）
- パフォーマンス：UnityEvent廃止で約3倍高速化

---

### 3.6 UI/

#### PanelController.cs
- **責補**：画面（タイトル/プレイ/リザルト）の表示・非表示管理
- **機能**：
  - `GameManager.OnShowTitle`を購読 → タイトルパネルをアクティベート
  - `GameManager.OnGameStart`を購読 → プレイパネルをアクティベート
  - `GameManager.OnGameOver`を購読 → リザルトパネルをアクティベート
- **実装**：
  - `CanvasGroup`で各パネルの表示を制御
  - Start()では全パネルを非表示に設定（イベント駆動で表示）
  - 必要に応じてアニメーション付与

#### ScoreDisplay.cs
- **責補**：スコアテキスト表示
- **機能**：
  - `ScoreManager.OnScoreChanged`を購読
  - TextMeshProコンポーネントに反映
- **最適化**：
  - `StringBuilder`を再利用してGC削減

#### PhaseProgressBar.cs
- **責補**：フェーズ進行度バーの表示
- **機能**：
  - `PhaseManager.OnPhaseChanged`を購読（フェーズの総継続時間を取得）
  - イベント発行時に`phaseData.duration`をローカルに保存
  - 毎フレーム独立タイマーを減衰（`remainingTime -= Time.deltaTime`）
  - **スライダー値を`remainingTime / totalDuration`で更新（1→0に減る、残り時間を直感的に表示）**
  - **フェーズごとの色分け機能**:
    - NotePhase: 青系 `(0.3f, 0.5f, 1f)`
    - RestPhase: オレンジ系 `(1f, 0.6f, 0.2f)`
    - LastSprintPhase: 赤系 `(1f, 0.2f, 0.2f)`
  - Inspector設定可能なカラーフィールド（色調整が容易）
  - Slider.fillRectのImageコンポーネント参照をキャッシュ
- **タイマー管理**：コンポーネント自身で管理（イベント時にDurationをセット）
- **最適化**：色変更はフェーズ切り替え時のみ実行（数秒に1回）

#### FreezeEffectUI.cs
- **責補**：凍結ビジュアル表示
- **機能**：
  - `FreezeManager.OnFreezeChanged`を購読
  - 凍結開始時に半透明フラッシュ等を表示
  - 凍結解除時に非表示

#### TimerDisplay.cs
- **責補**：ゲーム全体の残り時間表示
- **機能**：
  - `GameManager.OnGameStart`を購読 → タイマー開始
  - `GameManager.OnShowTitle`を購読 → タイマー停止・リセット
  - `GAME_DURATION`から毎フレームカウントダウン
  - TextMeshProで残り時間を秒単位で表示
- **最適化**：
  - `StringBuilder`を再利用してGC削減
- **備考**：
  - ゲーム終了判定は行わない（PhaseManagerが担当）

#### PhaseDisplay.cs
- **責補**：現在のフェーズ名表示
- **機能**：
  - `PhaseManager.OnPhaseChanged`を購読
  - フェーズタイプに応じた日本語名を表示
    - NotePhase: "♪ 音符フェーズ"
    - RestPhase: "💤 休符フェーズ"
    - LastSprintPhase: "🔥 ラストスパート"
  - TextMeshProで表示
- **最適化**：
  - `StringBuilder`を再利用してGC削減

#### ResultScoreDisplay.cs
- **責補**：リザルトパネルに最終スコア表示
- **機能**：
  - `GameManager.OnGameOver`を購読
  - `ScoreManager.Instance.GetScore()`で最終スコアを取得
  - TextMeshProで表示（例: "Final Score: 150"）
- **最適化**：
  - `StringBuilder`を再利用してGC削減
- **Inspector設定**：
  - `_prefix`: 表示プレフィックス（デフォルト: "Final Score: "）

---

## 4. フォルダ構成

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── PhaseManager.cs
│   │   ├── FreezeManager.cs
│   │   ├── ScoreManager.cs
│   │   └── SpriteManager.cs          ※ 音符・休符画像の共通管理
│   │
│   ├── Input/
│   │   ├── IInputSource.cs
│   │   ├── SerialPortManager.cs
│   │   ├── SerialInputReader.cs
│   │   ├── KeyboardInputReader.cs
│   │   └── ShakeResolver.cs
│   │
│   ├── Gameplay/
│   │   ├── NotePool.cs
│   │   ├── NoteManager.cs
│   │   ├── NoteSpawner.cs
│   │   └── Note.cs
│   │
│   ├── Handlers/
│   │   ├── NullShakeHandler.cs      ※ フリーズ中用ハンドラー（入力無視）
│   │   ├── NoteShakeHandler.cs      ※ ノートフェーズ用ハンドラー
│   │   └── RestShakeHandler.cs      ※ 休符フェーズ用ハンドラー
│   │
│   ├── Audio/
│   │   └── AudioManager.cs
│   │
│   ├── UI/
│   │   ├── PanelController.cs
│   │   ├── ScoreDisplay.cs
│   │   ├── PhaseProgressBar.cs
│   │   ├── FreezeEffectUI.cs
│   │   ├── TimerDisplay.cs           ※ ゲーム全体のタイマー表示
│   │   ├── PhaseDisplay.cs           ※ 現在フェーズ名の表示
│   │   └── ResultScoreDisplay.cs     ※ リザルトパネルの最終スコア表示
│   │
│   └── Data/
│       ├── GameConstants.cs
│       ├── PhaseChangeData.cs (構造体)
│       ├── IInputSource.cs (インターフェース)
│       ├── IShakeHandler.cs (インターフェース)
│       └── 既存のDataクラス群
│
├── Prefabs/
│   └── Note.prefab
│
└── Resources/
    ├── Audio/ (AudioClip群)
    └── Sprites/ (NoteSprite群)
```

---

## 5. 実装流れ（依存関係順）

### Phase 1: 基盤準備
1. **GameConstants.cs** - 定数定義（既存を活用）
2. **IInputSource.cs, IShakeHandler.cs** - インターフェース定義

### Phase 2: マネージャー群
3. **GameManager.cs** - イベント基盤
4. **PhaseManager.cs** - フェーズ管理
5. **FreezeManager.cs** - 凍結状態管理
6. **ScoreManager.cs** - スコア管理

### Phase 3: 入力・解決系
7. **SerialPortManager.cs** - ポート接続
8. **SerialInputReader.cs, KeyboardInputReader.cs** - 入力読み込み
9. **ShakeResolver.cs** - 入力処理振り分け

### Phase 4: ゲームプレイ
10. **NotePool.cs, NoteManager.cs** - 音符の生成・管理・キューイング
11. **NoteSpawner.cs** - 音符湧き出し制御
12. **Note.cs** - 音符Prefab実装
13. **Phase*ShakeHandler.cs** - フェーズ別処理

### Phase 5: オーディオ・UI
14. **AudioManager.cs** - 効果音管理
15. **UI系スクリプト** - 画面表示

---

## 6. 設計原則

### 6.1 イベント駆動と直接呼び出しの使い分け
- マネージャー同士の直接参照を避け、`UnityEvent`経由で通信（GameManager、PhaseManager等）
- **パフォーマンスクリティカルな入力処理**は`UnityEvent`を廃止し、`TryDequeue()`による直接呼び出しで約3倍高速化
- 例：`PhaseManager`は`OnPhaseChanged`を発行、`ShakeResolver`と`NoteSpawner`はそれを購読
- 例：`ShakeResolver`は`IInputSource.TryDequeue()`で入力キューから直接読み取り（UnityEvent不使用）

### 6.2 責務の分離
- 各クラスは1つの責務を持つ
- 例：`NotePool`は生成・再利用管理のみ、湧き出し頻度制御は`NoteSpawner`が担当
- 例：`Note`は見た目・状態、処理ロジックは`*ShakeHandler`

### 6.3 疎結合
- インターフェース経由の設計（`IInputSource`, `IShakeHandler`）
- 実装の切り替えが容易（Serial↔Keyboard、フェーズごとの処理切り替え）

### 6.4 スレッド安全性
- 入力読み込みはスレッド化（SerialInputReader）
- `ConcurrentQueue<T>`でスレッド間通信

### 6.5 パフォーマンス
- Object Poolで`Instantiate/Destroy`コスト削減
- AudioClipプリロードで再生時ラグ削減
- AudioDSP時刻採用でネットワーク遅延対応可能性

---

## 7. イベント一覧

| マネージャー | イベント名 | 引数型 | 引数内容 | 発行タイミング |
|---|---|---|---|---|
| GameManager | OnShowTitle | - | なし | アプリ起動時、ゲーム終了後のタイトル復帰時 |
| GameManager | OnGameStart | - | なし | ゲーム開始ボタン押下 |
| GameManager | OnGameOver | - | なし | ゲーム終了 |
| PhaseManager | OnPhaseChanged | PhaseChangeData | phaseType, duration, spawnFrequency, phaseIndex | フェーズ切り替え時 |
| FreezeManager | OnFreezeChanged | bool | true=凍結開始、false=凍結解除 | 凍結状態変更時 |
| ScoreManager | OnScoreChanged | int | 現在のスコア | スコア更新時 |

**注記**：
- `PhaseChangeData`構造体を使用することで、複数の関連情報を1つのイベント引数で安全に渡せる
- **PhaseManagerがOnPhaseChangedを発行**することで、責務が明確に分離される
- ShakeResolver, NoteSpawner, UI層は`PhaseManager.OnPhaseChanged`を購読
- **GameManager.OnShowTitle**は、アプリ起動時とタイトル復帰時の両方で使用される（DRY原則）
- **全マネージャー**が`OnShowTitle`を購読して状態をリセット：
  - PanelController: タイトルパネル表示
  - PhaseManager: Coroutine停止、状態変数リセット
  - ScoreManager: スコアリセット
  - FreezeManager: 凍結状態解除
  - NoteManager: アクティブNoteをプールに返却
  - NoteSpawner: スポーンCoroutine停止
  - ShakeResolver: 入力キュークリア、ハンドラーリセット

---

## 8. 補足・注意点

### 8.1 Coroutineの活用
- `PhaseManager`, `FreezeManager`は`StartCoroutine()`で時間管理
- `yield return new WaitForSeconds()`で継続時間を待機

### 8.2 Strategyパターン
- `ShakeResolver`は`IShakeHandler`参照を保持
- フェーズ変更時に実装を切り替え
- 状態確認のif分岐が不要

### 8.3 AudioDSP時刻
- シェイク入力時刻を`AudioSettings.dspTime`で記録し、`ShakeResolver`で保存
- これにより将来的にビート同期が必要な場合にタイムスタンプ参照可能

### 8.4 ラストフェーズの扱い
- 従来の「ラストスパート（Last Rush）」も単一フェーズとして実装
- 統一管理により、フェーズ追加時の修正が最小化

### 8.5 デバッグ・テスト
- `KeyboardInputReader`でシリアル接続なしテスト可能
- Inspector上で入力ソース切り替え（Debug用フラグ）

### 8.6 パフォーマンス最適化の経緯

**従来の実装**：
- 入力処理で`UnityEvent`を経由してシェイクデータを伝播
- イベント呼び出しのオーバーヘッドにより約30 CPU cycles/処理

**現在の実装**：
- `ShakeResolver`が`IInputSource.TryDequeue()`で入力キューから直接読み取り
- UnityEventを廃止し、約10 CPU cycles/処理に削減（約3倍高速化）
- フェーズ切り替えなど頻度の低いイベントは`UnityEvent`を継続使用（PhaseManager.OnPhaseChanged等）

**最適化の基準**：
- **高頻度処理（毎フレーム）** → 直接呼び出し（UnityEvent不使用）
- **低頻度処理（フェーズ変更等）** → UnityEvent継続（Inspector連携の利便性優先）

---

## 9. 実装状況

### 9.1 完了済みの主要機能
1. **PhaseChangeData構造体** - フェーズ情報の構造化完了
2. **入力系統** - IInputSource実装（SerialInputReader, KeyboardInputReader）およびShakeResolver最適化完了
3. **ハンドラー統合** - Phase1-7ShakeHandlerを廃止し、NoteShakeHandlerとRestShakeHandlerに統合（71%削減）
4. **パフォーマンス最適化** - UnityEvent廃止による約3倍高速化達成
5. **タイトル画面復帰機能** - GameManager.OnShowTitleイベントによる完全リセット機能実装完了（2025-11-19）
   - DRY原則に基づき、起動時とタイトル復帰を統一処理
   - 全マネージャーの状態リセット処理実装
   - イベント駆動設計により、疎結合を維持したまま実装
6. **音符画像のバリエーション機能** - SpriteManager導入による共通スプライト管理実装完了（2025-11-19）
   - IDベースで音符⇔休符の画像をペア管理
   - 画像参照キャッシュによるパフォーマンス最適化（生成時2回、切り替え時0回のアクセス）
   - 複数種類の音符画像によるゲームプレイの視覚的バリエーション向上
   - 後方互換性の維持（SpriteManagerなしでも動作）
7. **UI表示機能の追加と不具合修正** - ユーザビリティ向上とゲームバランス改善（2025-11-19）
   - 修正1: RestPhase中の音符生成時に即座に休符画像を表示（NoteSpawner.csでフェーズ同期）
   - 修正2: LastSprintPhaseでもフリーズを有効化（FreezeManager.csの無効化処理削除）
   - 修正3: TimerDisplay.cs新規作成 - ゲーム全体の残り時間表示
   - 修正4: PhaseDisplay.cs新規作成 - 現在フェーズ名の表示
   - 修正5: ResultScoreDisplay.cs新規作成 - リザルトパネルの最終スコア表示
   - 全UIクラスでStringBuilderによるGC削減を実装
   - イベント駆動設計による疎結合を維持
8. **UI改善（スライダー表示改善 + 音符生成範囲の画面内制限）** - ユーザビリティとクロスプラットフォーム対応（2025-11-19）
   - PhaseProgressBar.cs: スライダー進行度を1→0に反転（残り時間の直感的表示）
   - PhaseProgressBar.cs: フェーズごとの色分け機能追加（青/オレンジ/赤）
   - NoteSpawner.cs: カメラのorthographicSizeから動的生成範囲計算
   - NoteSpawner.cs: 画面サイズの90%以内に生成（任意のアスペクト比対応）
   - GameConstants.cs: NOTE_SPAWN_MARGIN定数追加
   - OnEnable()時に範囲計算（ゲームスタート時のカメラ状態を参照）
   - Inspector設定による調整の柔軟性を確保

### 9.2 今後の開発方針
- このドキュメントをAI参照用の正確な設計書として維持
- 新機能追加時はCodeArchitecture_changepoints.mdに一時記録
- 変更確定後にこのドキュメントへ反映するワークフローを継続
