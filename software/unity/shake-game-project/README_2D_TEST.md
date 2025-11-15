# 🎮 Shake Game 2D Edition (試験版)

**プロジェクト名:** shake-game-project  
**バージョン:** 0.2.0（試験版）  
**Unity バージョン:** 2021.3 LTS 以上  
**ステータス:** ⏳ Phase 2 進行中

---

## ⚠️ 重要なお知らせ

~~このプロジェクトは **2D での試験版実装** です。~~

~~**本体版は 3D 版です:**~~
~~→ `../shake-game-3d/` を参照してください。~~
→ 変更 シンプルな2D版を本番用第一案にする。
---

## 📋 このプロジェクトの目的

Processing での実装を **Unity C#** に移植し、以下を検証：

- ✅ Serial 通信が正常に動作するか
- ✅ ゲームロジックが正常に動作するか
- ✅ UI/UX がプレイしやすいか
- ✅ 複数デバイスでの同時動作は可能か

---

## 🎯 実装済み機能

- [x] Serial 通信（ESP32 親機とのデータ受信）
- [x] ~~2チーム対戦ゲームロジック~~ →1チームが高いスコアをねらうゲーム形式
- [x] ゲージシステム
- [x] フェーズシステム（Charge/Resist）
- [x] 2D UI 表示
- [ ] 効果音・SE（実装予定）
- [ ] ビジュアル改善（実装予定）

---

## � 既存スクリプトの改廃方針

### ✅ 継続・改修対象

| スクリプト | 現状 | 改修内容 |
|-----------|------|---------|
| `GameManager.cs` | Core層 | **全面刷新**: 2チーム対戦 → 1チーム協力型に変更。GameMode削除。|
| `UIManager.cs` | UI層 | **全面刷新**: 複数モード → 3Canvas（Start/Game/Result）に単純化 |
| `SerialManager.cs` | Input層 | **軽微改修**: パーサーシンプル化、イベント直接呼び出し |
| `GamePhaseManager.cs` | Core層 | **改修**: フェーズを Note/Rest に限定。ロジック簡略化。 |

### ✅ 削除完了（実装済み）

| スクリプト | 削除理由 | ステータス |
|-----------|---------|-----------|
| `BattleGameMode.cs` | 2チーム対戦ロジック（新設計では不要） | ✅ 削除済み |
| `GameMode.cs` | ベースクラス（新設計では不要） | ✅ 削除済み |
| `SerialDataParser.cs` | パーサーロジック（SerialManager に統合可能） | ✅ 削除済み |
| `VictoryManager.cs` | 勝敗判定ロジック（新設計: スコア最大化型） | ✅ 削除済み |
| `CommandSender.cs` | LED送信機能（3D版へ移行） | ✅ 削除済み |
| `VideoManager.cs` | 背景動画管理（VideoPlayer で直接制御で十分） | ✅ 削除済み |
| `SoundManager.cs` | 効果音管理（各 AudioSource で直接管理で十分） | ✅ 削除済み |

### 🆕 新規作成対象

| スクリプト | 役割 |
|-----------|------|
| `Game/NotePrefab.cs` | 音符・休符オブジェクト |
| `Game/PhaseController.cs` | フェーズ A/B 管理 |
| `Game/ScoreManager.cs` | スコア計算・加点・ペナルティ |
| `UI/TimerDisplay.cs` | タイマー表示更新 |
| `UI/ScoreDisplay.cs` | スコア表示更新 |
| `Core/GameConstants.cs` | 定数管理 |

---

## 🔍 実装チェックリスト（刷新後）

- [x] `GameManager.cs` 刷新完了 ✅
- [x] `InputManager.cs` (3D版参照) 統合完了 ✅
- [x] `UIManager.cs` Canvas 管理に改修完了 ✅
- [x] `PhaseController.cs` 新規実装完了 ✅
- [x] `NotePrefab.cs` 新規実装完了 ✅
- [x] `ScoreManager.cs` 新規実装完了 ✅
- [x] 不要スクリプト削除完了 ✅
- [ ] Scene `Game.unity` セットアップ完了（3Canvas + 背景動画） ⏳
- [ ] Serial通信テスト OK
- [ ] 60秒ゲームロジックテスト OK

---

## 🚀 セットアップと実行（新設計版）

### 1️⃣ Scene セットアップ

```
Assets/Scenes/Game.unity を開く（または新規作成）

Hierarchy に以下を構築:
├── Main Camera
│   └── (Background: BackGroundVideo GameObject を配下に、または独立)
│
├── BackGroundVideo (GameObject)
│   └── VideoPlayer (RenderMode: CameraFarPlane)
│       └── 背景動画を常時再生
│
├── Canvas_Start
│   ├── Title Text ("SHAKE GAME")
│   ├── Play Button
│   └── (Settings Panel オプション)
│
├── Canvas_Game
│   ├── Panel_Header
│   │   ├── Timer Text ("60.0")
│   │   └── Phase Indicator Text ("♪ NOTES")
│   │
│   ├── Panel_Notes (RectTransform: 音符生成エリア)
│   │   └── (Note Prefab が Runtime に Instantiate される)
│   │
│   └── Panel_Warning (Image: ホワイトフラッシュ or 凍結画像)
│       └── (フリーズ時に表示、透明度 0 でデフォルト非表示)
│
├── Canvas_Result
│   ├── Title Text ("RESULT")
│   ├── Final Score Text ("0")
│   ├── Ranking Panel (オプション)
│   │   └── Ranking Items
│   └── TitleButton （スタート画面に戻る）
│
└── Game Objects (Managers)
    ├── InputManager
    ├── GameManager
    ├── UIManager
    ├── PhaseController
    └── ScoreManager
```

**変更点:**
- ✅ RetryButton は削除（スタート画面に戻るのは TitleButton で統一）
- ✅ BackGroundVideo: VideoPlayer を **CameraFarPlane** モードで常時再生
- ⏳ NotePrefab の実装方針を再考（詳細は 2️⃣ 参照）

### 2️⃣ Note Prefab 準備（再設計版）

#### 概要
- **UI Button** ではなく、**World Space 上の SpriteRenderer** で実装
- **タッチ判定不要** ✅（CircleCollider2D は付けない）
  - 理由：シェイク検知で画面全体の音符を一括処理するため、個別の Collider は不要
  - GameManager が `FindObjectsOfType<NotePrefab>()` ですべての音符を取得して破裂判定
- 破裂エフェクト + 爆発音が再生される
- フェーズに応じた色変更（白 = 音符、グレー = 休符）

#### Note Prefab 構成 (`Assets/Prefabs/Note.prefab`)

```
GameObject: Note
├── Position: Random(画面内) [GameManager が生成時に設定]
├── Transform
│   ├── Scale: (1, 1, 1)
│   └── Rotation: (0, 0, 0)
│
├── SpriteRenderer ✅
│   ├── Sprite: 音符画像 (e.g., "note_white.png")
│   ├── Color: White (Phase_NotePhase時) / Gray (Phase_RestPhase時)
│   ├── Sorting Order: 10
│   └── Material: Sprites/Default
│
├── NotePrefab.cs Script ✅
│   └── 責務: フェーズ変更、エフェクト再生、スコア処理
│
└── AudioSource ✅
    ├── Spatial Blend: 0 (2D音声)
    ├── Volume: 0.7
    └── Play On Awake: false
```

#### NotePrefab.cs スクリプト（詳細実装方針）

```csharp
public class NotePrefab : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Phase currentPhase;
    private AudioSource audioSource;
    
    // 色設定
    private Color noteColor = Color.white;
    private Color restColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    // 画像設定（Inspector から割り当て）
    [SerializeField] private Sprite noteSprite;    // 白い音符
    [SerializeField] private Sprite restSprite;    // 灰色の休符
    
    // SE設定
    [SerializeField] private AudioClip noteBreakSE;    // 破裂音
    [SerializeField] private AudioClip freezePenaltySE; // フリーズ音
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        // 現在のフェーズを反映
        if (PhaseController.Instance != null)
            SetPhase(PhaseController.Instance.GetCurrentPhase());
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新
    /// </summary>
    public void SetPhase(Phase phase)
    {
        currentPhase = phase;
        
        if (spriteRenderer != null)
        {
            if (phase == Phase.NotePhase)
            {
                spriteRenderer.color = noteColor;
                if (noteSprite != null) spriteRenderer.sprite = noteSprite;
            }
            else
            {
                spriteRenderer.color = restColor;
                if (restSprite != null) spriteRenderer.sprite = restSprite;
            }
        }
    }
    
    /// <summary>
    /// シェイク検知時に呼び出される
    /// InputManager のイベントを GameManager が購読し、
    /// GameManager から NotePrefab の破裂判定を呼び出す
    /// </summary>
    public void OnNoteHit()
    {
        if (currentPhase == Phase.NotePhase)
        {
            // ✅ 音符をはじけた
            ScoreManager.Instance.AddNoteScore(1);
            PlayBurstEffect();
            Debug.Log("[NotePrefab] ✨ Note hit! +100");
        }
        else if (currentPhase == Phase.RestPhase)
        {
            // ❌ 休符をはじけた（ペナルティ）
            ScoreManager.Instance.SubtractRestPenalty(1);
            GameManager.Instance.TriggerFreeze();
            PlayFreezeEffect();
            Debug.Log("[NotePrefab] ❌ Rest hit! Penalty -50 + Freeze");
        }
        
        // オブジェクト削除
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 破裂エフェクト再生
    /// </summary>
    private void PlayBurstEffect()
    {
        // 1. 爆発音再生
        if (audioSource != null && noteBreakSE != null)
        {
            audioSource.clip = noteBreakSE;
            audioSource.Play();
        }
        
        // 2. パーティクルエフェクト（オプション）
        // ParticleSystem burst = Instantiate(burstParticlePrefab, transform.position, Quaternion.identity);
        // burst.Play();
        
        // 3. スコア表示（オプション）
        // FloatingTextController.Instance.ShowFloatingText("+100", transform.position);
    }
    
    /// <summary>
    /// フリーズペナルティエフェクト再生
    /// </summary>
    private void PlayFreezeEffect()
    {
        // 1. ペナルティ音再生
        if (audioSource != null && freezePenaltySE != null)
        {
            audioSource.clip = freezePenaltySE;
            audioSource.Play();
        }
        
        // 2. フリーズビジュアル（UIManager -> Panel_Warning でホワイトフラッシュ）
        // UIManager.Instance.ShowFreezeFlash();
        
        // 3. スコア表示（オプション）
        // FloatingTextController.Instance.ShowFloatingText("-50", transform.position, Color.red);
    }
}
```

#### 画像・SE 準備リスト

```
Assets/ フォルダ構成:
├── Sprites/
│   ├── note_white.png (白い8分音符 or 16分音符)
│   ├── rest_gray.png (灰色の4分休符)
│   ├── burst_particle.png (破裂パーティクル用スプライト)
│   └── freeze_particle.png (凍結エフェクト用スプライト)
│
├── Audio/SE/
│   ├── note_break.mp3 (破裂音: 軽い・明るい音)
│   ├── rest_penalty.mp3 (ペナルティ音: 低め・ネガティブな音)
│   └── freeze_warning.mp3 (フリーズ警告音: 冷たい感じ)
│
└── Prefabs/
    ├── Note.prefab (メイン：SpriteRenderer版)
    ├── BurstParticle.prefab (破裂パーティクル)
    └── FreezeEffect.prefab (凍結フラッシュエフェクト)
```

---

### 3️⃣ GameConstants.cs 作成

```csharp
// Assets/Scripts/Core/GameConstants.cs
public static class GameConstants
{
    // Serial Communication
    public const string SERIAL_PORT_NAME = "COM3";
    public const int SERIAL_BAUD_RATE = 115200;
    
    // Game Settings
    public const float GAME_DURATION = 60f;              // 60 秒
    public const float PHASE_DURATION = 10f;             // 10 秒ごと切り替え
    public const int SPAWN_RATE_BASE = 5;               // 初期湧き出し: 5個/秒
    public const float LAST_SPRINT_DURATION = 10f;      // ラストスパート: 最後10秒
    public const float LAST_SPRINT_MULTIPLIER = 2f;     // スポーン2倍、スコア2倍
    
    // Scoring
    public const int NOTE_SCORE = 100;                  // 音符スコア
    public const int REST_PENALTY = -50;                // 休符ペナルティ
    public const int PERFECT_BONUS = 500;               // 完璧プレイボーナス
    
    // Visuals
    public const float FREEZE_DURATION = 0.5f;          // フリーズ時間
    public const float FREEZE_TIME_SCALE = 0.2f;        // スローモーション倍率
    
    // Debug
    public const bool DEBUG_MODE = true;                // キーボード入力有効化
    public const bool USE_KEYBOARD_INPUT = true;
}
```

### 4️⃣ BackGroundVideo セットアップ（CameraFarPlane モード）

**背景動画を GameObject として配置する（既に BackGroundVideo がある場合は設定確認）**

```
BackGroundVideo (GameObject)
├── VideoPlayer コンポーネント
│   ├── Render Mode: Camera Far Plane ✅
│   ├── Target Camera: Main Camera
│   ├── Video Clip: background.mp4
│   ├── Loop: true (ループ再生)
│   ├── Play On Awake: true (自動再生)
│   ├── Wait For First Frame: false
│   ├── Skip On Drop: true
│   └── Playback Speed: 1.0
│
└── Canvas (オプション: Raw Image でテクスチャ表示する場合)
    └── RawImage
        ├── Texture: VideoPlayer の Target Texture
        ├── Material: Default
        └── (全画面表示に合わせてリサイズ)
```

**設定手順（Unity Editorで）:**
1. Hierarchy で BackGroundVideo GameObject を選択
2. Inspector → VideoPlayer コンポーネント
3. **Render Mode を "Camera Far Plane" に設定**
   - 画面全体を覆う背景として機能（Canvas の後ろに表示）
4. Target Camera に Main Camera を割り当て
5. Video Clip に 背景動画ファイルを割り当て
6. Loop を ON にして無限ループ設定

**利点：**
- Canvas-based 管理が不要
- 画面解像度に自動的に合わせられる
- Camera レンダリング時に自動的に最背面に配置
- 複数 Canvas を使う際にレイヤー管理が簡単

---

### 5️⃣ InputManager セットアップ（3D版参照）

```csharp
// Assets/Scripts/Input/InputManager.cs（3D版から移植、シンプル化）
public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance => _instance;
    
    // ===== Event =====
    public delegate void OnShakeEvent(int deviceId, int shakeCount, float acceleration);
    public event OnShakeEvent OnShakeDetected;
    
    private SerialPort _serialPort;
    private bool _isSerialConnected;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeSerialPort();
    }
    
    void Update()
    {
        if (_isSerialConnected) ProcessSerialInput();
        if (GameConstants.DEBUG_MODE) ProcessKeyboardInput();
    }
    
    private void ProcessSerialInput()
    {
        if (_serialPort.BytesToRead > 0)
        {
            string line = _serialPort.ReadLine();
            if (int.TryParse(line.Split(',')[0], out int deviceId))
            {
                int shakeCount = int.Parse(line.Split(',')[1]);
                float acceleration = float.Parse(line.Split(',')[2]);
                // ✨ 高速: 直接 Invoke
                OnShakeDetected?.Invoke(deviceId, shakeCount, acceleration);
            }
        }
    }
    
    private void ProcessKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnShakeDetected?.Invoke(0, 1, 1500f);
        }
    }
}
```

### 5️⃣ 実行

```
1. InputManager, GameManager, UIManager, PhaseController を Scene に配置
2. UIManager Inspector で Canvas_Start/Game/Result を割り当て
3. Play ボタン → ゲーム開始
4. Space キー（デバッグモード）で音符/休符処理をテスト
```

---（新設計 v2.0）

### ゲームの基本コンセプト
**1チーム・協力型シェイクゲーム**  
複数プレイヤーが同じゴール達成に向けて協力し、制限時間内に**音符をできるだけ多くはじける**ことで高スコアを狙う。

### ゲーム進行フロー

```
[スタート画面] 
    ↓ (Play ボタン)
[プレイ画面 - ゲーム進行]
    ↓ (時間切れ)
[リザルト画面]
    ↓ (TitleButton: タイトルに戻る)
[スタート画面]
```

### ゲームシーン構成（1シーン管理）
- **1つの Unity Scene**: `Game.unity`
- **3つの Canvas/Panel** の表示・非表示で画面切り替え：
  - `Canvas_Start`: スタート画面（Play ボタン配置）
  - `Canvas_Game`: プレイ画面（ゲームUI、音符表示）
  - `Canvas_Result`: リザルト画面（スコア表示、TitleButton で統一）
  - 背景動画レイヤー（常時再生）

---

## 🎯 ゲームメカニクス詳細

### フェーズシステム（交互に切り替わる）

| フェーズ | 名前 | 動作 | 視覚表現 |
|---------|------|------|---------|
| **Phase A** | 音符フェーズ | 音符（♪）がたくさん生成される。シェイクではじける。加点。 | 白色・キラキラ |
| **Phase B** | 休符フェーズ | 音符がすべて休符（𝄽）に変わる。シェイクするとフリーズ。 | 灰色・暗い |

### 音符の生成・消滅ロジック

#### Phase A: 音符フェーズ（初期 ~60 秒）
1. **音符の湧き出し**
   - ランダムな位置でプリファブ(`NotePrefab`)が生成
   - 初期湧き出し率: 毎秒 5～10 個
   - スコア増加に応じて（ラストスパート時）湧き出し率上昇

2. **シェイク検知時の動作**
   - プレイヤーシェイク（3D InputManager 参照）を検知
   - 画面上の音符をランダムに複数個はじける
   - はじけた音符数 × 加点値（例: 100 点/個）をスコア加算
   - はじけた音符は消滅

#### Phase B: 休符フェーズ（~10 秒間）
1. **フェーズ切り替え時**
   - 時刻が 10 秒単位（例: 10s, 20s, 30s...）に達したら切り替え
   - 画面上のすべての音符が休符に変わる

2. **休符をはじけた場合**
   - 画面全体が一定時間（例: 0.5～1秒）フリーズ
   - フリーズ中は新しい入力を受け付けない
   - 視覚表現: ホワイトフラッシュ + スローモーション効果（Time.timeScale 低下）
   - ペナルティスコア: -50 点程度（休符 1 個ごと）

3. **休符フェーズ終了後**
   - 残された休符が消滅し、新しい音符フェーズへ

---

### スコアリングシステム

| アクション | スコア | 備考 |
|-----------|--------|------|
| 音符をはじける | +100 点/個 | Phase A のみ有効 |
| 休符をはじける | -50 点/個 | Phase B のみペナルティ |
| ラストスパート(最終 10 秒) | ×2 倍率 | 湧き出し率 2 倍 + スコア 2 倍 |
| 完璧プレイ（休符ノーミス） | +500 ボーナス | ゲーム終了時 |

---

### タイマーと時間管理

- **ゲーム制限時間**: 60 秒（カスタマイズ可能）
- **タイマー表示**: Canvas_Game の上部に大きく表示
- **タイムアップ検知**: Time.time または タイマー変数でカウント
- **ラストスパート**: 最後 10 秒で効果音+ビジュアル警告

---

### 背景動画

- **配置**: Canvas の背景レイヤー（深さ -1 など）
- **動作**: ゲーム中常時再生（スタート～リザルト）
- **素材**: `Assets/Media/background-video.mp4` など
- **実装方法**: 
  - `VideoPlayer` コンポーネントを使用
  - または TextureRect に動画テクスチャをループ再生

---

## 📐 詳細な UI・シーン構成

### Canvas 構造（Hierarchy）

```
Game.unity
├── Camera (Main Camera)
├── Canvas_Background
│   └── Video Player (背景動画)
│
├── Canvas_Start
│   ├── Title Text
│   ├── Play Button
│   └── Settings Button (オプション)
│
├── Canvas_Game
│   ├── Panel_Header
│   │   ├── Timer Text (残り時間)
│   │   ├── Score Text (現在スコア)
│   │   └── Phase Indicator (現在フェーズ)
│   │
│   ├── Panel_Notes (音符・休符表示エリア)
│   │   └── (複数の Note Prefab がInstantiate される)
│   │
│   └── Panel_Warning (フリーズ時のフラッシュ画像)
│
├── Canvas_Result
│   ├── ResultTitle Text
│   ├── FinalScore Text
│   ├── Ranking Panel (スコアランキング: ローカル保存)
│   └── TitleButton （スタート画面に戻る）
│
└── Audio Sources (効果音・BGM)
    ├── SE_NotePopup
    ├── SE_Shake
    ├── SE_Freeze
    ├── SE_TimerWarning
    └── BGM_Background
```

---

## 🔧 スクリプト設計（刷新版）

### スクリプト構成（シンプル化）

```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs          # ゲーム進行・フェーズ管理
│   └── InputManager.cs         # Serial 通信・入力管理（3D版参照）
│
├── Game/
│   ├── NotePrefab.cs           # 音符・休符オブジェクト（個別制御）
│   ├── PhaseController.cs      # Phase A/B 交互切り替え
│   └── ScoreManager.cs         # スコア計算・管理
│
├── UI/
│   ├── UIManager.cs            # Canvas 表示・非表示管理
│   ├── TimerDisplay.cs         # タイマー表示更新
│   └── ScoreDisplay.cs         # スコア表示更新
│
└── Utils/
    └── GameConstants.cs        # 定数管理（ポート、タイムリミット等）
```

### 主要スクリプトの責務

#### 1. **InputManager.cs** (3D版から刷新)
```csharp
// 概要: Serial通信とキーボード入力を処理
// 速度重視: EventInvokeの直接呼び出し

public delegate void OnShakeEvent(int deviceId, int shakeCount, float acceleration);
public event OnShakeEvent OnShakeDetected;

// Serial受信時:
void ProcessSerialInput()
{
    // パース → OnShakeDetected?.Invoke(deviceId, count, accel);
}

// キーボード(デバッグ):
void ProcessKeyboardInput()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        OnShakeDetected?.Invoke(0, 1, 1500f);
    }
}
```

#### 2. **GameManager.cs** (中枢)
```csharp
// 概要: ゲーム全体の進行制御

public class GameManager : MonoBehaviour
{
    private float gameTimer;
    private bool isGameRunning;
    private Phase currentPhase; // Phase.NotePhase / Phase.RestPhase
    
    void Start() { gameTimer = 60f; isGameRunning = true; }
    void Update() 
    { 
        UpdateTimer();
        CheckPhaseChange();
        if (gameTimer <= 0) EndGame();
    }
    
    // InputManager からイベント受け取り:
    void OnInputReceived(int deviceId, int shakeCount, float accel)
    {
        if (!isGameRunning) return;
        
        // Phase A なら音符をはじける
        if (currentPhase == Phase.NotePhase)
        {
            int hittedCount = PopNotes(shakeCount); // 音符はじけ判定
            ScoreManager.AddScore(hittedCount * 100);
        }
        
        // Phase B なら フリーズ
        else if (currentPhase == Phase.RestPhase)
        {
            int hitRest = PopRests(shakeCount); // 休符をはじけた数
            ScoreManager.AddScore(-(hitRest * 50));
            TriggerFreeze(0.5f); // 0.5秒フリーズ
        }
    }
}
```

#### 3. **PhaseController.cs** (フェーズ管理)
```csharp
// 概要: Phase A ↔ Phase B の自動切り替え

public enum Phase { NotePhase, RestPhase }

public class PhaseController : MonoBehaviour
{
    private Phase currentPhase = Phase.NotePhase;
    private float phaseTimer = 0f;
    private float phaseDuration = 10f; // 10秒ごと切り替え
    
    void Update()
    {
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= phaseDuration)
        {
            SwitchPhase();
            phaseTimer = 0f;
        }
    }
    
    void SwitchPhase()
    {
        currentPhase = (currentPhase == Phase.NotePhase) 
            ? Phase.RestPhase 
            : Phase.NotePhase;
        
        // すべての Note を NotePhase/RestPhase に変更
        NotePrefab[] allNotes = FindObjectsOfType<NotePrefab>();
        foreach (var note in allNotes)
        {
            note.SetPhase(currentPhase);
        }
    }
}
```

#### 4. **NotePrefab.cs** (音符・休符個別制御)
```csharp
// 概要: 音符オブジェクトの動作定義

public enum Phase { NotePhase, RestPhase }

public class NotePrefab : MonoBehaviour
{
    private Phase currentPhase;
    private Image image; // 見た目変更用
    
    public void SetPhase(Phase phase)
    {
        currentPhase = phase;
        if (phase == Phase.NotePhase)
            image.color = Color.white; // 白
        else
            image.color = Color.gray; // グレー
    }
    
    // GameManager から呼び出される
    public bool TryHit()
    {
        // オブジェクト消滅
        Destroy(gameObject);
        return true;
    }
}
```

#### 5. **UIManager.cs** (Canvas 管理)
```csharp
// 概要: 画面遷移を管理（ステート機)

public enum GameState { Start, Playing, Result }

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvasStart, canvasGame, canvasResult;
    
    void ShowStartScreen() { ActivateOnly(canvasStart); }
    void ShowGameScreen() { ActivateOnly(canvasGame); }
    void ShowResultScreen() { ActivateOnly(canvasResult); }
    
    void ActivateOnly(Canvas target)
    {
        canvasStart.gameObject.SetActive(target == canvasStart);
        canvasGame.gameObject.SetActive(target == canvasGame);
        canvasResult.gameObject.SetActive(target == canvasResult);
    }
}
```

---

### Singleton パターンの改善（重要）

#### 問題点（旧設計）
すべての Manager スクリプトの Singleton プロパティで、**不要な自動生成ロジック** が含まれていました：

```csharp
// ❌ 旧設計（不要な AddComponent ロジック）
public static GameManager Instance
{
    get
    {
        if (_instance == null)
        {
            _instance = FindObjectOfType<GameManager>();
            if (_instance == null) 
            { 
                // 💥 不要: Scene に手動で配置しているのに実行時に新規生成！
                GameObject go = new GameObject("GameManager"); 
                _instance = go.AddComponent<GameManager>(); 
            }
        }
        return _instance;
    }
}
```

**問題：**
- Scene に既に Manager GameObject が存在するため、AddComponent による自動生成は無駄
- 重複生成の可能性（デバッグ時の混乱原因）
- コードの意図が不明確

#### 解決策（新設計 ✅ 完了）
すべての Manager から AddComponent ロジックを削除し、**FindObjectOfType のみ** を使用：

```csharp
// ✅ 新設計（シンプル＆効率的）
public static GameManager Instance
{
    get
    {
        if (_instance == null) { _instance = FindObjectOfType<GameManager>(); }
        return _instance;
    }
}
```

**メリット：**
- コードがシンプル（1行に短縮）
- Scene 設計の意図が明確（Manager は手動配置）
- 重複生成の心配なし
- 3～5 行のコード削減（5つの Manager × ~10行 = ~50行削減）

#### 修正対象スクリプト（すべて完了 ✅）
1. ✅ `GameManager.cs` - AddComponent 削除（27行 → 11行）
2. ✅ `InputManager.cs` - AddComponent 削除（23行 → 13行）
3. ✅ `UIManager.cs` - AddComponent 削除（34行 → 14行）
4. ✅ `PhaseController.cs` - AddComponent 削除（23行 → 13行）
5. ✅ `ScoreManager.cs` - AddComponent 削除（20行 → 10行）

**前提条件：**
- Scene（Game.unity）に Manager GameObject が **事前に作成** されていること
- Singleton は Scene に存在するオブジェクトを見つけるのみ

---

### 削除対象スクリプト（旧設計から削除）

以下は新設計では不要なため削除：

- ✅ `VideoManager.cs` (背景動画管理 → VideoPlayer で直接制御)
- ✅ `SoundManager.cs` (効果音管理 → AudioSource.PlayOneShot() で直接制御)
- ✅ `BattleGameMode.cs` (2チーム対戦ロジック)
- ✅ `GameMode.cs` (ベースクラス)
- ✅ `SerialDataParser.cs` (パーサー)
- ✅ `VictoryManager.cs` (勝敗判定)
- ✅ `CommandSender.cs` (LED送信)

---

### Serial 通信の簡略化

#### 旧設計: 複数デバイス状態管理（重い）
```csharp
// ❌ 削除: 複雑な辞書管理
private Dictionary<int, DeviceInputState> _deviceStates;
```

#### 新設計: イベント直接呼び出し（速い）
```csharp
// ✅ 採用: シンプル＆高速
public event OnShakeEvent OnShakeDetected;

void ProcessSerialInput()
{
    // 1行データ受信: "deviceId,shakeCount,acceleration"
    OnShakeDetected?.Invoke(deviceId, shakeCount, acceleration);
    // → GameManager が受け取る
}
```

---

## ✨ 実装上の妥当性検討

### 提案内容の評価

| 項目 | 妥当性 | コメント |
|------|--------|---------|
| 1チーム形式 | ✅ **高** | 複数プレイヤーの協力感が出る。導入が簡単。|
| 音符はじけメカニクス | ✅ **高** | フィードバック明確。直感的。|
| フェーズシステム | ✅ **高** | 単純で分かりやすい。戦略性も付く。 |
| フリーズペナルティ | ✅ **高** | 視覚的インパクト大。プレイヤー集中力UP。|
| 1シーン+Canvas切り替え | ✅ **高** | 軽量。ロード時間ゼロ。推奨方式。 |
| 背景動画常時再生 | ⚠️ **中** | 動画解像度によって負荷増加。最適化要検討。 |
| Event Invoke方式 | ✅ **高** | 速度重視・シンプル。正解。 |

### 補足：性能上の考慮

**音符生成の負荷を抑えるコツ:**
```csharp
// ❌ 毎フレーム: Instantiate (重い)
void Update() { if(Random.value < 0.1f) Instantiate(notePrefab); }

// ✅ CoroutineまたはInvokeで時間間隔制御 (軽い)
private IEnumerator SpawnNotes()
{
    while(isGameRunning)
    {
        for(int i=0; i<currentSpawnRate; i++)
            Instantiate(notePrefab, GetRandomPos(), Quaternion.identity);
        yield return new WaitForSeconds(1f);
    }
}
```

---

## 🎨 NotePrefab の詳細実装ガイド

### NotePrefab.cs スクリプト（世界座標空間版）

```csharp
using UnityEngine;

public enum Phase { NotePhase, RestPhase }

public class NotePrefab : MonoBehaviour
{
    // === 表示系 ===
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite noteSprite;      // 白い音符
    [SerializeField] private Sprite restSprite;      // 灰色の休符
    [SerializeField] private Color noteColor = Color.white;
    [SerializeField] private Color restColor = Color.gray;
    
    // === 音響 ===
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip burstSoundClip;   // 音符がはじける音
    [SerializeField] private AudioClip penaltySoundClip; // 休符をはじけた時の音
    
    // === パーティクル ===
    [SerializeField] private ParticleSystem burstParticles;
    [SerializeField] private ParticleSystem penaltyParticles;
    
    // === 内部状態 ===
    private Phase currentPhase = Phase.NotePhase;
    private GameManager gameManager;
    private ScoreManager scoreManager;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        scoreManager = FindObjectOfType<ScoreManager>();
        
        // 初期フェーズを設定
        SetPhase(Phase.NotePhase);
    }
    
    /// <summary>
    /// フェーズを切り替え（色・スプライト変更）
    /// </summary>
    public void SetPhase(Phase phase)
    {
        currentPhase = phase;
        
        if (phase == Phase.NotePhase)
        {
            spriteRenderer.sprite = noteSprite;
            spriteRenderer.color = noteColor;
        }
        else // Phase.RestPhase
        {
            spriteRenderer.sprite = restSprite;
            spriteRenderer.color = restColor;
        }
    }
    
    /// <summary>
    /// 音符がはじけた時の処理（GameManager から呼び出される）
    /// </summary>
    public void OnNoteHit()
    {
        if (currentPhase == Phase.NotePhase)
        {
            // 正しく音符をはじけた
            PlayBurstEffect();
            scoreManager.AddNoteScore(1);
        }
        else if (currentPhase == Phase.RestPhase)
        {
            // 誤って休符をはじけた
            PlayPenaltyEffect();
            scoreManager.SubtractRestPenalty(1);
            gameManager.TriggerFreeze();
        }
        
        // いずれの場合もオブジェクト破棄
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 破裂エフェクト（音符をはじけた時）
    /// </summary>
    private void PlayBurstEffect()
    {
        // 効果音
        if (burstSoundClip && audioSource)
        {
            audioSource.PlayOneShot(burstSoundClip);
        }
        
        // パーティクル
        if (burstParticles)
        {
            burstParticles.Play();
        }
    }
    
    /// <summary>
    /// ペナルティエフェクト（休符をはじけた時）
    /// </summary>
    private void PlayPenaltyEffect()
    {
        // ペナルティ音
        if (penaltySoundClip && audioSource)
        {
            audioSource.PlayOneShot(penaltySoundClip);
        }
        
        // パーティクル
        if (penaltyParticles)
        {
            penaltyParticles.Play();
        }
    }
}
```

### Unity Editor での NotePrefab 作成手順

#### **Step 1: GameObject 作成**

1. Hierarchy ウィンドウで右クリック
2. **Create Empty** を選択
3. 名前を **Note** に変更
4. Transform リセット（Position: 0, 0, 0 / Scale: 1, 1, 1）

#### **Step 2: SpriteRenderer 追加**

1. **Add Component** → **SpriteRenderer**
2. **Sprite** に「白い音符」画像を割り当て
3. **Color** を白（White）に設定
4. **Order in Layer** = 5（Canvas より上に表示）

#### **Step 3: AudioSource 追加**

1. **Add Component** → **Audio Source**
2. **Spatial Blend** = 0（2D音声）
3. **Volume** = 0.7
4. 初期クリップは設定しない（スクリプトから PlayOneShot で再生）

#### **Step 4: NotePrefab.cs スクリプト割り当て**

1. **Add Component** → **Script** → **NotePrefab**

#### **Step 5: ParticleSystem 追加（オプション）**

2つのパーティクルシステムを用意：

**「破裂」パーティクル（notePopEffect）:**
- Emission: Rate = 10/sec
- Initial Velocity: Y = 3 m/s（上向き）
- Lifetime: 0.5～1秒
- 色: 白から透明へ（フェード）

**「ペナルティ」パーティクル（penaltyFlashEffect）:**
- Emission: Rate = 20/sec
- Initial Velocity: Randomized = 2 m/s
- Lifetime: 0.3～0.5秒
- 色: 赤から透明へ（警告表現）

#### **Step 6: Inspector で参照を割り当て**

NotePrefab.cs スクリプトの以下項目を埋める：

```
Sprite Renderer: (このGameObjectのSpriteRenderer)
Note Sprite: Assets/Media/Sprites/note_white.png
Rest Sprite: Assets/Media/Sprites/rest_gray.png
Note Color: White
Rest Color: Gray (128, 128, 128)

Audio Source: (このGameObjectのAudioSource)
Burst Sound Clip: Assets/Media/Audio/burst.wav
Penalty Sound Clip: Assets/Media/Audio/penalty.wav

Burst Particles: (notePopEffect ParticleSystem)
Penalty Particles: (penaltyFlashEffect ParticleSystem)
```

#### **Step 7: Prefab として保存**

1. Note GameObject をドラッグして、**Assets/Prefabs/** フォルダにドロップ
2. ファイル名: **Note.prefab**
3. Hierarchy から元の GameObject を削除

#### **Step 8: GameManager で Prefab 参照を設定**

GameManager スクリプトの Inspector で：
```
Note Prefab: Assets/Prefabs/Note.prefab
Spawn Container: Canvas_Game の Panel_Notes
```

---

### アセットフォルダ構成

以下の構造でスプライト・音声ファイルを整理してください：

```
Assets/
├── Media/
│   ├── Sprites/
│   │   ├── note_white.png        # 音符（白、キラキラ）
│   │   ├── rest_gray.png         # 休符（灰色）
│   │   ├── freeze_flash.png      # フリーズ時のホワイトフラッシュ（オプション）
│   │   └── particle_burst.png    # パーティクル用スプライト
│   │
│   ├── Audio/
│   │   ├── burst.wav             # 音符がはじける音（125ms 程度）
│   │   ├── penalty.wav           # 休符をはじけた時の警告音（250ms 程度）
│   │   ├── timer_warning.wav     # 最後 10 秒の警告音（ピッ）
│   │   ├── freeze_effect.wav     # フリーズ時のホワイトノイズ（500ms）
│   │   └── bgm_background.mp3    # BGM（ループ）
│   │
│   └── Videos/
│       └── background.mp4        # 背景動画（1920×1080, 60fps推奨）
│
└── Prefabs/
    └── Note.prefab               # 音符オブジェクト
```

---

## 🚀 次のステップ（実装優先度）

1. **Phase 1: 基盤構築** (1～2週間)
   - [ ] GameManager.cs 実装（ゲーム進行制御）
   - [ ] InputManager.cs 刷新（3D版参照、イベントベース）
   - [ ] UIManager.cs 実装（Canvas管理）
   - [ ] 既存スクリプト削除

2. **Phase 2: ゲームロジック** (2～3週間)
   - [ ] NotePrefab.cs 実装
   - [ ] PhaseController.cs 実装
   - [ ] ScoreManager.cs 実装
   - [ ] フリーズ効果実装（Time.timeScale操作）

3. **Phase 3: ビジュアル＆サウンド** (2週間)
   - [ ] 音符・休符の見た目（色、アニメーション）
   - [ ] 効果音実装（Pop音、Freeze音、Timer警告音）
   - [ ] フリーズ時のホワイトフラッシュ
   - [ ] 背景動画統合

4. **Phase 4: テスト＆最適化** (1週間)
   - [ ] 複数デバイス同時接続テスト
   - [ ] 60秒ゲームプレイテスト
   - [ ] 性能プロファイリング
   - [ ] バグ修正

### キーボード (デバッグモード)
```
Space: シェイク検知をシミュレート
```

---

## ⚠️ 既知の問題と制限事項（新設計版）

### 実装予定時の考慮事項

| 項目 | 状況 | 説明 |
|------|------|------|
| **複数デバイス同時接続** | ✅ 対応予定 | 最大10プレイヤー |
| **音符スポーンパフォーマンス** | ⚠️ 検討中 | 大量生成時のGC最適化必要 |
| **背景動画再生** | ⚠️ 要最適化 | 動画ファイルサイズに注意 |
| **フリーズエフェクト** | ✅ 実装予定 | Time.timeScale + ホワイトフラッシュ |
| **ランキング保存** | ✅ 実装予定 | PlayerPrefs または JSON ファイル |

---

## 🔄 実装フェーズ（新設計に基づく）

```
Phase 1: 基盤構築（2025-11月中旬）
  ✅ GameConstants.cs 作成
  ✅ InputManager.cs 刷新（イベント方式）
  ⏳ GameManager.cs 新規実装（1チーム協力型）
  ⏳ UIManager.cs 改修（3Canvas管理）
  ⏳ Game.unity Scene セットアップ
  
Phase 2: ゲームロジック（2025-11月下旬）
  ⏳ PhaseController.cs 実装（10秒ごと切り替え）
  ⏳ NotePrefab.cs 実装（音符・休符オブジェクト）
  ⏳ ScoreManager.cs 実装（スコア計算）
  ⏳ フリーズ機能実装（Time.timeScale操作）
  
Phase 3: ビジュアル＆オーディオ（2025-12月上旬）
  ⏳ 音符・休符のビジュアル（色、アニメーション）
  ⏳ 効果音実装（Pop, Freeze, Warning, BGM）
  ⏳ ホワイトフラッシュエフェクト
  ⏳ 背景動画統合
  
Phase 4: テスト＆最適化（2025-12月中旬）
  ⏳ 複数デバイス接続テスト
  ⏳ 60秒ゲームフルテスト
  ⏳ パフォーマンスプロファイリング
  ⏳ バグ修正・チューニング
```

---

## 📚 関連ドキュメント

- [プロジェクト全体の開発履歴](../../docs/DEVELOPMENT.md)
- [セットアップガイド](../../docs/SETUP.md)
- ~~**本体版（3D）:** `../shake-game-3d/README.md`~~

---

## 📝 開発者向けメモ


## ✅ テストチェックリスト（新設計版）

ゲーム実装後に確認してください：

### 基本機能テスト
- [ ] Serial 通信が正常に接続される
- [ ] キーボード入力（Space）でデバッグ入力可能
- [ ] Canvas_Start / Canvas_Game / Canvas_Result が正しく切り替わる

### ゲームプレイテスト
- [ ] Play ボタン → ゲーム開始 → 60秒カウントダウン開始
- [ ] 音符が画面に湧き出す（毎秒5個以上）
- [ ] シェイク検知 → 音符がはじける → スコア加算 + 破裂エフェクト・音声
- [ ] 10秒ごとに Phase A (音符) / Phase B (休符) が切り替わる
- [ ] 休符をはじけるとペナルティ (-50点) + フリーズ (0.5秒) + ペナルティ音
- [ ] 60秒経過 → タイムアップ
- [ ] リザルト画面に最終スコア表示
- [ ] Title ボタンでスタート画面に戻る

### パフォーマンステスト
- [ ] 最大50個の音符同時表示でも 60fps 維持
- [ ] 複数デバイス（最大5個）同時入力でもラグなし
- [ ] メモリリーク無し（1時間連続プレイ）

### 複数デバイステスト
- [ ] 2個のデバイスから同時入力可能
- [ ] 各デバイスの入力が正常に処理される
- [ ] スコア合算が正しく計算される

---

## 🆘 トラブルシューティング（新設計版）

**Serial ポートに接続できない:**
```
→ Arduino IDE / VS Code のシリアルモニタを閉じてください
→ GameConstants.SERIAL_PORT_NAME がデバイスに合っているか確認
→ GameConstants.DEBUG_MODE = true にしてキーボード入力で動作確認
```

**音符が湧き出さない:**
```
→ PhaseController.cs が Scene にあるか確認
→ Canvas_Game が Active か確認
→ NotePrefab が Assets/Prefabs/ に存在するか確認
→ SpawnNotes() Coroutine が実行されているか Debug.Log で確認
```

**フリーズが動作しない:**
```
→ Time.timeScale が正しく操作されているか確認
→ Panel_Warning (ホワイトフラッシュ画像) が配置されているか確認
→ Canvas_Game の Render Mode が ScreenSpace-Overlay か確認
```

**スコアが表示されない:**
```
→ ScoreManager.cs が Scene にあるか確認
→ ScoreDisplay.cs が Canvas_Game の Score Text に割り当てられているか確認
→ GameManager.Instance.AddScore() が呼び出されているか Debug.Log で確認
```

**複数デバイスで入力競合:**
```
→ InputManager.OnShakeDetected イベントが複数リスナー登録されているか確認
→ GameManager.OnInputReceived() が各デバイスの入力を正しく処理しているか確認
```

---

## 📞 ご質問・バグ報告

問題が発生した場合は、GitHub Issues で報告してください。

---

**作成日:** 2025-11月  
**更新日:** 2025-11-15（✅ 実行完了：VideoManager/SoundManager削除、Singleton AddComponent削除、コード簡潔化、README更新）  
**作成者:** ユーザー & GitHub Copilot  
**ステータス:** ✨ コード実装完了、Scene セットアップ段階へ移行
