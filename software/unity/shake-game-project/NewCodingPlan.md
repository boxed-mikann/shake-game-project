# コード再構築計画
コードの構造を丸々考え直す。
### 動機
- より理解しやすいコードにし、メンテナンス性・カスタマイズ性を上げる。
- Serial通信関連やObjectPoolなどの高速性・音同期性向上策を適用する。
- AIによるコード生成速度を上げる。

### 各スクリプトと役割

#### GameConstants.cs
- そのまま使えそう。

#### シリアル入力受け取るやつ(名称要検討)
  **命名提案**: `SerialInputReader.cs` または `SerialInputListener.cs`
  **補足**: OnDestroy or OnDisableで`keepReading = false`とスレッド終了処理を忘れずに。スレッド終了時は`thread.Join(timeout)`で待機。

- スレッドを使ってフレームに依存せず、入力を受け取る。
- Port.ReadLineは入力があるまで待つ処理のようなので、別スレッドにすることでポーリング(定期的なチェック)を減らせる。
- 入力のデータは整形してキューに入れる。
- ロックしないタイプのキューがいいらしい←なぜ？
  **回答**: `ConcurrentQueue<T>`はロックフリーな内部実装で、高頻度のスレッド間通信でのロック競合を避けられるため、レイテンシが低い。
- 以下参考コード(データの整形は入ってない)
```
// スレッドで受信
void ReadSerial() {
    while (keepReading) {
        string data = port.ReadLine();
        double timestamp = AudioSettings.dspTime;
        queue.Enqueue((data, timestamp));
    }
}

// メインスレッドで反映
void Update() {
    while (queue.TryDequeue(out var input)) {
        ProcessInput(input.data, input.timestamp);
    }
}
```
- データ形式は既存のものを参照+タイムスタンプを追加(将来のため)

#### ポート接続をするやつ(名称検討)
  **命名提案**: `SerialPortManager.cs` または `PortConnector.cs`
  **補足**: 接続処理と入力受け取りを分離。このクラスはポートOPEN/CLOSE・再接続ロジック担当。`SerialInputReader`はこのクラスから`SerialPort`参照を受け取り、読み込みのみに集中。指数バックオフ(最初短い、だんだん長い間隔)で再接続試行回数を制限するとUX向上。

- COMポートの接続を行う。
- 接続できていない場合は、数秒間隔で再接続を試みる。

#### 受け取ったシェイクデータを処理するやつ。(名称要検討ShakeResolver.csとか？)
  **命名提案**: `ShakeResolver.cs` は良好。複数フェーズ対応なら`IShakeHandler`インターフェースを使い、フェーズごとの実装切り替えが効果的。
  
  **IShakeHandlerの説明**:
  ```csharp
  public interface IShakeHandler {
      void HandleShake(string data, double timestamp);
  }
  ```
  例：`Phase1ShakeHandler`, `Phase2ShakeHandler`など、各フェーズごとにこのインターフェースを実装。ShakeResolverは現在のハンドラー参照を持ち、`currentHandler.HandleShake(data, timestamp)`を呼び出すだけ。フェーズ変更時に`currentHandler`を切り替える。新フェーズ追加時、既存のShakeResolver.csは修正不要で、新しいハンドラー実装クラスを追加するだけで済む。

- シリアル入力受け取るやつでキューに入れたデータをメインスレッドで読みだして処理する役割。
- コード例でいうとProcessInputの関数を呼び出す。名前はHandleShakeとかにする？
- シリアル通信による入力の代わりに、キーボード入力でテストできるようにする。
  **実装提案**: `IInputSource`インターフェースを作成。`SerialInputReader`と`KeyboardInputReader`の2つが実装。ShakeResolverは実装に依存しない。ビルド時またはInspectorで入力ソース切り替え（Debug用フラグ等）。これにより、シリアル接続なしでも開発テストが容易。
  
  **高速性・IInputSource設計の両立について**：
  - ✅ インターフェースは単なる仮想呼び出しで、オーバーヘッド極小。高速性は保持。
  - ✅ キーボード入力は`Input.GetKey()`（毎フレーム評価）を`KeyboardInputReader`の`Update()`内で呼び、結果をキューに入れる。`OnKey*`関数より高頻度で反応。
  - 実装例：`if(Input.GetKey(KeyCode.Space)) { queue.Enqueue((timestamp, "shake"), AudioSettings.dspTime); }`
  - シリアル入力と同じキュー経由でメインスレッドに渡すため、後続処理は変わらず。

#### ゲーム全体の進行を管理するやつ(GameManager？)
- ゲームスタートや終了、リザルト表示、タイトル画面表示などのイベントを発行する。
  **補足**: シングルトンパターンで実装可。重要なイベント（OnGameStart, OnGameOver, OnPhaseChanged等）はstaticなUnityEventで定義すると、各マネージャーが容易に購読できる。

#### ゲーム内のフェーズを管理するやつ(PhaseManager？)
  **回答**: `Phase_Sequence`にDurationを保持し、Coroutineで管理。`yield return new WaitForSeconds(duration)`を使用。現在フェーズはプロパティで読み取り可能にする。

- ゲームフェーズデータ(Phase_Sequence)(既存)を使って、Coroutine(?)で状態管理、イベント発行
- yield return new WaitForSeconds(duration)　を使う？
- 必要なら、現在のフェーズを示す変数を用意する？

#### 凍結状態を管理するやつ。
  **回答**: PhaseManagerと同様の構造にし、「状態管理の基底クラス」を共通化するか、別の専門マネージャー(`FreezeManager`)として独立させる。凍結中はイベント購読側で入力を無視する仕組みが簡潔。
  
  →「状態管理の基底クラス」を採用して、可読性を高められるか？
  **補足**（基底クラス採用の場合）: `StateManager<T>`のような汎用基底クラスで時間管理・イベント発行を共通化。PhaseManagerとFreezeManagerはそれぞれ専用のStateData型を持たせるとDRY原則に従える。

#### シェイクデータのHandleShake関数を配線するやつ
- イベントを購読して、HandleShakeを配線しかえる。
- こうすることで、HandleShakeとかで、現在の状態を読みに行ったりしなくて済む。
  **設計の妥当性**：✅ 妥当。フェーズ変更イベント（PhaseManagerから発行）を購読して、対応するHandleShake実装を切り替える。このパターン（Strategy パターン）により、「状態確認のif分岐」が不要になり、コード可読性と保守性が大幅向上。ハンドラーを動的に差し替えられるため、実行時のフェーズ変更にも対応可能。

#### HandleShakeの中身。各フェーズ用のがある。
- シェイク→音符をはじけさせる。なので、Prefabの返却や効果音の再生、スコアの加算を呼び出す？
- 各フェーズ用のを作って　配線するやつがHandleShakeに配線する。
  **補足**: 各フェーズ用のHandleShake実装は`IPhaseShakeHandler`をimplementして、ファクトリーパターンで切り替えると拡張性が上がる。新フェーズ追加時の既存コード修正を最小化。

#### 効果音の管理をするやつ。
- 効果音を事前にロードしておくことで、再生時のラグを減らしたい
  **補足**: `AudioClip`キャッシング用に辞書を用意（key: クリップ名、value: AudioClip）。再生時は`AudioSource.PlayOneShot(clip)`で。複数フェーズで同じ音を使う場合も辞書経由で統一管理できる。

#### Prefab(音符)管理するやつ。
  **命名提案**: `NotePool.cs` または `NoteObjectPool.cs`

- オブジェクトプールを使うことにより、処理を速く軽くする。
- 音符の湧き出しもここでやる？←ゲーム内のフェーズによって量が変わる。
  **回答**: NotePoolはインスタンス化のみ担当。湧き出し量・タイミングはPhaseManagerから「湧き出し設定データ」をもらい、別の`NoteSpawner.cs`で制御するのが関心の分離につながる。
- 各音符でつかう音符画像を変えたい(8分音符,4分音符とか)ので、「データ（スプライトセット）をScriptableObjectで分離し、オブジェクトプールから取り出してアクティベート時に差し替える」といいらしい。
- GPTによると、注意点は、共有参照を使う（同じSpriteを複数インスタンスで共有）。プールから取り出す際は状態リセット。
  **補足**: 状態リセットは`Note`コンポーネントの`ResetState()`メソッドで：位置・回転・スケール・アニメーション状態・イベント購読状況をすべてリセット。返却時も同様に呼ぶ。
- 音符を休符に変える(Prefabにアタッチするコードでイベントを購読してやった方がいいか？)

- Prefabにアタッチするコードと機能をどう切り分けるか検討。
  **回答**: 「データ変更時のビジュアル反映」はPrefabのコンポーネント側で購読。「スポーン時のデータセット」はPoolから返却時にセッター経由で設定。責務は「見た目の更新」と「データ保持」で分離。

#### Prefabにアタッチするコード

#### スコア管理するやつ。
  **補足**: スコア更新時にイベント発行（`OnScoreChanged`）。UI層はそれを購読して表示を更新。ゲーム終了時にスコア保存（PlayerPrefs or JSON保存）も合わせて検討。

### UI系
UI系は基本イベントを購読して動くようにする。
ボタンは、シンプルにインスペクターでGameStartやリザルト画面などのイベントをInvokeするやつ(GameManagerのやつ？)とかをアタッチする。

#### パネル操作するやつ。
- SetActivateでいいか？
- GameManagerのイベントを購読して、各画面(タイトル・ゲームプレイ・リザルト)の親パネルを表示・日表示する。
  **補足**: `CanvasGroup`を使うとより細かい制御が可能（フェード、レイキャスト無効化）。表示/非表示の切り替え時にAnimationやDOTweenでアニメーション付与も検討。

#### 文字表示するやつ。
- イベントを購読して内容を変える。スコアの表示とかも？

#### スライダ表示するやつ。
  **回答**: フェーズマネージャーから「イベント時点での残り時間」を含めて購読。スライダ側で独立タイマーを持ち、毎フレーム`(remainingTime / totalDuration)`で更新。これなら共通ロジック化も容易。

- フェーズのイベントには継続時間の情報があるので、それをもとにする。
- イベント購読時に色や継続時間を設定し(時間＝継続時間と設定し)、そのあと減っていく処理は別でずっと動く(時間＞0なら時間-deltaTimeして、スライダの割合を時間/継続時間にするとか)というのはどうだろうか？
- つまり、時間は自分で管理する。

#### タイマー表示するやつ。
- GPT曰く「UIは文字列変換のみ。GC削減のためにStringBuilder再利用。」するといいらしい。
  **実装例**: `StringBuilder sb = new StringBuilder();`をキャッシュ。毎フレーム`sb.Clear(); sb.AppendFormat("{0:00}:{1:00}", minutes, seconds);`でテキスト更新。`Text.text = sb.ToString()`で反映。

#### 凍結エフェクト表示するやつ。
- イベントを購読して表示。非表示

### 注意点
- 元のコードはラストスパートを特別処理しているものが多いが、それはなくして一つのフェーズとして統一管理できるようにする。
  **補足**: この統一化により、フェーズ追加時の修正が最小化される。新しい難度や企画変更にも容易に対応可能。
### フォルダの分類
- 要検討 UI, 状態管理系(GameManager,フェーズ管理,凍結管理とか),シリアル通信(スレッドで入力受け取るやつ、将来的には指示送る出力のやつもつくりたい),その他(Game？)

**提案フォルダ構成**:
- `Managers/` - GameManager, PhaseManager, FreezeManager, ScoreManager
- `Input/` - SerialPortManager, SerialInputReader, KeyboardInputReader, ShakeResolver
- `Gameplay/` - NotePool, NoteSpawner, Note（Prefabコンポーネント）
- `Audio/` - AudioManager
- `UI/` - 各UI制御コンポーネント
- `Data/` - ScriptableObject群、GameConstants, IInputSource等インターフェース

### 実装順序の提案
1. **GameConstants & Data定義** - 基盤作成
2. **Managers** - GameManager, PhaseManager, FreezeManager（イベント基盤）
3. **Input系** - SerialInputReader, ShakeResolver
4. **Gameplay系** - NotePool, NoteSpawner
5. **Audio & UI** - AudioManager, UI系コンポーネント

※マネージャー群は相互依存が少なく、イベントで疎結合にすることで、並行実装が容易。