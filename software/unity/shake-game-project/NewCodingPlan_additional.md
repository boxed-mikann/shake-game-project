## ✅ 完了済み項目（2025-11-19）
- ~~大量のハンドラー(シェイク処理は音符時と休符時の2種類でいい)~~ → **完了**: Phase1～7ShakeHandler（7個）を NoteShakeHandler + RestShakeHandler（2個）に統合
- ~~シェイク処理の高速性について検討(イベント駆動は早いのか？)~~ → **完了**: UnityEvent廃止、直接呼び出し方式で約3倍高速化

## ✅ 微小修正項目（完了 - 2025-11-19）
- ~~スライダは減っていくようにする。フェーズの種類によって色を変える。~~ → **完了**: 進行度を1→0に反転、フェーズごとに色分け（青/オレンジ/赤）
- ~~音符の生成範囲を画面内に。~~ → **完了**: カメラのorthographicSizeから動的計算、画面サイズの90%以内に生成

---

## 重要修正項目

### 1. 入力ソースの統一キュー化（DEBUG_MODE不要化）

**現状の問題**：
- ShakeResolver.cs で `GameConstants.DEBUG_MODE` により入力ソースを切り替え（L22-24）
- SerialInputReaderとKeyboardInputReaderが**別々のキュー**を持つ
- 開発時にシリアルとキーボードを同時に使いたい場合に不便
- ShakeResolverが複数キューを監視する必要がある

**修正方針（CodeArchitecture.md設計思想に基づく）**：
1. **統一キューの導入（最もシンプル）**
   - ShakeResolver.csに**単一の共有キュー**を作成
   - SerialInputReaderとKeyboardInputReaderは直接この共有キューに入力を追加
   - IInputSourceインターフェースを廃止（不要になる）

2. **実装詳細**
   ```csharp
   // ShakeResolver.cs
   public class ShakeResolver : MonoBehaviour {
       // ★ 統一された入力キュー（static for shared access）
       private static ConcurrentQueue<(string data, double timestamp)> _sharedInputQueue 
           = new ConcurrentQueue<(string data, double timestamp)>();
       
       // ★ 外部から入力を追加するメソッド
       public static void EnqueueInput(string data, double timestamp) {
           _sharedInputQueue.Enqueue((data, timestamp));
       }
       
       void Update() {
           // ★ シンプル！1つのキューだけチェック
           while (_sharedInputQueue.TryDequeue(out var input)) {
               _currentHandler?.HandleShake(input.data, input.timestamp);
           }
       }
   }
   
   // SerialInputReader.cs / KeyboardInputReader.cs
   // ★ 各InputReaderは直接ShakeResolverのキューに追加
   ShakeResolver.EnqueueInput(data, timestamp);
   ```

3. **メリット**
   - **最もシンプル**：キューが1つだけ、ループも1つだけ
   - DEBUG_MODE定数が完全に不要
   - IInputSourceインターフェースが不要（オーバーエンジニアリング解消）
   - 入力の時系列が自然に保たれる（複数ソースでも順序保証）
   - メモリ効率向上（キューが1つ）

4. **デメリットと対策**
   - デメリット: ShakeResolverへの依存が生じる
   - 対策: staticメソッドなので結合度は低い（DI不要）
   - 対策: 入力層は「キューに入れるだけ」で責務が明確

**修正ファイル**：
- `Assets/Scripts/Input/ShakeResolver.cs`（統一キュー追加）
- `Assets/Scripts/Input/SerialInputReader.cs`（IInputSource廃止、ShakeResolver.EnqueueInput()呼び出し）
- `Assets/Scripts/Input/KeyboardInputReader.cs`（同上）
- `Assets/Scripts/Data/IInputSource.cs`（削除）
- `Assets/Scripts/Data/GameConstants.cs`（DEBUG_MODE削除・非推奨化）

---

### 2. フリーズ処理の2段階ハンドラー切り替え（フリーズ中のフェーズ変更対応）

**現状の問題**：
- KeyboardInputReader.cs の Update()内でフリーズチェック（L73-74）
- SerialInputReader.csにはフリーズチェックが**存在しない**
- **設計上の矛盾**：入力層がゲームロジック（フリーズ）に依存
- **見落とし**：フリーズ中にフェーズが切り替わるケースに未対応

**修正方針（CodeArchitecture.md設計思想に基づく）**：
1. **責務の明確化**
   - **入力層**：常に入力をキューに格納（フリーズ無関係）
   - **処理層**：フリーズ状態を判定して処理をスキップ

2. **2段階ハンドラー構造の導入**
   ```
   [フリーズ層]
   _currentHandler → NullShakeHandler（フリーズ中）
                  → _activeHandler（通常時）
   
   [フェーズ層]
   _activeHandler → NoteShakeHandler（音符フェーズ）
                 → RestShakeHandler（休符フェーズ）
   ```

3. **新クラスの導入**
   - `NullShakeHandler.cs` - フリーズ中用（何もしない）
   - `ActiveShakeHandler.cs` - 通常時用（_activeHandlerへ委譲、名称案: "ActiveShakeHandler", "DelegatingHandler", "ForwardingHandler"）
   
   ```csharp
   // NullShakeHandler.cs
   public class NullShakeHandler : MonoBehaviour, IShakeHandler {
       public void HandleShake(string data, double timestamp) {
           // 何もしない（フリーズ中の入力を無視）
       }
   }
   
   // ActiveShakeHandler.cs（委譲用）
   public class ActiveShakeHandler : MonoBehaviour, IShakeHandler {
       private IShakeHandler _delegateHandler;  // 実際の処理ハンドラー
       
       public void SetDelegate(IShakeHandler handler) {
           _delegateHandler = handler;
       }
       
       public void HandleShake(string data, double timestamp) {
           _delegateHandler?.HandleShake(data, timestamp);
       }
   }
   ```

4. **ShakeResolverの改修**
   ```csharp
   // ShakeResolver.cs
   [SerializeField] private NullShakeHandler _nullHandler;        // フリーズ中用
   [SerializeField] private ActiveShakeHandler _activeHandler;    // 通常時用
   [SerializeField] private NoteShakeHandler _noteHandler;        // 音符処理
   [SerializeField] private RestShakeHandler _restHandler;        // 休符処理
   
   private IShakeHandler _currentHandler;  // Update()で呼ばれる最終ハンドラー
   
   void Start() {
       // 初期状態：通常時ハンドラーを設定
       _currentHandler = _activeHandler;
       
       // イベント購読
       FreezeManager.OnFreezeChanged.AddListener(OnFreezeChanged);
       PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
   }
   
   void OnFreezeChanged(bool isFrozen) {
       // ★ フリーズ層の切り替え（_currentHandler）
       if (isFrozen) {
           _currentHandler = _nullHandler;  // フリーズ中は何もしない
       } else {
           _currentHandler = _activeHandler;  // 通常時は処理を委譲
       }
   }
   
   void OnPhaseChanged(PhaseChangeData data) {
       // ★ フェーズ層の切り替え（_activeHandlerの委譲先）
       switch (data.phaseType) {
           case Phase.NotePhase:
               _activeHandler.SetDelegate(_noteHandler);
               _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
               break;
           case Phase.LastSprintPhase:
               _activeHandler.SetDelegate(_noteHandler);
               _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
               break;
           case Phase.RestPhase:
               _activeHandler.SetDelegate(_restHandler);
               break;
       }
   }
   ```

5. **メリット**
   - **フリーズ中のフェーズ切り替えに完全対応**
   - フリーズ解除時に自動的に正しいフェーズハンドラーで処理再開
   - 入力層とゲームロジックの疎結合化（設計思想に合致）
   - SerialとKeyboardで動作が統一される
   - Strategyパターンの一貫した利用（2段階構造）
   - Update()内の分岐が完全に消える（最速処理維持）

6. **動作例**
   ```
   [通常時 NotePhase]
   _currentHandler = _activeHandler → _noteHandler（音符処理）
   
   [RestPhaseでフリーズ発生]
   _currentHandler = _nullHandler（入力無視）
   _activeHandler → _restHandler（変更なし、待機中）
   
   [フリーズ中に NotePhase に切り替わり]
   _currentHandler = _nullHandler（入力無視継続）
   _activeHandler → _noteHandler（★ 委譲先だけ変更）
   
   [フリーズ解除]
   _currentHandler = _activeHandler → _noteHandler（★ 自動的に音符処理開始）
   ```

**修正ファイル**：
- `Assets/Scripts/Handlers/NullShakeHandler.cs`（新規作成）
- `Assets/Scripts/Handlers/ActiveShakeHandler.cs`（新規作成、名称要検討）
- `Assets/Scripts/Input/ShakeResolver.cs`（2段階ハンドラー実装）
- `Assets/Scripts/Input/KeyboardInputReader.cs`（フリーズチェック削除）

---

### 3. SerialPort.ReadLine()のブロッキング特性活用

**現状の問題**：
- SerialInputReader.cs のReadThreadLoop()でポーリング形式
- SerialPortManager.ReadLine()を繰り返し呼び出し（L94）
- 接続待ちでThread.Sleep(100)（L104）
- SerialPortManager.cs で ReadTimeout=100ms 設定（L101）

**調査結果**：
- `SerialPort.ReadLine()`はデータ到着まで**ブロッキング**する仕様
- ただし現状はReadTimeout=100msで早期タイムアウト設定済み
- タイムアウトすると`TimeoutException`が発生

**修正方針（CodeArchitecture.md設計思想に基づく）**：
1. **ReadTimeoutを無制限に変更**
   ```csharp
   // SerialPortManager.cs Connect()
   _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;  // ブロッキング待機
   ```

2. **例外処理の最適化**
   - TimeoutExceptionのcatch処理が不要になる
   - データ到着まで自然にブロック（CPU使用率削減）

3. **スレッド停止の安全性確保**
   ```csharp
   // SerialInputReader.cs Disconnect()
   public void Disconnect() {
       _isRunning = false;
       
       // ReadLine()のブロックを解除するためポート切断
       if (SerialPortManager.Instance != null) {
           SerialPortManager.Instance.Disconnect();
       }
       
       if (_readThread != null && _readThread.IsAlive) {
           _readThread.Join(2000);  // 最大2秒待機（余裕を持たせる）
       }
   }
   ```

4. **メリット**
   - **CPU使用率の大幅削減**（ポーリング→イベント駆動、待機中はCPU 0%）
   - **Thread.Sleep(100)の削除**：現在は100msごとにポーリング、これが完全に不要に
   - **入力レイテンシの劇的改善**：最大100ms遅延 → 即座に処理（データ到着と同時）
   - **コードの簡潔化**（接続待機の条件分岐削減、例外処理の簡素化）
   - **ラグの大幅削減**：Thread.Sleep(100)が最大の遅延原因だった可能性が高い

5. **ラグの原因分析（重要）**
   - **現在の最大遅延**: ReadTimeout(100ms) + Thread.Sleep(100ms) = **最大200ms**
   - **修正後の遅延**: ブロッキング待機 = **0ms（データ到着即処理）**
   - これが体感できるラグの主要因である可能性が非常に高い
   - シリアル通信自体は高速（ボーレート依存、通常1ms未満）
   - ポーリング間隔が全体のレイテンシを支配していた

6. **注意点**
   - ReadLine()がブロック中にDisconnect()を呼ぶとスレッド終了に時間がかかる
   - SerialPortManager.Disconnect()を先に呼んでブロック解除が必要
   - Join()のタイムアウトを十分に長く設定（2000ms推奨）

**修正ファイル**：
- `Assets/Scripts/Input/SerialPortManager.cs`
- `Assets/Scripts/Input/SerialInputReader.cs`

---

## 重要修正項目（原文保持）
1. DEBUGモード関係なく、入力はシリアル通信、キーボードが両方とも受け取れるようにする。IInputSorcesとか不要。→コードを簡潔にする。
2. 現在フリーズ時の入力停止はKeyboadInputReaderが行っており、シリアルポート入力の方はフリーズしないようになっている。フリーズはShakeResolverあたりが行うようにする(何もしないHandlerに差し替えるとか。)

3. SerialInputReaderのスレッドではSerialPortManagerのReadLineを呼び出す形となっていて、ポーリングになっていると思う。もとの_serialPort.ReadLine()はデータが来るまで待つ処理らしい←要調査。なのでその特性を生かして、処理量を少なく、処理速度を上げられないか？

## 重要検討項目
- ランキング、ハイスコア、最高記録表示など
- 音符がはじけるときのエフェクト
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