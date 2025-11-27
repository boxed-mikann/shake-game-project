# バージョン2計画（チーム音ゲー）

## 1. 概要

### ゲームコンセプト
- **曲に合わせてチーム全員でシンクロシェイク**
- **シンクロ率でボルテージゲージが上昇**し、盛り上がりを演出
- **音楽＋動画** で臨場感あるビジュアル体験

### プレイ環境
- **プレイヤー**: 最大10人のチーム（イベント会場の班単位）
- **表示**: スクリーンに投影（全員で共有）
- **デバイス**: 
  - **ESP8266**: シェイク検知棒（各プレイヤー）
  - **ESP32**: 信号集約・Unity送信（1台）
  - **Unity PC**: ゲーム本体実行

---

## 2. 技術構成

### 2.1 プロジェクト構造（単純化方針）
- **方針**: 共通コードの抽出をやめ、Version1は既存コードを丸ごと`Version1/`に格納。Version2は新規で実装し、必要時にVersion1のコードを「参照」して再実装。
- **フォルダ構成**:
  ```
  Assets/Scripts/
    ├── Version1/          # 旧ゲームコード（全ファイル格納）
    │   ├── Managers/
    │   ├── Gameplay/
    │   ├── Handlers/
    │   ├── Input/
    │   └── UI/
    └── Version2/          # 新ゲームコード（全ファイル新規作成）
        ├── Managers/      # GameManagerV2, DeviceManager, VoltageManager等
        ├── Input/         # SerialInputReaderV2, KeyboardInputReaderV2
        ├── Gameplay/      # ロジック、判定、エフェクト
        ├── UI/            # スコア、ゲージ、演出表示、PopupPool
        ├── Synchronization/ # シンクロ判定（SyncDetector等）
        └── Music/         # 動画再生、譜面管理
  ```
- **フォルダ操作**: 手動で実施（Version1へ全移動、Version2は空から開始）
- **設計上の前提**: Version2はVersion1を「参照」するがコード共有はしない（コピペまたは再実装）

### 2.2 旧コードの取り扱い方針（参照のみ・共有なし）

| コンポーネント | Version2への扱い | 備考 |
|---|---|---|
| SerialPort関連 | 再実装/必要箇所のみ引用 | V1の設計を参照しつつV2の要件に合わせて新規作成 |
| SerialInputReader | 再実装 | V2もV1と同じく`AudioSettings.dspTime`を使用（スレッドセーフ、高精度） |
| KeyboardInputReader | 再実装 | デバッグ入力をV2仕様（CSV形式、dspTime）で新規作成 |
| AudioManager | 再実装 | SE再生APIはV2用に最低限で新規作成 |
| GameConstants | 再実装 | V2固有定数のみを`GameConstantsV2`として定義 |
| Phase/Note系 | 参照のみ | 必要に応じてアルゴリズムや構造を再設計して実装 |
| EffectPool | 参照のみ | パターンを参考にして`PopupPool`を新規作成 |

#### 原則
- Version2では既存コードと共有しない。必要ならコード断片を参照して「再実装」する。
- 共有を避けることで将来的な仕様分岐の保守コストを削減する。

### 2.3 音楽＋動画再生

#### 動画再生
- **UnityのVideoPlayer** を使用
  - **理由**: 標準コンポーネントで安定性が高い、MP4/WebM対応
  - **代替案検証**:
    - ❌ AVPro Video: 有料（$450）、オーバースペック
    - ❌ MediaPlayer: Android/iOS向け、PC不要
  - **最適解**: VideoPlayerで十分（無料、軽量、PC対応）
- **動画ファイル**: MP4形式（H.264コーデック推奨）
  - **音楽トラック**: VideoPlayerから直接再生（AudioSourceと分離不要）
  - **サイズ対策**: 1.5GB動画への対応
    - ✅ **StreamingAssets配置**: ビルドに含めるがメモリ展開しない
    - ✅ **VideoPlayer.url**: ファイルパス直接指定でストリーミング再生
    - ✅ **解像度最適化**: 1920x1080推奨（4Kは過剰）
    - ⚠️ **圧縮率調整**: ビットレート3-5Mbps程度で品質とサイズのバランス
- **再生タイミング**:
  - 待機画面: `videoPlayer.isLooping = true`
  - ゲーム開始: `videoPlayer.time = 0; videoPlayer.Play();`

#### 音楽同期
- **AudioSettings.dspTime** を使用（高精度オーディオクロック）
  - **理由**: 
    - VideoPlayerは内部でAudioSourceを使用しており、dspTimeと同期している
    - スレッドセーフ（バックグラウンドスレッドで使用可能）
    - 高精度（サンプル単位、44.1kHz = 約0.02ms精度）
    - フレームレート非依存で安定
  - **Version1との共通点**: Version1もdspTimeを使用（実績あり）
- **✅ Version2での実装方針**: **dspTimeに統一**
  - **SerialInputReaderV2**: `AudioSettings.dspTime`でタイムスタンプ記録（Version1と同じ）
  - **InputQueueV2**: `double timestamp`でdspTimeを保持
  - **ゲーム開始時の基準記録**: `gameStartDspTime = AudioSettings.dspTime`
  - **音楽時刻取得**: `AudioSettings.dspTime - gameStartDspTime`
- **判定タイミング取得**:
  ```csharp
  // ゲーム開始時
  private double gameStartDspTime;
  
  void StartGame() {
      gameStartDspTime = AudioSettings.dspTime;
      videoPlayer.time = 0;
      videoPlayer.Play();
  }
  
  // 現在の音楽時刻（VideoPlayer.timeの代替）
  float GetMusicTime() {
      return (float)(AudioSettings.dspTime - gameStartDspTime);
  }
  
  // 入力判定
  void ProcessShake(double shakeTimestamp) {
      float musicTime = GetMusicTime();
      float shakeRelativeTime = (float)(shakeTimestamp - gameStartDspTime);
      float diff = Mathf.Abs(musicTime - shakeRelativeTime);
      // diff < SYNC_WINDOW_SIZE でシンクロ判定
  }
  ```
- **待機画面での入力**: dspTimeで記録し、ゲーム開始後に相対時刻に変換

### 2.4 譜面データ

#### 譜面フォーマット
- **JSON形式** を採用（編集しやすさ、可読性）
- **NoteEditor等の既存ツール** [NoteEditor](https://github.com/setchi/NoteEditor),[参考サイト](https://baba-s.hatenablog.com/entry/2018/04/16/085900#%E3%82%B3%E3%83%9E%E3%83%B3%E3%83%89%E4%B8%80%E8%A6%A7%E5%BC%95%E7%94%A8)
- **データ構造**:
```json
{
    "name": "bgm",
    "maxBlock": 5,
    "BPM": 120,
    "offset": 0,
    "notes": [
        {
            "LPB": 4,
            "num": 12,
            "block": 2,
            "type": 1,
            "notes": []
        },
        {
            "LPB": 4,
            "num": 12,
            "block": 0,
            "type": 1,
            "notes": []
        },
        {
            "LPB": 4,
            "num": 16,
            "block": 1,
            "type": 1,
            "notes": []
        },
        {
            "LPB": 4,
            "num": 16,
            "block": 3,
            "type": 1,
            "notes": []
        },
        {
            "LPB": 4,
            "num": 20,
            "block": 2,
            "type": 2,
            "notes": [
                {
                    "LPB": 4,
                    "num": 24,
                    "block": 2,
                    "type": 2,
                    "notes": []
                }
            ]
        },
        {
            "LPB": 4,
            "num": 20,
            "block": 0,
            "type": 2,
            "notes": [
                {
                    "LPB": 4,
                    "num": 24,
                    "block": 0,
                    "type": 2,
                    "notes": []
                }
            ]
        }
    ]
}
```

JSON は MusicDTO クラスを使用して上記のような形式で保存されますとのこと。
- **note.type**:
  - `shake`: 単発シェイク
  - `synchro`: チーム全員同時シェイク（高得点）
  - ※ 流れるノーツの実装有無は後述

#### 譜面読み込み
- **Resources.Load<TextAsset>()** でJSONファイル読み込み
- **JsonUtility** でデシリアライズ

### 2.5 ノーツ表示の実装判断

#### 実装方針
- **段階的実装**:
  1. **Phase 1**: ノーツなし（シンクロ判定のみ）
  2. **Phase 2**: ノーツ表示追加（ラグ検証後に実装判断）

#### ノーツ表示を実装する場合
- **生成方式**: **事前生成＋プール再利用** 
  - 理由: リアルタイム生成は負荷が高い、GC発生リスク
  - 譜面読み込み時に全ノーツを生成（非アクティブ状態）
  - 表示タイミングでアクティブ化＋位置設定
- **表示タイミング**: `note.time - LOOKAHEAD_TIME`（例: 1秒前）
- **移動方式**:
  - または画面上から下に落ちる（縦スクロール）
- **判定ライン**: 画面各メンバーを示すアイコンがあり、そこに流れてくる。

#### 判定精度
- **AudioSettings.dspTime** でサンプル単位の高精度（44.1kHz = 約0.02ms）
- **判定ウィンドウ案**:
  - Perfect: ±50ms
  - Good: ±100ms
  - Bad: ±150ms
- **実装例**:
  ```csharp
  JudgeResult Judge(double noteTime, double shakeTime) {
      double diff = Math.Abs(noteTime - shakeTime);
      if (diff < 0.05) return JudgeResult.Perfect;
      if (diff < 0.10) return JudgeResult.Good;
      if (diff < 0.15) return JudgeResult.Bad;
      return JudgeResult.Miss;
  }
  ```

---

## 3. ゲームフロー

### 3.1 待機画面（Idle Loop）
- **状態**: ゲーム外、常時ループ
- **表示**:
  - 動画ループ再生
  - 「シェイクでデバイス登録！」のメッセージ
- **機能**:
  - シェイク検知(10回連続とか)で **デバイス登録**（ESP8266のID記録）(それ以降ほかのIDからのシェイク信号は無視する)
  - 登録済みデバイス一覧を表示
  - 「全員でシェイクしてスタート！」のガイド
  - 一度静かになった後、**同時シェイク検知** でゲーム開始（後述）

### 3.2 ゲーム開始トリガー

#### 登録済みデバイスの同時シェイク検知
- **条件**:
  - 登録済みデバイスが2台以上
  - 全デバイスが200ms以内にシェイク
- **処理**:
  - `GameManager.StartGame()` 呼び出し
  - 動画を先頭から再生開始
  - ゲームタイマー開始

### 3.3 ゲーム中（Gameplay）

#### 画面構成
- **背景**: 動画再生（全画面またはワイド表示）
- **UI要素**:
  - **ボルテージゲージ** - 画面上部、シンクロ率に応じて上昇
  - **キャラクター＋吹き出し** - 画面下部、指示・応援メッセージ
  - **スコア表示** - 現在のポイント
  - **タイマー** - 残り時間または経過時間
  - **シンクロ演出** - 「シンクロ！」「完璧！」等のポップアップ
  - 各プレイヤーの判定枠(シェイク時にエフェクトが出る)。画面中央部にオーバーレイ

#### ゲームロジック

##### シンクロ判定システム
- **入力収集**:
  - SerialInputReaderV2が各ESP8266からのシェイク信号を受信（バックグラウンドスレッド）
  - タイムスタンプ（**AudioSettings.dspTime**）と共に記録（スレッドセーフ、高精度）
  - デバイスIDと時刻のペアを保存: `(deviceId, timestamp)` ※timestampはdouble型
- **シンクロ率計算**:
  - **方式1: スライディングウィンドウ（推奨）**
    ```csharp
    float CalculateSyncRate(double currentTime, float windowSize = 0.2f) {
        int shakeCount = 0;
        foreach (var device in registeredDevices) {
            double lastShakeTime = GetLastShakeTime(device);  // dspTime
            double relativeShakeTime = lastShakeTime - gameStartDspTime;
            double relativeMusicTime = currentTime - gameStartDspTime;
            if (Math.Abs(relativeMusicTime - relativeShakeTime) < windowSize) {
                shakeCount++;
            }
        }
        return (float)shakeCount / registeredDevices.Count;
    }
    ```
  - **方式2: イベントトリガー（代替案）**
    - 任意のデバイスがシェイク時、200msウィンドウで他デバイスをチェック
    - 例: Device1がシェイク → 200ms以内にDevice2,3,4もシェイク → シンクロ率80%
  - **選択**: 方式1（リアルタイム判定、譜面タイミングと連携可能）
- **ボルテージ加算**:
  - シンクロ率 × フェーズ係数 × ベース値 = ボルテージ増加量
  - 計算式例:
    ```csharp
    float baseVoltage = 5f; // 1シェイクあたりの基本値
    float phaseMultiplier = currentPhase.multiplier; // 1.0 ~ 2.0
    float syncBonus = Mathf.Pow(syncRate, 2); // 2乗でシンクロ重視
    float voltageIncrease = baseVoltage * phaseMultiplier * syncBonus;
    ```
  - 例: Chorus（係数2.0）、シンクロ率80% → 5 × 2.0 × 0.64 = 6.4ポイント
  - 例: Intro（係数1.0）、シンクロ率50% → 5 × 1.0 × 0.25 = 1.25ポイント

##### フェーズシステム
- **譜面データから読み込み**:
  - `{ "time": 30.0, "type": "chorus", "message": "サビだ！盛り上げろ！" }`
  - NoteEditorのデータ形式にはないのでそれ用のレーンを作って、タイミングを記録→文章は自分で後で編集するなど工夫が必要。
- **フェーズ切り替え**:
  - 指定時刻で自動切り替え
  - キャラクター吹き出しにメッセージ表示
  - ボルテージ係数を変更

##### エフェクト演出
- **シェイク時**:
  - 画面フラッシュ、パーティクルエフェクト（EffectPool使用）
  - 効果音再生（AudioManager使用）
- **シンクロ成功時**:
  - 大きな演出（画面全体エフェクト、歓声SE）
  - 「シンクロ！」アニメーション表示
  - ボルテージゲージ急上昇

#### ゲーム終了条件
- **時間切れ**: 曲の終わり（duration秒経過）
- **手動終了**: ESCキー等（デバッグ用）

### 3.4 結果発表（Result）

#### 表示内容
- **最終ボルテージゲージ**: 大きく表示
- **ランク評価**: S/A/B/C（ゲージ量に応じて）
- **メッセージ**: ランクに応じた文言
  - S: "完璧なチームワーク！"
  - A: "素晴らしい！"
  - B: "良いぞ！"
  - C: "まだまだだな..."
- **キャラクターリアクション**: ランクに応じたアニメーション・表情

#### UI実装

##### 1. スコア・ランク表示
- **TextMeshPro** でスコア・ランク表示
  - 標準的なテキスト表示、高品質なフォントレンダリング
  - 実装: `tmpText.text = $"Score: {score}"`

##### 2. ボルテージゲージ
- **UI.Slider** で実装（DOTween不要）
  - **理由**: シンプル、軽量、リアルタイム更新が容易
  - **実装方法**:
    ```csharp
    // 毎フレーム更新でスムーズな動き
    slider.value = Mathf.Lerp(slider.value, targetVoltage / maxVoltage, Time.deltaTime * 5f);
    ```
  - **代替案**: `Mathf.SmoothDamp()` でより自然な減速
  - **アニメーション**: 不要（Lerpで十分滑らか）

##### 3. ポップアップテキスト（「シンクロ！」等）
- **⚠️ Object Pool必須**: 頻繁に生成・破棄されるため
- **Animation Component** が正式名称
  - **実装方法1: Animation Component + Object Pool（推奨）**←こちらに決定
    1. **PopupPool.cs** 作成（Version1のEffectPoolと同様）
       ```csharp
       public class PopupPool : MonoBehaviour {
           [SerializeField] private GameObject popupPrefab;
           private Queue<GameObject> pool = new Queue<GameObject>();
           private const int INITIAL_SIZE = 10;
           
           void Start() {
               for (int i = 0; i < INITIAL_SIZE; i++) {
                   GameObject popup = Instantiate(popupPrefab, transform);
                   popup.SetActive(false);
                   pool.Enqueue(popup);
               }
           }
           
           public GameObject GetPopup() {
               if (pool.Count > 0) {
                   GameObject popup = pool.Dequeue();
                   popup.SetActive(true);
                   return popup;
               }
               return Instantiate(popupPrefab, transform);
           }
           
           public void ReturnPopup(GameObject popup) {
               popup.SetActive(false);
               pool.Enqueue(popup);
           }
       }
       ```
    2. TextMeshProオブジェクトに **Animation** コンポーネントをアタッチ
    3. Animation Clipを作成（Window > Animation > Animation）
    4. キーフレーム設定:
       - 0.0s: Scale(0,0,0), Alpha(0) - 初期状態
       - 0.2s: Scale(1.2,1.2,1.2), Alpha(1) - 拡大
       - 0.5s: Scale(1,1,1), Alpha(1) - 通常サイズ
       - 1.0s: Scale(0.8,0.8,0.8), Alpha(0) - フェードアウト
    5. Animation終了時に自動でプールに返却
       ```csharp
       // Animation Eventで呼び出し
       public void OnAnimationComplete() {
           PopupPool.Instance.ReturnPopup(gameObject);
       }
       ```
  - **実装方法2: Coroutine + Object Pool（軽量）**
    ```csharp
    IEnumerator PopupAnimation(GameObject popup) {
        popup.SetActive(true);
        // 拡大
        float elapsed = 0f;
        while (elapsed < 0.3f) {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / 0.3f);
            popup.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        // 待機
        yield return new WaitForSeconds(0.5f);
        // フェードアウト
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        elapsed = 0f;
        while (elapsed < 0.3f) {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
            yield return null;
        }
        popup.SetActive(false);
    }
    ```
  - **推奨**: Animation Component（GUIで編集可能、デザイナーフレンドリー）

##### 4. キャラクターアニメーション
- **段階的実装**（優先度: 低）
  - **Phase 1**: 静止画像のみ（Imageコンポーネント）
  - **Phase 2**: 表情差分の切り替え（Sprite切り替え）
    ```csharp
    image.sprite = rankSprites[rankIndex]; // S/A/B/Cで画像切り替え
    ```
  - **Phase 3**: **Animator** でアニメーション追加
    - Animation Controller作成
    - ランクに応じたアニメーション状態遷移

##### 実装優先度
| UI要素 | 実装方法 | 優先度 | 備考 |
|---|---|---|---|
| スコア表示 | TextMeshPro | ★★★ 高 | Phase 1で実装 |
| ボルテージゲージ | UI.Slider + Lerp | ★★★ 高 | Phase 1で実装 |
| ポップアップ | Animation / Coroutine | ★★☆ 中 | Phase 2で実装 |
| キャラクター | Image → Animator | ★☆☆ 低 | Phase 4で実装 |

#### 終了後
- **「もう一度プレイ」ボタン**は不要。待機画面に戻るだけでいい。
- **待機画面に戻る**: 数秒後に自動遷移

---

## 4. 実装計画（詳細版）

### ⚠️ 重要な技術方針
1. **タイムスタンプ統一**: `AudioSettings.dspTime`に統一（Version1と同じ、実績あり）
   - スレッドセーフ（バックグラウンドスレッドで使用可能）
   - 高精度（サンプル単位、約0.02ms精度）
   - VideoPlayerの音声クロックと同期
2. **ポップアップのObject Pool化**: GC削減のため必須
3. **時刻同期検証**: dspTimeベースの相対時刻計算の精度確認が重要

---

### 準備フェーズ: プロジェクト構造整理(1日)

#### 0-1. Version1/フォルダ作成(0.5日)
- [x] Version1/フォルダ構造作成
  ```
  Assets/Scripts/Version1/
    ├── Managers/
    ├── Gameplay/
    ├── Handlers/
    ├── Input/
    └── UI/
  ```
- [x] 既存の全コードをVersion1/配下へ移動（共通フォルダは使用しない）
  - Managers/: GameManager, PhaseManager, ScoreManager, FreezeManager, SpriteManager, HighScoreManager
  - Gameplay/: Note, NotePool, NoteSpawner, NoteManager, EffectPool
  - Handlers/: NoteShakeHandler, RestShakeHandler, NullShakeHandler
  - Input/: ShakeResolver, SerialInputReader, KeyboardInputReader
  - UI/: TimerDisplay, PhaseDisplay, PhaseProgressBar, ScoreDisplay等
#### 0-2. Version2/フォルダ構成作成(0.3日)
- [x] Version2/フォルダ構造作成(上記2.1参照)
- [x] Version2専用GameConstants作成
  - `GameConstantsV2.cs`: Version2固有定数
  - SYNC_WINDOW_SIZE, BASE_VOLTAGE, VOLTAGE_MAX等

#### 0-3. シーン準備(0.2日)
- [x] `Scenes/Version1_GameScene.unity` に現在のシーンをリネーム
- [x] `Scenes/Version2_GameScene.unity` を新規作成
- [x] Version2シーンに必要なオブジェクト配置
  - MainCamera, EventSystem, Canvas
  - VideoPlayerをCamera Far Planeで表示（UIはCanvasで前面に重ねる）

**準備フェーズ完了条件**:
- Version1が元のフォルダ構成で正常動作
- Version2用の空フォルダ構造が整備
- 2つのシーンが独立して管理可能

### Phase 1: コア実装（3日）
#### 0. シリアルポート管理
- Version1のをコピーしてくる
#### 1-1. 入力システム構築（1日、V2は完全新規）
- [x] **SerialInputReaderV2.cs** 新規作成 ← Version1と同じdspTime方式
  - SerialInputReader（Version1）を参考にする。
  - タイムスタンプ: `AudioSettings.dspTime`（Version1と同じ、スレッドセーフ）
  - 入力先: `InputQueueV2`（Version2専用の統一キュー）
  - スレッド実装はVersion1と同様
  ```csharp
  // dspTimeでタイムスタンプ記録（スレッドセーフ）
  string data = SerialPortManagerV2.Instance.ReadLine();
  if (!string.IsNullOrEmpty(data)) {
      double timestamp = AudioSettings.dspTime;
      InputQueueV2.Enqueue(data.Trim(), timestamp);
  }
  ```
- [x] **KeyboardInputReaderV2.cs** 新規作成
  - デバッグ用、スペースキー・数字キー（0-9）でシェイク
  - CSV形式シミュレーション: `ID,COUNT,ACCEL`
  - タイムスタンプ: `AudioSettings.dspTime`
  - 入力先: `InputQueueV2`
- [x] **InputQueueV2.cs** 新規作成（静的クラス）
  - Version2専用の統一入力キュー
  - `ConcurrentQueue<ShakeInput>` ※ShakeInputは`double timestamp`を含む
  - CSV形式パース機能を含む: `ID,COUNT,ACCEL`
  ```csharp
  public static class InputQueueV2 {
      public struct ShakeInput {
          public string rawData;
          public string deviceId;  // 0~9の数値文字列
          public double timestamp; // AudioSettings.dspTime
          public int count;
          public float accel;
      }
      private static ConcurrentQueue<ShakeInput> queue = new ConcurrentQueue<ShakeInput>();
      
      public static void Enqueue(string data, double timestamp) {
          ParseSerialPayload(data, out string deviceId, out int count, out float accel);
          queue.Enqueue(new ShakeInput(data, deviceId, timestamp, count, accel));
      }
      
      public static bool TryDequeue(out ShakeInput input) {
          return queue.TryDequeue(out input);
      }
  }
  ```
- [x] **動作確認**: シリアル入力とキーボード入力が同時に動作
- **検証**: スペースキーでAudioSettings.dspTimeがログ出力されることを確認

#### 1-2. 基本マネージャー実装（1日、V2は完全新規）
- [ ] **GameManagerV2.cs** 新規作成
  - イベント管理: `OnIdleStart`, `OnGameStart`, `OnGameEnd`
  - 基準時刻記録: `gameStartDspTime`（AudioSettings.dspTime）
  - シングルトンパターン
  - Version1のGameManagerを参考（別ファイル）
  ```csharp
  private double gameStartDspTime;
  public bool IsGameStarted { get; private set; }
  
  public void StartGame() {
      gameStartDspTime = AudioSettings.dspTime;
      IsGameStarted = true;
      VideoManager.Instance.PlayFromStart();
      OnGameStart?.Invoke();
  }
  
  // 音楽時刻取得（VideoPlayer.timeの代替）
  public float GetMusicTime() {
      if (!IsGameStarted) return 0f;
      return (float)(AudioSettings.dspTime - gameStartDspTime);
  }
  
  // 入力時刻を相対時刻に変換
  public float GetRelativeTime(double absoluteDspTime) {
      return (float)(absoluteDspTime - gameStartDspTime);
  }
  ```
- [ ] **DeviceManager.cs** 新規作成
  - InputQueueV2から入力取得
  - デバイスID管理: `Dictionary<string, DeviceInfo>`
  - 登録ロジック: 10回連続シェイク検知
  - 最終シェイク時刻記録: `Dictionary<string, double>` ※dspTime
  - 同時シェイク検知: 200msウィンドウ
  - デバッグモード: 1台でもゲーム開始可能
  ```csharp
  public class DeviceInfo {
      public string deviceId;
      public double lastShakeTime;  // AudioSettings.dspTime
      public int consecutiveShakes;  // 登録用カウント
  }
  ```
- [ ] **VoltageManager.cs** 新規作成
  - ボルテージ値管理: 0～100%
  - シンクロボーナス計算: `Mathf.Pow(syncRate, 2)`
  - フェーズ係数対応
  - イベント: `OnVoltageChanged(float voltage)`
  ```csharp
  public void AddVoltage(float syncRate, float phaseMultiplier) {
      float baseVoltage = GameConstantsV2.BASE_VOLTAGE;
      float bonus = Mathf.Pow(syncRate, 2);
      float increase = baseVoltage * phaseMultiplier * bonus;
      currentVoltage = Mathf.Clamp(currentVoltage + increase, 0f, 100f);
      OnVoltageChanged?.Invoke(currentVoltage);
  }
  ```
- **検証**: デバイス登録→同時シェイク→ゲーム開始の流れを確認

#### 1-3. 動画再生システム（0.5日、V2は完全新規）
- [ ] **VideoManager.cs** 新規作成
  - VideoPlayerコンポーネント制御
  - StreamingAssets/Videos/からMP4読み込み
  - ループ再生・頭出し再生切り替え
  - **重要**: 音楽時刻はGameManagerV2.GetMusicTime()を使用（dspTimeベース）
  ```csharp
  private VideoPlayer videoPlayer;
  
  public void PlayLoop() {
      videoPlayer.isLooping = true;
      videoPlayer.Play();
  }
  
  public void PlayFromStart() {
      videoPlayer.time = 0;
      videoPlayer.isLooping = false;
      videoPlayer.Play();
      // ゲーム開始dspTimeはGameManagerV2が管理
  }
  
  // 音楽時刻取得はGameManagerV2に委譲
  public float GetMusicTime() {
      return GameManagerV2.Instance.GetMusicTime();
  }
  ```
- [ ] 動画素材準備
  - 解像度: 1920x1080
  - コーデック: H.264
  - ビットレート: 3-5Mbps（500MB以下目標）
  - 配置: `StreamingAssets/Videos/test_video.mp4`
- [ ] 表示方式をCamera Far Planeに設定
  - VideoPlayerのRender Modeを「Camera Far Plane」に設定
  - Target Camera に `MainCamera` を割り当て
  - CameraのDepthはUI Canvasより背面（CanvasはScreen Space - Overlay推奨）
  - アスペクト調整はCameraのViewport RectまたはVideoPlayerの「Aspect Ratio: Fit Vertically/Letterbox」等で調整
  - URPの場合、PostProcessingの影響を受けるため、必要ならLayer分離（例: `Video`レイヤー）とCamera Stackで管理

時刻同期テスト** ← ⚠️ 最重要
  ```csharp
  void Update() {
      if (!GameManagerV2.Instance.IsGameStarted) return;
      
      float musicTime = GameManagerV2.Instance.GetMusicTime();  // dspTimeベース
      float videoTime = (float)VideoManager.Instance.videoPlayer.time;
      float diff = Mathf.Abs(musicTime - videoTime);
      
      if (diff > 0.1f) {
          Debug.LogWarning($"[時刻ずれ検出] dspTime: {musicTime:F3}s, VideoTime: {videoTime:F3}s, Diff: {diff:F3}s");
      }
  }
  ```
- **検証**: 動画ループ再生→ゲーム開始で頭出し→dspTimeとVideoPlayer.timeのずれが0.1秒以内

#### 1-4. 基本UI（0.5日、V2は完全新規）
- [ ] **VoltageGaugeUI.cs** 新規作成
  - UI.Sliderコンポーネント使用
  - VoltageManager.OnVoltageChanged購読
  - Lerp補間でスムーズ更新
  ```csharp
  void Update() {
      slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * 5f);
  }
  ```
- [ ] **DeviceListUI.cs** 新規作成
  - 登録デバイス一覧表示（TextMeshPro）
  - DeviceManager.OnDeviceRegistered購読
  - 各デバイスにアイコン・色を割り当て
- [ ] **IdleScreenUI.cs** 新規作成
  - タイトルメッセージ表示
  - 「シェイクでデバイス登録！」
  - 「全員でシェイクしてスタート！」
- [ ] **デバッグUI追加**
  - 現在時刻表示: VideoPlayer.time, Time.time, Diff
  - 登録デバイス数表示
  - 最終シェイク時刻表示（各デバイス）

**Phase 1 目標**: 動画ループ＋デバイス登録＋同時シェイクで開始、時刻同期確認（ずれ<100ms）

**Phase 1 完了条件**:
- ✅ Version2専用の入力システムが動作（dspTimeベース）
- ✅ デバイス登録→同時シェイク→ゲーム開始の流れが完成
- ✅ dspTimeベースの音楽時刻とVideoPlayer.timeのずれが100ms以内
- ✅ 基本UIが表示され、ゲージが動作

---

### Phase 2: シンクロシステム（2日、V2は完全新規）

#### 2-1. シンクロ判定実装（1日）
- [ ] **SyncDetector.cs** 新規作成（Synchronization/）
  - InputQueueV2から入力取得
  - DeviceManagerから登録デバイス情報取得
  - スライディングウィンドウ方式でシンクロ率計算
  - ウィンドウサイズ: Inspector調整可能（default 0.2s）
  ```csharp
  void Update() {
      if (!GameManagerV2.Instance.IsGameStarted) {
          // 待機中はデバイス登録処理のみ
          while (InputQueueV2.TryDequeue(out var input)) {
              DeviceManager.Instance.ProcessRegistration(input.deviceId, input.timestamp);
          }
          return;
      }
      
      // ゲーム中の入力処理
      while (InputQueueV2.TryDequeue(out var input)) {
          // DeviceManagerに最終シェイク時刻を記録
          DeviceManager.Instance.RecordShake(input.deviceId, input.timestamp);
          
          // シンクロ率計算
          float musicTime = GameManagerV2.Instance.GetMusicTime();
          float syncRate = CalculateSyncRate(musicTime);
          
          // イベント発行
          OnSyncDetected?.Invoke(syncRate, musicTime);
      }
  }
  
  float CalculateSyncRate(float currentMusicTime) {
      float windowSize = GameConstantsV2.SYNC_WINDOW_SIZE;
      int syncCount = 0;
      
      foreach (var device in DeviceManager.Instance.GetRegisteredDevices()) {
          double lastShakeDspTime = device.lastShakeTime;  // dspTime
          float relativeShakeTime = GameManagerV2.Instance.GetRelativeTime(lastShakeDspTime);
          
          if (Mathf.Abs(currentMusicTime - relativeShakeTime) < windowSize) {
              syncCount++;
          }
      }
      
      return (float)syncCount / DeviceManager.Instance.GetDeviceCount();
  }
  ```
- [ ] **VoltageCalculator.cs** 新規作成（Synchronization/）
  - SyncDetector.OnSyncDetected購読
  - シンクロボーナス計算: `Mathf.Pow(syncRate, 2)`
  - フェーズ係数取得
  - VoltageManager.AddVoltage()呼び出し
  ```csharp
  void OnSyncDetected(float syncRate, float timestamp) {
      float phaseMultiplier = GetCurrentPhaseMultiplier();
      VoltageManager.Instance.AddVoltage(syncRate, phaseMultiplier);
      
      // デバッグログ
      if (GameConstants.DEBUG_MODE) {
          float bonus = Mathf.Pow(syncRate, 2);
          float voltage = GameConstantsV2.BASE_VOLTAGE * phaseMultiplier * bonus;
          Debug.Log($"[Sync] Rate:{syncRate:P0} Phase:{phaseMultiplier:F1}x → +{voltage:F1}V");
      }
  }
  ```
- [ ] **デバッグUI強化**
  - 現在のシンクロ率リアルタイム表示（バー表示）
  - 各デバイスの最終シェイク時刻（相対時刻）
  - シンクロ成功時のボルテージ増加量
  - ウィンドウ内デバイス数 / 総デバイス数
- **検証**: 複数デバイス（KeyboardInputReaderV2で擬似）でシンクロ判定

#### 2-2. エフェクト＋演出（1日）
- [ ] **PopupPool.cs** 新規作成（UI/） ← ⚠️ Object Pool必須
  - Version1のEffectPoolを参考に実装
  - 初期プールサイズ: 10
  - プール不足時は自動拡張
  - 戻し処理: Animation終了時に自動
- [ ] **SyncPopup.cs** 新規作成（UI/）
  - TextMeshPro + Animation Component
  - 「シンクロ！」「完璧！」「GOOD!」等
  - シンクロ率に応じてメッセージ変更
  - SyncDetector.OnSyncDetected購読
  - PopupPoolから取得・返却
  ```csharp
  void OnSyncDetected(float syncRate, float timestamp) {
      if (syncRate < 0.5f) return;  // 50%未満は表示なし
      
      GameObject popup = PopupPool.Instance.GetPopup();
      TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
      
      if (syncRate >= 0.9f) text.text = "完璧！";
      else if (syncRate >= 0.7f) text.text = "シンクロ！";
      else text.text = "GOOD!";
      
      popup.GetComponent<Animation>().Play();
  }
  ```
- [ ] **ShakeEffectController.cs** 新規作成（Gameplay/）
  - Core/Audio/AudioManagerで効果音再生
  - Version1のEffectPoolを参考にパーティクルエフェクト
  - 各プレイヤーアイコン位置にエフェクト
  - シンクロ時: 画面全体エフェクト
- [ ] **パフォーマンステスト**
  - KeyboardInputReaderV2で10デバイス擬似
  - 同時シェイク時のFPS確認（60fps維持）
  - Profilerでポップアップ生成時のGC確認（0であること）
  - 3分間連続プレイでメモリリーク確認

**Phase 2 目標**: シンクロ判定→ゲージ上昇→演出の完成、GC発生なし

**Phase 2 完了条件**:
- ✅ シンクロ率計算が正確（デバッグUIで確認）
- ✅ シンクロ率に応じてボルテージゲージが上昇
- ✅ ポップアップがObject Pool経由で表示（GC発生なし）
- ✅ 10デバイス同時シェイクで60fps維持

---

### Phase 3: 譜面システム（3日、オプショナル／V2は完全新規）

⚠️ **実装判断**: Phase 2完了後、ラグ検証とUXテストを行い、必要性を判断

#### 3-1. 譜面ローダー（1日）
- [ ] **ChartData.cs** 新規作成（Music/）
  - NoteEditor JSON構造に対応
  - 時刻計算式実装
- [ ] **ChartLoader.cs** 新規作成（Music/）
  - Resources.Load<TextAsset>("Charts/bgm")
  - JsonUtility.FromJson<ChartData>()
  - ノーツ時刻計算＋ソート

#### 3-2. ノーツ表示システム（1.5日）
- [ ] **NotePoolV2.cs** 新規作成（Gameplay/）
- [ ] **NoteV2.cs** 新規作成（Gameplay/）
- [ ] **NoteSpawnerV2.cs** 新規作成（Gameplay/）
- [ ] **JudgeLine.cs** 新規作成（UI/）

#### 3-3. 判定システム（0.5日）
- [ ] **JudgeManager.cs** 新規作成（Gameplay/）
- [ ] タイミングテスト、ラグ検証

**Phase 3 目標**: 音ゲーとしての完成度向上（UX次第で実装判断）

---

### Phase 4: 演出強化（2日、V2は完全新規）

#### 4-1. フェーズシステム（1日）
- [ ] **PhaseData.cs** 新規作成（Music/）
  - phase_data.json読み込み（別ファイル推奨）
  - または譜面JSON内の専用レーン（block=99）
- [ ] **PhaseManagerV2.cs** 新規作成（Managers/）
  - VideoManager.GetMusicTime()で時刻監視
  - フェーズ切り替え検知
  - イベント発行: `OnPhaseChanged(PhaseData phase)`
- [ ] **MessageBubble.cs** 新規作成（UI/）
  - キャラクター＋吹き出し表示
  - PhaseManagerV2.OnPhaseChanged購読
  - TextMeshProでメッセージ表示
  - Animation Componentでフェードイン/アウト

#### 4-2. 結果画面（1日）
- [ ] **ResultUI.cs** 新規作成（UI/）
  - 最終ボルテージ表示
  - ランク判定（S/A/B/C）
  - ランクに応じたメッセージ
- [ ] **AutoReturnToIdle.cs** 新規作成（Managers/）
  - 5秒後に自動でGameManagerV2.ShowIdle()呼び出し
  - スペースキーで手動スキップ（デバッグ用）

**Phase 4 目標**: 盛り上がる体験の完成

---

### 最終調整（1日）

#### 調整-1. パフォーマンス最終検証（0.5日）
- [ ] **10台入力負荷テスト**
  - KeyboardInputReaderV2で10デバイス擬似
  - 同時シェイク連打でFPS確認（60fps維持）
  - Profilerで各システムのCPU使用率確認
- [ ] **メモリリークテスト**
  - 5分間連続プレイ
  - Profilerでメモリ増加確認（GC発生確認）
  - PopupPool, InputQueueV2のメモリ使用量確認

#### 調整-2. 時刻同期最終検証（0.3日） ← ⚠️ 最重要
- [ ] **短期ずれ確認**
  - 1分間のプレイでdspTimeベースの音楽時刻 vs VideoPlayer.timeのずれ記録
  - 許容範囲: ±100ms以内
- [ ] **長期ドリフト確認**
  - 3分間（曲1本分）のプレイでドリフト確認
  - dspTimeは高精度なのでドリフトはほぼ発生しないはず
- [ ] **補正処理追加（必要に応じて）**
  ```csharp
  // ずれが大きい場合の補正（通常は不要だが保険として）
  float dspMusicTime = GameManagerV2.Instance.GetMusicTime();
  float videoTime = (float)videoPlayer.time;
  float diff = videoTime - dspMusicTime;
  
  if (Mathf.Abs(diff) > 0.2f) {
      // ゲーム開始dspTimeを補正
      GameManagerV2.Instance.gameStartDspTime -= diff;
      Debug.LogWarning($"[時刻補正] Adjusted dspTime offset by {diff:F3}s");
  }
  ```

#### 調整-3. UI・バグ修正（0.2日）
- [ ] UI配置・サイズ調整
- [ ] 色・フォント調整
- [ ] エラーハンドリング追加
- [ ] デバッグログの整理（本番ビルド時は非表示）

**総工数見積もり**: 13日（準備1 + Phase1:3 + Phase2:2 + Phase3:3 + Phase4:2 + 調整1 + バッファ1）
**最小構成（Phase1～2のみ）**: 6日
**推奨構成（Phase1～2＋4）**: 8日（譜面システムなし、演出あり）

---

## 5. 今後の検討事項

### 優先度: 高
- [ ] **ESP32からのデータフォーマット確認**: デバイスID付きシェイク信号の仕様
- [ ] **譜面作成ツールの選定**: NoteEditor等の互換性検証
- [ ] **動画素材の準備**: 解像度、フレームレート、音質の確認

### 優先度: 中
- [ ] **ノーツ表示の実装判断**: ラグ検証後に決定
- [ ] **シンクロウィンドウ調整**: プレイテストで最適値を決定
- [ ] **ボルテージ計算式の調整**: バランステストで調整

### 優先度: 低（拡張案）
- [ ] **曲アンロックシステム**: ボルテージ一定以上で次の曲解放
- [ ] **子機側判定**: ゲーム側からタイミング送信、子機でバイブレーション等のフィードバック
- [ ] **ハイスコアシステム**: PlayerPrefs保存（Version1と同様）

---

## 6. 注意事項

- **フォルダ操作は手動**: AI生成後に人間が整理
- **段階的実装**: 最小要件→拡張の順で実装
- **パフォーマンス**: 10台のESP8266からの入力を処理できること
- **デバッグ**: KeyboardInputReaderで1人でもテスト可能にする
- **イベント設計**: Version1と同様のイベント駆動設計を踏襲