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

## 3. ゲームフロー（3状態：Idle → GamePlay → Result → Idle）

### 3.0 状態管理設計（Version1踏襲）
- **3つのCanvas**で状態を管理（SetActive切り替え）
  - `IdleCanvas`: 待機画面
  - `GamePlayCanvas`: ゲームプレイ画面
  - `ResultCanvas`: 結果画面
- **GameManagerV2**がイベント発行、各UIコンポーネントが購読
  - `OnStateChanged(GameState newState)`: 状態遷移時
  - `OnDeviceRegistered(int deviceId)`: デバイス登録時
  - `OnGameStarted()`: ゲーム開始時
  - `OnSyncDetected(float syncRate)`: シンクロ検知時
  - `OnVoltageChanged(float voltage)`: ボルテージ変化時
  - `OnPhaseChanged(PhaseData phase)`: フェーズ変更時
  - `OnGameEnded(float finalVoltage, string rank)`: ゲーム終了時

### 3.1 待機画面（Idle）
- **状態**: ゲーム外、常時ループ
- **表示**:
  - 動画ループ再生（背景）
  - タイトル：「振りまくって、プレイ登録」
  - **デバイスアイコン**（画面下部、横並び）
    - **DeviceIconManager**が自動生成・管理（プレファブは1つのみ）
    - **SpriteRendererコンポーネント**でID（0-9）に応じた画像を自動差し替え
    - 対応IDのシェイク受信時：**CFXR3 Hit Misc A**エフェクト再生（プレファブそのまま使用）
    - エフェクトはSetActive切り替えのみ（自動停止機能を活用）
  - ガイドメッセージ：「全員でシェイクしてスタート！」
  - **Startボタン**（主にデバッグ用、Enter/クリックで起動）
- **機能**:
  - **デバイス登録**（10回連続シェイクで登録、それ以降他IDは無視）
  - **全員同時シェイク検知**またはStartボタンでGamePlayへ遷移
    - 条件：登録済みデバイスが200ms以内に全てシェイク

### 3.2 ゲームプレイ画面（GamePlay）

#### 画面構成
- **背景**: 動画再生（全画面、頭から再生）
- **UI要素**（オーバーレイ表示）:
  - **ボルテージゲージ**（画面上部）- シンクロ率に応じて上昇
  - **キャラクター＋吹き出し**（左下）- フェーズに応じたメッセージ表示
  - **参加子機アイコン**（画面中央下部、横並び）
    - Idle画面と同じDeviceIconプレファブを使用（DeviceIconManagerが管理、**SpriteRenderer方式**）
    - ゲーム開始時に登録済みアイコンを自動再生成
    - シェイク時：**CFXR3 Hit Misc A**エフェクト再生（プレファブそのまま使用）
    - **ノーツ表示時**：アイコンに向かってノーツが流れてくる
  - **シンクロポップアップ**（画面中央）
    - 「シンクロ！」「完璧！」等、シンクロ率に応じた文言
    - Animation Component + Object Pool（PopupPool）
  - **判定ポップアップ**（各アイコン上）
    - ノーツ判定時：「Perfect」「Good」「Bad」
    - Animation Component + Object Pool
  - **タイマー/スコア**（画面右上、デバッグ表示）

#### ゲームロジック

##### シンクロ判定システム
- **入力収集**:
  - SerialInputReaderV2が各ESP8266からのシェイク信号を受信（バックグラウンドスレッド）
  - タイムスタンプ（**AudioSettings.dspTime**）と共に記録（スレッドセーフ、高精度）
  - デバイスIDと時刻のペアを保存: `(deviceId, timestamp)` ※timestampはdouble型
- **シンクロ率計算**:
  - **方式: スライディングウィンドウ**
    ```csharp
    // SyncDetectorが毎フレーム判定（FixedUpdate推奨）
    void CheckSync() {
        double now = AudioSettings.dspTime;
        double windowStart = now - SYNC_WINDOW_SIZE; // 200ms
        int shakeCount = InputQueueV2.CountShakesInWindow(windowStart, now);
        float syncRate = (float)shakeCount / registeredDeviceCount;
        if (syncRate > 0.5f) { // 50%以上でシンクロ判定
            OnSyncDetected?.Invoke(syncRate, now);
        }
    }
    ```
- **ボルテージ加算**（VoltageManagerで実装）:
  - **VoltageManager**がSyncDetector.OnSyncDetectedを購読
  - シンクロ率 × フェーズ係数 × ベース値 = ボルテージ増加量
  - 計算式:
    ```csharp
    // VoltageManager.ProcessSync()
    private void ProcessSync(float syncRate, double timestamp) {
        float phaseMultiplier = PhaseManagerV2.Instance.GetCurrentMultiplier(); // Phase 4で実装
        float baseVoltage = GameConstantsV2.BASE_VOLTAGE;
        float bonus = Mathf.Pow(syncRate, 2); // 2乗でシンクロ率が高いほど急上昇
        float increase = baseVoltage * phaseMultiplier * bonus;
        
        currentVoltage = Mathf.Clamp(currentVoltage + increase, 0f, GameConstantsV2.VOLTAGE_MAX);
        OnVoltageChanged?.Invoke(currentVoltage);
    }
    ```
  - 例: Chorus（係数2.0）、シンクロ率80% → 5 × 2.0 × 0.64 = 6.4ポイント
  - 例: Intro（係数1.0）、シンクロ率50% → 5 × 1.0 × 0.25 = 1.25ポイント
  - **サビ前の連打モード**: Phase係数を上げることで実現（フェーズデータで制御）

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

### 3.3 結果画面（Result）

#### 表示内容
- **最終ボルテージゲージ**: 大きく表示（中央上部）
- **ランク評価**: S/A/B/C（ゲージ量に応じて、画面中央大きく表示）
- **メッセージ**: ランクに応じた文言
  - S: "完璧なチームワーク！"
  - A: "素晴らしい！"
  - B: "良いぞ！"
  - C: "まだまだだな..."
- **キャラクターリアクション**: ランクに応じた静止画像（Phase 1）→アニメーション（Phase 4）
- **自動遷移**: 5秒後にIdleへ戻る（AutoReturnToIdleコンポーネント）

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

## 4. 実装計画（UI優先・詳細版）

### ⚠️ 重要な技術方針
1. **イベント駆動設計（Version1踏襲）**:
   - GameManagerV2が状態変化・ゲームイベントを発行
   - UI/ロジックコンポーネントがイベント購読
   - 疎結合設計で保守性向上
2. **タイムスタンプ統一**: `AudioSettings.dspTime`に統一（Version1と同じ、実績あり）
   - スレッドセーフ（バックグラウンドスレッドで使用可能）
   - 高精度（サンプル単位、44.1kHz = 約0.02ms精度）
   - VideoPlayerの音声クロックと同期
3. **UI優先実装**: Canvas・プレファブ・エフェクトを先に作成し、後からロジックを接続
4. **ポップアップのObject Pool化**: GC削減のため必須
5. **VoltageCalculator削除**: VoltageManagerに統合（責任の明確化）

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
- [x] Version2/フォルダ構成作成(上記2.1参照)
- [x] Version2専用GameConstants作成（運用方針を明記）
  - `GameConstantsV2.cs`: 最小定数から開始し、必要になったタイミングで都度追記していく（変更点は計画書に反映）
  - 初期定数例: `SYNC_WINDOW_SIZE`, `BASE_VOLTAGE`, `VOLTAGE_MAX`
  - 追加定数例（今回追加）: シリアル通信向け `SERIAL_PORT_NAME`, `SERIAL_BAUD_RATE`, `SERIAL_RECONNECT_INTERVAL`

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

### Phase 1: UI基盤構築（3日、UI優先）

#### 1-0. Canvas・シーン構成（0.5日）
- [x] **3つのCanvas作成**（Version2_GameScene）
  - IdleCanvas: 待機画面UI
  - GamePlayCanvas: ゲームプレイUI
  - ResultCanvas: 結果画面UI
- [x] **VideoPlayer配置**（Camera Far Plane設定）
  - 全Canvas共通の背景として動画再生
  - UIはCanvasでオーバーレイ表示
- [x] **EventSystem配置**（ボタン・UI操作用）

#### 1-1. デバイスアイコンプレファブ作成（0.5日）
- [x] **CFXR3 Hit Misc Aのプレファブを特定**
  - `Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/`等を確認
  - Hit系エフェクトから適切なものを選択（名前が異なる場合は類似のものを使用）
- [x] **DeviceIconプレファブ作成**（Prefabs/Version2/UI/）← **プレファブは1つのみ**
  - **SpriteRendererコンポーネントをアタッチ**← 画像は後でDeviceIconManagerが差し替える
  - CFXR3 Hit Misc Aを**子オブジェクト**として配置
  - エフェクトは初期状態で`SetActive(false)`
  - プレファブ構成:
    ```
    DeviceIconPrefab (SpriteRendererアタッチ)
    ├── SpriteRenderer              ← デバイスID画像表示
    └── CFXR3 Hit Misc A (Prefab)   ← エフェクト（子オブジェクト）
        └── ParticleSystem複数
    ```
- [ ] **DeviceIcon.cs**作成（Scripts/Version2/Gameplay/）
  - `Initialize(string id, Sprite sprite)`: 初期化＋画像設定
  - `PlayHitEffect()`: エフェクト再生（SetActive切り替えのみ）
  - GameManagerV2.OnDeviceShakeイベント購読
  ```csharp
  public class DeviceIcon : MonoBehaviour {
      private SpriteRenderer spriteRenderer;
      private GameObject hitEffectObject; // CFXR3 Hit Misc A
      
      private string deviceId;
      
      void Awake() {
          spriteRenderer = GetComponent<SpriteRenderer>();
          // エフェクトは子オブジェクトから取得
          if (transform.childCount > 0) {
              hitEffectObject = transform.GetChild(0).gameObject; // CFXR3 Hit Misc A
          }
      }
      
      public void Initialize(string id, Sprite sprite) {
          deviceId = id;
          spriteRenderer.sprite = sprite;
          
          // エフェクトを初期化
          if (hitEffectObject != null) {
              hitEffectObject.SetActive(false);
          }
      }
      
      public string GetDeviceId() => deviceId;
      
      // ★ ShakeResolverV2から直接呼び出される
      public void OnShakeProcessed() {
          PlayHitEffect();
      }
      
      private void PlayHitEffect() {
          if (hitEffectObject == null) return;
          
          // エフェクトを一時的にアクティブ化
          hitEffectObject.SetActive(false); // リセット
          hitEffectObject.SetActive(true);  // 再生
          
          // CFXRは自動で停止・非アクティブ化される（Stop Action: Disable）
      }
  }
  ```
- [ ] **DeviceIconManager.cs**作成（UI/Scripts/）
  - プレファブ＋Sprite配列をInspector設定
  - DeviceManager.OnDeviceRegisteredイベント購読
  - 登録時にアイコン生成＋画像設定
  ```csharp
  public class DeviceIconManager : MonoBehaviour {
      [SerializeField] private GameObject deviceIconPrefab;
      [SerializeField] private Transform iconContainer; // HorizontalLayoutGroup
      [SerializeField] private Sprite[] deviceSprites; // Inspector設定（10種類、0-9）
      
      private Dictionary<string, DeviceIcon> activeIcons = new Dictionary<string, DeviceIcon>();
      
      void OnEnable() {
          if (DeviceManager.Instance != null) {
              DeviceManager.Instance.OnDeviceRegistered += OnDeviceRegistered;
          }
      }
      
      void OnDisable() {
          if (DeviceManager.Instance != null) {
              DeviceManager.Instance.OnDeviceRegistered -= OnDeviceRegistered;
          }
      }
      
      private void OnDeviceRegistered(string deviceId) {
          // アイコン生成
          GameObject iconObj = Instantiate(deviceIconPrefab, iconContainer);
          DeviceIcon icon = iconObj.GetComponent<DeviceIcon>();
          
          // デバイスID（0-9）に応じた画像設定
          int id = int.Parse(deviceId);
          icon.Initialize(deviceId, deviceSprites[id]);
          
          activeIcons[deviceId] = icon;
      }
      
      // ★ ShakeResolverV2から呼び出される
      public DeviceIcon GetDeviceIcon(string deviceId) {
          return activeIcons.TryGetValue(deviceId, out var icon) ? icon : null;
      }
      
      // GamePlayCanvas用に再生成（Phase 1-3で使用）
      public void RegenerateForGamePlay(Transform gamePlayContainer) {
          foreach (var kvp in activeIcons) {
              GameObject iconObj = Instantiate(deviceIconPrefab, gamePlayContainer);
              DeviceIcon icon = iconObj.GetComponent<DeviceIcon>();
              icon.Initialize(kvp.Key, deviceSprites[int.Parse(kvp.Key)]);
          }
      }
  }
  ```
- [x] **アイコン画像素材準備**（10種類、Media/DeviceIcons/）
- [ ] **動作確認**: KeyboardInputReaderV2でID 0-9を入力、画像切り替え＋エフェクト再生

**重要：シェイク処理の設計方針**
- **Version1のShakeResolver方式を踏襲**：イベントではなく直接呼び出し
- **ShakeResolverV2**が入力を受け取り、DeviceIconManager.GetDeviceIcon()でアイコン取得
- アイコンが存在すれば`icon.OnShakeProcessed()`を呼び出し
- **メリット**：ラグ減少、見通し良い、Version1と同じ設計パターン

#### 1-2. 待機画面UI実装（0.5日）
- [ ] **IdleScreenUI.cs** 新規作成（UI/）
  - タイトルテキスト（TextMeshPro）
  - ガイドメッセージ（TextMeshPro）
  - Startボタン（Button、Enter/クリックでGameManagerV2.TryStartGame()）
- [ ] **IdleCanvas内にDeviceIconManager配置**
  - DeviceListContainer（HorizontalLayoutGroup）にDeviceIconManager.csをアタッチ
  - Inspector設定:
    - Device Icon Prefab: DeviceIconプレファブを割り当て
    - Icon Container: 自身のTransform
    - Device Sprites: 10種類のSprite配列（0-9の順）
  - DeviceIconManagerが自動でアイコン生成・配置
- [ ] **動作確認**: KeyboardInputReaderV2でデバイス登録、アイコン自動追加、ボタンクリックでログ出力

#### 1-3. ゲームプレイ画面UI実装（0.5日）
- [ ] **VoltageGaugeUI.cs** 新規作成（UI/）
  - UI.Slider使用、Lerpで滑らか更新
  - VoltageManager.OnVoltageChangedイベント購読
- [ ] **GamePlayCanvas内にDeviceIconManager配置**
  - PlayerIconsContainer（HorizontalLayoutGroup）に2つ目のDeviceIconManager.csをアタッチ
  - Inspector設定: IdleCanvasと同じ設定
  - ゲーム開始時に`RegenerateForGamePlay()`を呼んで登録済みアイコンを再生成
  - または、GameManagerV2.OnGameStartedイベントで自動再生成
  ```csharp
  // GamePlayCanvasのDeviceIconManager
  void OnEnable() {
      if (GameManagerV2.Instance != null) {
          GameManagerV2.Instance.OnGameStarted += OnGameStarted;
      }
  }
  
  private void OnGameStarted() {
      // IdleCanvasのDeviceIconManagerから登録済みデバイス情報を取得して再生成
      var idleManager = FindObjectOfType<IdleCanvas>().GetComponentInChildren<DeviceIconManager>();
      // または、DeviceManagerから直接登録デバイス一覧を取得
      foreach (var deviceId in DeviceManager.Instance.GetRegisteredDeviceIds()) {
          OnDeviceRegistered(deviceId);
      }
  }
  ```
- [ ] **MessageBubbleUI.cs** 新規作成（UI/）
  - キャラクター画像 + 吹き出しテキスト（TextMeshPro）
  - PhaseManagerV2.OnPhaseChangedイベント購読（Phase 4で実装）
- [ ] **動作確認**: ゲージ手動更新、ゲーム開始時にアイコン再生成、メッセージ表示

#### 1-4. 結果画面UI実装（0.5日）
- [ ] **ResultUI.cs** 新規作成（UI/）
  - 最終ボルテージゲージ表示
  - ランク表示（TextMeshPro、大きく）
  - メッセージ表示（ランクに応じた文言）
  - キャラクターリアクション画像
  - GameManagerV2.OnGameEndedイベント購読
- [ ] **AutoReturnToIdle.cs** 新規作成（Managers/）
  - 5秒後に自動でGameManagerV2.ShowIdle()呼び出し
  - スペースキーで手動スキップ（デバッグ用）
- [ ] **動作確認**: 手動でランク切り替え、自動遷移

#### 1-5. 入力システム構築（0.5日、V2は完全新規）
- [x] **SerialPortManager.cs** Version1からコピー（Core/Serial/）
- [x] **SerialInputReaderV2.cs** 新規作成（Input/） ← Version1と同じdspTime方式
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

#### 1-6. 基本マネージャー実装（0.5日、V2は完全新規、イベント駆動）
- [x] **GameManagerV2.cs** 新規作成（Managers/）
  - **状態管理**: `GameState { Idle, GamePlay, Result }`
  - **Canvas切り替え**: `ShowIdle()`, `ShowGamePlay()`, `ShowResult()`
  - **イベント発行**:
    - `OnStateChanged(GameState newState)`
    - `OnGameStarted()`
    - `OnGameEnded(float finalVoltage, string rank)`
  - **※OnDeviceShakeイベントは使用しない**（ShakeResolverV2が直接DeviceIconを呼び出す）
  - 基準時刻記録: `gameStartDspTime`（AudioSettings.dspTime）
  - シングルトンパターン
  - **VideoManager経由で動画制御**（VideoPlayerの直接参照なし）
  - Version1のGameManagerを参考（別ファイル）
  ```csharp
  private double gameStartDspTime;
  public bool IsGameStarted { get; private set; }
  
  public void StartGame() {
      gameStartDspTime = AudioSettings.dspTime;
      IsGameStarted = true;
      if (VideoManager.Instance != null) {
          VideoManager.Instance.PlayFromStart();
      }
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
- [x] **DeviceManager.cs** 新規作成
  - InputQueueV2から入力取得
  - デバイスID管理: `Dictionary<string, DeviceInfo>`
  - 登録ロジック: 10回連続シェイク検知
  - 最終シェイク時刻記録: `Dictionary<string, double>` ※dspTime
  - 同時シェイク検知: 200msウィンドウ
  - デバッグモード: 1台でもゲーム開始可能
  - **イベント**: `event System.Action<string> OnDeviceRegistered` ← DeviceIconManagerが購読
  ```csharp
  public class DeviceInfo {
      public string deviceId;
      public double lastShakeTime;  // AudioSettings.dspTime
      public int consecutiveShakes;  // 登録用カウント
  }
  ```
- [x] **VoltageManager.cs** 新規作成（Managers/）
  - **SyncDetector.OnSyncDetectedを直接購読**（VoltageCalculatorは使わない）
  - ボルテージ計算・管理を一元化
  ```csharp
  public class VoltageManager : MonoBehaviour {
      public static VoltageManager Instance { get; private set; }
      private float currentVoltage = 0f;
      public event System.Action<float> OnVoltageChanged;
      
      void OnEnable() {
          if (SyncDetector.Instance != null) {
              SyncDetector.Instance.OnSyncDetected += ProcessSync;
          }
      }
      
      void OnDisable() {
          if (SyncDetector.Instance != null) {
              SyncDetector.Instance.OnSyncDetected -= ProcessSync;
          }
      }
      
      private void ProcessSync(float syncRate, double timestamp) {
          float phaseMultiplier = 1.0f; // TODO: PhaseManagerから取得（Phase 4）
          float baseVoltage = GameConstantsV2.BASE_VOLTAGE;
          float bonus = Mathf.Pow(syncRate, 2);
          float increase = baseVoltage * phaseMultiplier * bonus;
          
          currentVoltage = Mathf.Clamp(currentVoltage + increase, 0f, GameConstantsV2.VOLTAGE_MAX);
          OnVoltageChanged?.Invoke(currentVoltage);
      }
      
      public float GetVoltage() => currentVoltage;
      public void ResetVoltage() { currentVoltage = 0f; OnVoltageChanged?.Invoke(currentVoltage); }
  }
  ```
- **検証**: デバイス登録→同時シェイク→ゲーム開始の流れを確認

#### 1-7. 動画再生システム（0.5日、V2は完全新規）
- [x] **VideoManager.cs** 新規作成
  - VideoPlayerコンポーネント制御（シングルトン、DontDestroyOnLoad）
  - StreamingAssets/Videos/からMP4読み込み（VideoPlayer.url使用）
  - Prepare処理対応（非同期読み込み）
  - ループ再生・頭出し再生切り替え
  - **重要**: 音楽時刻はGameManagerV2.GetMusicTime()を使用（dspTimeベース）
  ```csharp
  private VideoPlayer videoPlayer;
  
  public void LoadFromStreamingAssets(string relativePath) {
      string filePath = Path.Combine(Application.streamingAssetsPath, relativePath);
      string url = "file://" + filePath;  // Windows用
      videoPlayer.source = VideoSource.Url;
      videoPlayer.url = url;
      isPrepared = false;
  }
  
  public void PlayLoop() {
      videoPlayer.isLooping = true;
      if (!isPrepared) {
          Prepare();
          videoPlayer.prepareCompleted += PlayOnPreparedLoopOnce;
      } else {
          videoPlayer.Play();
      }
  }
  
  public void PlayFromStart() {
      videoPlayer.isLooping = false;
      if (!isPrepared) {
          Prepare();
          videoPlayer.prepareCompleted += PlayFromStartOnPreparedOnce;
      } else {
          videoPlayer.time = 0;
          videoPlayer.Play();
      }
  }
  
  // 音楽時刻取得はGameManagerV2に委譲
  public float GetMusicTime() {
      return GameManagerV2.Instance.GetMusicTime();
  }
  
  // 同期検証用
  public float GetVideoTime() {
      return (float)videoPlayer.time;
  }
  ```
- [x] 動画素材準備
  - 解像度: 1920x1080
  - コーデック: H.264
  - ビットレート: 3-5Mbps（500MB以下目標）
  - 配置: `StreamingAssets/Videos/test_video.mp4`
- [x] 表示方式をCamera Far Planeに設定
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

### Phase 2: シェイク処理＋シンクロシステム＋エフェクト（2.5日、V2は完全新規）

#### 2-1. シェイク処理システム実装（0.5日） ← **Version1踏襲、ラグ減少**
- [ ] **ShakeResolverV2.cs** 新規作成（Input/）
  - Version1のShakeResolverを参考に実装
  - 統一入力キュー: `static ConcurrentQueue<(string deviceId, double timestamp)>`
  - `static EnqueueInput(string deviceId, double timestamp)`: SerialInputReaderV2/KeyboardInputReaderV2から呼ばれる
  - Update()でキューから取り出して処理
  - **処理フロー**:
    1. `DeviceManager.RecordShake(deviceId, timestamp)` ← 最終シェイク時刻記録
    2. `DeviceIconManager.GetDeviceIcon(deviceId)` ← アイコン取得
    3. `icon?.OnShakeProcessed()` ← エフェクト再生（イベントではなく直接呼び出し）
    4. `SyncDetector.CalculateSyncRate()` → シンクロ率計算（Phase 2-3で実装）
  ```csharp
  public class ShakeResolverV2 : MonoBehaviour {
      private static ConcurrentQueue<(string deviceId, double timestamp)> _sharedInputQueue 
          = new ConcurrentQueue<(string deviceId, double timestamp)>();
      
      [SerializeField] private DeviceIconManager idleIconManager;
      [SerializeField] private DeviceIconManager gamePlayIconManager;
      
      public static void EnqueueInput(string deviceId, double timestamp) {
          _sharedInputQueue.Enqueue((deviceId, timestamp));
      }
      
      void Update() {
          while (_sharedInputQueue.TryDequeue(out var input)) {
              ProcessShake(input.deviceId, input.timestamp);
          }
      }
      
      private void ProcessShake(string deviceId, double timestamp) {
          // 1. デバイスマネージャーに記録
          DeviceManager.Instance?.RecordShake(deviceId, timestamp);
          
          // 2. 現在の状態に応じたアイコンマネージャーを取得
          var currentManager = GameManagerV2.Instance.IsGameStarted 
              ? gamePlayIconManager 
              : idleIconManager;
          
          // 3. アイコンにエフェクト再生を指示（直接呼び出し）
          var icon = currentManager?.GetDeviceIcon(deviceId);
          icon?.OnShakeProcessed();
          
          // 4. シンクロ判定（Phase 2-3で実装）
          // SyncDetector.Instance?.OnShakeInput(deviceId, timestamp);
      }
  }
  ```
- [ ] **SerialInputReaderV2/KeyboardInputReaderV2修正**
  - `ShakeResolverV2.EnqueueInput(deviceId, timestamp)` を呼び出すように変更
- [ ] **動作確認**: KeyboardInputReaderV2でシェイク、アイコンエフェクト再生確認

#### 2-2. ポップアッププール実装（0.5日） ← UI優先
- [ ] **PopupPool.cs** 新規作成（UI/） ← ⚠️ Object Pool必須
  - Version1のEffectPoolを参考に実装
  - `Get(string popupType)`: プール取得
  - `Return(GameObject popup)`: プール返却
  - 戻し処理: Animation終了時に自動
- [ ] **SyncPopup.cs** 新規作成（UI/Prefabs/）
  - TextMeshPro + Animation Component
  - シンクロ率に応じた文言切り替え
    - 100%: "完璧！"
    - 80-99%: "シンクロ！"
    - 50-79%: "いいね！"
  - アニメーション: フェードイン→スケール拡大→フェードアウト（1秒）
- [ ] **JudgePopup.cs** 新規作成（UI/Prefabs/）
  - TextMeshPro + Animation Component
  - 判定文言: "Perfect" / "Good" / "Bad"
  - 色分け: Perfect=金、Good=緑、Bad=赤
- [ ] **動作確認**: 手動でポップアップ表示、プール再利用確認

#### 2-3. シンクロ判定実装（1日）
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
- ✅ ShakeResolverV2が動作（イベントではなく直接呼び出し、ラグ減少）
- ✅ ポップアッププールが動作（GC発生なし、3分間連続使用でメモリ安定）
- ✅ シンクロ率計算が正確（デバッグUIで確認）
- ✅ シンクロ率に応じてボルテージゲージが上昇
- ✅ シンクロポップアップが表示される（Object Pool経由）
- ✅ デバイスアイコンのシェイクエフェクトが再生される
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
  - NoteプレファブのObject Pool管理
- [ ] **NoteV2.cs** 新規作成（Gameplay/Prefabs/）
  - ノーツ移動ロジック（各デバイスアイコンに向かって流下）
  - 判定ライン到達時の処理
- [ ] **NoteSpawnerV2.cs** 新規作成（Gameplay/）
  - ChartLoaderから譜面データ取得
  - 表示タイミングでNotePoolからノーツ取得・配置
  - 各ノーツのターゲットデバイスID設定

#### 3-3. 判定システム（0.5日）
- [ ] **JudgeManager.cs** 新規作成（Gameplay/）
  - ノーツ判定ロジック（Perfect/Good/Bad）
  - JudgePopup表示（PopupPool使用）
  - GameManagerV2.OnDeviceShakeイベント購読
- [ ] タイミングテスト、ラグ検証（±50ms/±100ms/±150ms）

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

#### 4-2. 結果画面統合（0.5日）
- [ ] **ResultUI統合**（Phase 1で作成済み）
  - ランク判定ロジック追加（S: 80%以上、A: 60%、B: 40%、C: それ以下）
  - キャラクターリアクション画像切り替え（ランクに応じて）
- [ ] **AutoReturnToIdle統合**（Phase 1で作成済み）
  - タイマー起動確認（5秒後にIdle遷移）
  - デバッグスキップ確認（スペースキー）

#### 4-3. 全体調整（0.5日）
- [ ] **全イベント接続確認**
  - GameManagerV2 → 各UIコンポーネントのイベント購読確認
  - VoltageManager → VoltageGaugeUI
  - SyncDetector → VoltageManager, SyncPopupController
  - PhaseManagerV2 → MessageBubbleUI
- [ ] **デバッグUI追加**
  - 現在時刻表示（dspTime, VideoPlayer.time, Diff）
  - FPS表示
  - 現在のフェーズ表示
  - 各デバイスの最終シェイク時刻

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

**総工数見積もり**: 12.5日（準備1 + Phase1:3 + Phase2:2.5 + Phase3:3 + Phase4:2 + 調整1）
**最小構成（Phase1～2のみ）**: 6.5日（UI基盤 + シェイク処理 + シンクロシステム）
**推奨構成（Phase1～2＋4）**: 8日（譜面システムなし、演出・フェーズあり）
**完全版（Phase1～4）**: 11日（譜面システムあり）

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