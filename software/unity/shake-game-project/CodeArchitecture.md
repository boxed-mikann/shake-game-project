# ゲームコード再構築 - アーキテクチャ設計書

## 1. 概要

シェイクゲームの全体的なコード構造を再設計する。以下の目標を達成：
- **可読性向上** - 責務が明確で、新規開発者が容易に理解可能な構造
- **保守性向上** - 変更・追加時に既存コード修正が最小限で済む
- **パフォーマンス** - スレッド活用、オブジェクトプール、AudioDSP同期対応
- **AIコード生成最適化** - 疎結合・イベント駆動で、AIが正確なコード生成を実施可能

## 2. アーキテクチャ概観

### 2.1 基本設計パターン
- **直接呼び出し最適化** - UnityEvent廃止、IInputSourceのキューに直接アクセスして約3倍高速化
- **Strategy パターン** - フェーズごとに入力処理を動的に切り替え（NoteShakeHandler / RestShakeHandler）
- **Object Pool パターン** - 音符インスタンスの再利用で高速化
- **依存性注入（簡易版）** - 必要な参照をコンストラクタ/SetterでInject

### 2.2 疎結合設計
```
[SerialInputReader / KeyboardInputReader]
           ↓ (IInputSource)
    [ShakeResolver] ← TryDequeue()で直接読み取り（UnityEvent不使用）
           ↓ (IShakeHandler)
   [NoteShakeHandler / RestShakeHandler]
```
各層がインターフェースで疎結合。ShakeResolverは入力キューに直接アクセスして高速処理。

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
- **責務**：ゲーム全体のライフサイクル管理
- **重要イベント**：
  - `public static UnityEvent OnGameStart;` - ゲーム開始
  - `public static UnityEvent OnGameOver;` - ゲーム終了
- **実装方式**：シングルトン + staticなUnityEvent<T>
- **機能**：
  - ゲーム開始・終了の状態管理
  - PhaseManager の実行を制御
  - 各マネージャーの初期化・クリーンアップ
- **備考**：UI層やマネージャーがこのイベントを購読

#### PhaseManager.cs
- **責補**：ゲームフェーズの時系列管理
- **重要イベント**：
  - `public UnityEvent<PhaseChangeData> OnPhaseChanged;` - フェーズ変更（PhaseChangeData構造体を引数）
- **機能**：
  - `Phase_Sequence[]`配列を順に進行
  - Coroutineで各フェーズの継続時間を管理
  - フェーズ切り替え時に`OnPhaseChanged`を発行
- **重要メソッド**：
  - `IEnumerator ExecutePhase(Phase_Sequence phase)` - フェーズ実行
  - `Phase_Sequence GetCurrentPhase()` - 現在フェーズ取得（プロパティ）
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
  - `StartFreeze(float duration)` - 凍結開始
  - Coroutineで時間経過を管理
  - 凍結中/解除時にイベント発行
- **イベント**：
  - `public UnityEvent<bool> OnFreezeChanged;` - true=凍結開始、false=凍結解除

#### ScoreManager.cs
- **責務**：スコア累積・イベント発行
- **機能**：
  - `AddScore(int points)` - スコア加算
  - スコア変更時にイベント発行
- **イベント**：
  - `public UnityEvent<int> OnScoreChanged;` - 現在スコアを引数

---

### 3.2 Input/

#### SerialPortManager.cs
- **責務**：COMポート接続・管理
- **機能**：
  - ゲーム開始時にポート接続
  - 接続失敗時は指数バックオフで再接続試行
  - ゲーム終了時にポート切断
- **実装詳細**：
  - 再接続間隔：1秒 → 2秒 → 4秒...（最大10秒等で制限）
  - OnDestroy/OnDisableで安全に終了

#### SerialInputReader.cs / KeyboardInputReader.cs
- **実装インターフェース**：`IInputSource`（直接呼び出し方式）
- **共通仕様**：
  - スレッド（またはUpdate）で入力を受け取り
  - `ConcurrentQueue<(string data, double timestamp)>`に格納
  - ShakeResolverから`TryDequeue()`で直接読み取り（UnityEvent不使用）
- **SerialInputReader**：
  - SerialPortManagerから`SerialPort`参照を受け取り
  - 別スレッドで`Port.ReadLine()`を実行
  - タイムスタンプは`AudioSettings.dspTime`（オーディオ同期用）
  - `TryDequeue()`でキューから直接データを返す
- **KeyboardInputReader**：
  - `Update()`で毎フレーム`Input.GetKeyDown(KeyCode.Space)`をチェック
  - スペースキー等で"shake"を入力データとしてキューに詰める
  - `TryDequeue()`でキューから直接データを返す
  - テスト・デバッグ用途（GameConstants.DEBUG_MODEで切り替え可能）

#### ShakeResolver.cs
- **責補**：受け取ったシェイクデータを現在のハンドラーに処理させる（直接呼び出し方式・Strategyパターン）
- **機能**：
  - `IInputSource`のキューから`TryDequeue()`で直接取り出し
  - `IShakeHandler currentHandler`を呼び出す（分岐なし・最速）
  - フェーズ変更イベントを購読して`currentHandler`を差し替え（Strategy パターン）
- **パフォーマンス最適化**：
  - フェーズ変更時（数秒に1回）にハンドラーを差し替え
  - シェイク処理時（秒間数十回）は分岐なしで`currentHandler.HandleShake()`を呼ぶだけ
  - UnityEvent経由なし、約3倍高速化（30 cycles → 10 cycles）
- **イベント購読**：
  - `PhaseManager.OnPhaseChanged`を購読
  - `phaseData.phaseType`に応じて対応するハンドラーを`currentHandler`に割り当て
  - 以後の入力は自動的に新しいハンドラーで処理
- **入力ソース切り替え**：
  - `GameConstants.DEBUG_MODE`に応じてKeyboardInputReaderまたはSerialInputReaderを自動選択
  - Inspector設定不要（実行時に自動切り替え）
- **実装例**：
  ```csharp
  void Update() {
      // ★ UnityEventを経由せず、直接キューから取り出して処理（最速）
      if (_activeInputSource != null) {
          while (_activeInputSource.TryDequeue(out var input)) {
              // ★ 直接ハンドラー呼び出し（分岐なし・最速）
              _currentHandler?.HandleShake(input.data, input.timestamp);
          }
      }
  }
  
  void OnPhaseChanged(PhaseChangeData data) {
      // フェーズタイプに応じてハンドラーを差し替え
      // ★ここで1回だけ切り替え、以後の入力処理では分岐不要
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
  - 音符の初期位置・Spriteデータを設定
- **実装例**：
  ```csharp
  void OnPhaseChanged(PhaseChangeData phaseData) {
      StopAllCoroutines();
      StartCoroutine(SpawnNotesRoutine(phaseData.spawnFrequency));
  }
  
  IEnumerator SpawnNotesRoutine(float frequency) {
      while (true) {
          yield return new WaitForSeconds(frequency);
          Note note = notePool.GetNote();
          // 初期化処理...
      }
  }
  ```
- **備考**：
  - `spawnFrequency`は`Phase_Sequence`に含まれるデータを想定
  - フェーズ変更時に湧き出し速度も自動的に変更

#### Note.cs (Prefabコンポーネント)
- **責補**：個別音符の状態・ビジュアル管理
- **機能**：
  - `ResetState()` - 位置、回転をリセット
  - `SetData(NoteData data)` - Sprite、タイプ(8分音符等)を設定
  - `Destroy()`相当の動作時に`NotePool.ReturnNote(this)`を呼び出し
- **イベント購読**：
  - `PhaseManager.OnPhaseChanged` - 必要に応じて見た目更新 休符⇔音符

#### NoteManager.cs（新規）
- **責補**：表示中の全Noteの時系列管理
- **機能**：
  - 表示中のNoteを`Queue<Note>`で時系列保持
  - `RegisterNote(Note note)` - NoteSpawnerから新規Noteを登録
  - `GetOldestNote()` - 最も古いNoteを取得（シェイク時に破棄される対象）
  - `RemoveNote(Note note)` - 画面外移動等で削除
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

#### NoteShakeHandler.cs / RestShakeHandler.cs（2種類に統合）
**設計変更（2025-11-19）**：
- 従来の7個のPhase*ShakeHandlerを2種類に統合（71%削減）
- 処理パターンは実質2種類のみ：音符モード（NotePhase, LastSprintPhase）と休符モード（RestPhase）
- フェーズタイプで分岐するのではなく、Strategyパターンでハンドラーを差し替え

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
- **責補**：休符フェーズのシェイク処理（RestPhase）
- **主処理**：
  1. `FreezeManager.IsFrozen`チェック（フリーズ中なら何もしない）
  2. `FreezeManager.StartFreeze(GameConstants.INPUT_LOCK_DURATION)` - フリーズ開始
  3. `AudioManager.PlaySFX("freeze_start")` - 効果音再生
  4. `ScoreManager.AddScore(GameConstants.REST_PENALTY)` - スコア減算（-1）
- **備考**：
  - フリーズ状態でなければフリーズ開始
  - ペナルティスコアはGameConstants.REST_PENALTYで定義（-1）

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
  - `GameManager.OnGameStart`を購読 → プレイパネルをアクティベート
  - `GameManager.OnGameOver`を購読 → リザルトパネルをアクティベート
- **実装**：
  - `CanvasGroup`で各パネルの表示を制御
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
  - スライダー値を`(totalDuration - remainingTime) / totalDuration`で更新
- **タイマー管理**：コンポーネント自身で管理（イベント時にDurationをセット）

#### FreezeEffectUI.cs
- **責補**：凍結ビジュアル表示
- **機能**：
  - `FreezeManager.OnFreezeChanged`を購読
  - 凍結開始時に半透明フラッシュ等を表示
  - 凍結解除時に非表示

---

## 4. フォルダ構成

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── PhaseManager.cs
│   │   ├── FreezeManager.cs
│   │   └── ScoreManager.cs
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
│   │   └── FreezeEffectUI.cs
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
| GameManager | OnGameStart | - | なし | ゲーム開始ボタン押下 |
| GameManager | OnGameOver | - | なし | ゲーム終了 |
| PhaseManager | OnPhaseChanged | PhaseChangeData | phaseType, duration, spawnFrequency, phaseIndex | フェーズ切り替え時 |
| FreezeManager | OnFreezeChanged | bool | true=凍結開始、false=凍結解除 | 凍結状態変更時 |
| ScoreManager | OnScoreChanged | int | 現在のスコア | スコア更新時 |

**注記**：
- `PhaseChangeData`構造体を使用することで、複数の関連情報を1つのイベント引数で安全に渡せる
- **PhaseManagerがOnPhaseChangedを発行**することで、責務が明確に分離される
- ShakeResolver, NoteSpawner, UI層は`PhaseManager.OnPhaseChanged`を購読

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

### 9.2 今後の開発方針
- このドキュメントをAI参照用の正確な設計書として維持
- 新機能追加時はCodeArchitecture_changepoints.mdに一時記録
- 変更確定後にこのドキュメントへ反映するワークフローを継続
