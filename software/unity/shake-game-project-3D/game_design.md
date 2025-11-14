# ゲーム設計書（未定）
このドキュメントは今後のゲーム設計を記録します。

## 現在の検討事項
- 詳細なゲームシステム

## 更新予定

具体的なゲーム設計が決定次第、このドキュメントを更新します。

## 詳細
- ボス(トットムジカ)を倒す。ボス戦的形式
- 班(10人)で協力してプレイする。
- プレイ時間は30秒くらい
- デバイスはフリフリするやつ(加速度センサ＆LED)
- 音楽要素
- フェーズ展開があったほうがいいかどうか
### フリフリ→行動のつながり方。
1. フリフリ回数→味方キャラのエネルギーなど→味方キャラがボスを攻撃
   1. キャラ選択できるようにする？
2. フリフリ→味方キャラの攻撃行動
   1. フリフリの数＝味方キャラの数？
3. ~~フリフリ→ダメージ(味方キャラは登場しない)~~
### 協力要素はどう入れるか？
1. (全員で、2人で)タイミングをそろえる。(同時攻撃だ！)
2. ~~役割分担(タンク・アタッカーとかとか)→ワンピースのバトルってそういうのないかも~~
3. ~~役割分担その二(ダウンしたキャラクターの回復、技の繰り出しとか、攻撃部位とか)→シェイク入力だけだと難しい~~
### 音楽要素はどこに入れるか？
1. みんなでタイミングをそろえる。
2. 曲のカウントに合わせる(参:異次元獣戦ラスト)
3. 指定リズムでコールアンドレスポンス(リズム天国)
### 勝ったらどうなる？
1. 特別映像？
### フェーズ展開
1. まず雑魚敵処理→ボス戦
2. 部位破壊
3. ボスの第2形態的な(同時攻撃しか通らなくなる)

## 参考ゲーム
- ワンピース
  - 海賊無双 [https://oppw4-20.bn-ent.net/action/bossbattle/]
  - オデッセイ
  - トレジャークルーズ
- 音ゲー
  - 太鼓の達人
  - プロセカ
  - リズム天国
- ネプリーグのイメージ

## 仮仕様版仕様
- ボス(トットムジカ)を倒す。ボス戦的形式
- 複数子機対応
- 数字キーでフリフリのダミー信号をやって動作チェックできるようにする
- 曲が流れて太鼓の達人やリズム天国のようにタイミングに合わせて振る。連打部分もある。
- 手前に味方キャラとそのエネルギーのゲージ。奥に巨大ボス(トットムジカ)がいる(3Dモデル)一番上に残りHPのバーが現れる
- モデルがないやつはとりあえず画像を表示　最終的にはモーションをつけたい
- シリアル通信はchildID,count,acceralationのcsv形式のデータ。
- 時々ボスの攻撃が来るので全員でタイミングを合わせて振ることで防ぐ。
- 振るタイミングはデバイスのLEDが光ってお知らせ(firmwareの更新が必要)

## 2025-11-13 20:03:50
- ゲーム時間を30秒にして思ってたものより短く・シンプルなものにする方針
- ボスが現れる
- シェイクしてダメージを与える
- みんなのタイミングが合えば大ダメージorリズムに合えば大ダメージ
- 全力でシェイクするフェーズとクールタイムが発生してタイミングをちゃんと見ないといけないフェーズ

- 雑魚キャラが現れて練習モード

---

# 最小機能版 詳細設計（MVP - Minimum Viable Product）

## 1. ゲーム概要

**タイトル**: Shake Together - Boss Battle

**プレイ時間**: 30秒

**プレイ人数**: 1～10人（複数のESP32シェイクデバイスでリアルタイム通信）

**概要**: 複数のプレイヤーが同時にシェイクデバイスを振り、タイミングを合わせてボスにダメージを与えるリズムゲーム要素を持つ協力型ボス戦。

---

## 2. ゲームの流れ（ユーザージャーニー）

```
[タイトル画面] 
    ↓ (Enterキーまたはシェイク開始)
[ゲーム開始]
    ↓
[ステージ1: 練習フェーズ（5秒）]
  - 雑魚敵が登場し、タイミングの合わせ方を学習
  - 全員で同じタイミングで振るとダメージ表示
    ↓
[ステージ2: ボス戦フェーズ（25秒）]
  - トットムジカが登場
  - 2つの攻撃パターンが交互に発生:
    a) 【高ダメージ期（15秒）】- クールタイムなし
       - 画面下部にタイミング表示（ビート可視化）
       - いつでも振ってOK、振る回数が多いほどダメージ
    b) 【同期期（10秒）】- クールタイムあり
       - タイミングインジケータが表示
       - 全員で同じタイミングで振ると大ダメージ
    ↓
[ゲーム終了画面]
  - ボスHP: 0 → 「勝利！」画面
  - ボスHP: >0 → 「敗北...」画面
  - スコア表示（総シェイク回数、同期成功回数）
    ↓
[タイトルに戻る]
```

---

## 3. ゲーム画面構成

### 3.1 ゲーム画面レイアウト

```
┌─────────────────────────────────────────────────┐
│  [HP Bar - ボスHP進捗表示]     [Time: 0:30]     │ ← UI層
├─────────────────────────────────────────────────┤
│                                                 │
│           [ボス 3Dモデル/画像]                  │ ← メインゲーム層
│                                                 │
│  ┌──────────────┐  ┌──────────────┐           │
│  │ Player 1     │  │ Player 2     │  ...    │ ← プレイヤーステータス
│  │ ████░░░░░░░░│  │ ████░░░░░░░░│           │
│  └──────────────┘  └──────────────┘           │
│                                                 │
│     [シェイク検知表示]  [タイミングインジケータ] │ ← フィードバック層
├─────────────────────────────────────────────────┤
│  Shake Count: 1234  |  Sync Success: 5/10     │ ← デバッグ情報
└─────────────────────────────────────────────────┘
```

### 3.2 キャンバスヒエラルキー（Canvas構成）

```
Canvas (WorldSpace or ScreenSpace)
├── HUD_Panel
│   ├── HPBar_Boss (Image: ボスHP)
│   ├── TimerText (Text: 残り時間)
│   └── PhaseIndicator (Text: 現在フェーズ)
├── MainGameWorld
│   ├── Boss (3DModel or Sprite)
│   ├── PlayerStatusDisplay
│   │   ├── PlayerSlot_1
│   │   │   ├── PlayerNameText
│   │   │   └── EnergyBar (Image)
│   │   ├── PlayerSlot_2
│   │   ├── PlayerSlot_3 ...
│   └── EffectsLayer
│       ├── DamagePopup (TextMesh)
│       ├── ShakeFeedback (VFX)
│       └── SyncIndicator (Glow Effect)
├── FeedbackPanel
│   ├── TimingIndicator (Slider: ビート表示)
│   ├── ShakeDetectionIndicator (Visual: フリフリ検知)
│   └── CurrentPhaseLabel
└── DebugPanel (Devel Only)
    ├── ShakeCountText
    └── SyncSuccessCountText
```

---

## 4. 主要スクリプト・システム設計

### 4.1 システムアーキテクチャ図

```
┌─────────────────────────────────────────────────────┐
│                    GameManager (Singleton)          │
│  - ゲーム全体の状態管理・フロー制御               │
└────┬────────────────────────────────────────────────┘
     │
     ├─→ [InputManager]
     │    - SerialPort通信からデータ受信
     │    - CSV形式パース (ChildID, ShakeCount, Acceleration)
     │    - フリフリイベント発火
     │
     ├─→ [PhaseController]
     │    - ゲームフェーズ管理（練習/ボス戦）
     │    - 攻撃パターン切り替え
     │    - タイミングジェネレータ
     │
     ├─→ [BossController]
     │    - ボスのHP・状態管理
     │    - ダメージ計算
     │    - アニメーション制御
     │
     ├─→ [PlayerManager]
     │    - マルチプレイヤー管理
     │    - 各プレイヤーのデータ統合
     │    - UI表示用データ提供
     │
     ├─→ [TimingSystem]
     │    - リズムビート生成
     │    - 同期判定ロジック
     │    - タイミングウィンドウ管理
     │
     ├─→ [UIManager]
     │    - HP BAR更新
     │    - タイミングインジケータ表示
     │    - スコア表示
     │
     └─→ [AudioManager]
          - BGM再生
          - シェイク音SFX
          - ボス攻撃SE
```

### 4.2 主要スクリプト一覧

#### **Core Managers**

| スクリプト | 責務 | 主要メソッド |
|-----------|------|-----------|
| `GameManager.cs` | ゲーム全体の状態・流れ制御 | `StartGame()`, `Update()`, `EndGame()` |
| `InputManager.cs` | SerialPort通信・デバイス入力 | `OnShakeDetected()`, `ParseSerialData()` |
| `AudioManager.cs` | BGM/SE再生 | `PlayBGM()`, `PlaySFX()` |
| `UIManager.cs` | UI全体の更新 | `UpdateHPBar()`, `UpdateTimer()`, `ShowDamage()` |

#### **Game Logic**

| スクリプト | 責務 | 主要メソッド |
|-----------|------|-----------|
| `PhaseController.cs` | ゲームフェーズ管理 | `SwitchPhase()`, `GetCurrentPhase()` |
| `BossController.cs` | ボスHP・ダメージ処理 | `TakeDamage()`, `UpdateHP()`, `PlayAttackAnimation()` |
| `PlayerManager.cs` | マルチプレイヤー管理 | `RegisterPlayer()`, `GetPlayerData()`, `UpdatePlayerUI()` |
| `TimingSystem.cs` | リズム・同期判定 | `GenerateBeat()`, `CheckTiming()`, `CalculateSyncBonus()` |

#### **UI/Display**

| スクリプト | 責務 | 主要メソッド |
|-----------|------|-----------|
| `HPBarUI.cs` | ボスHP表示 | `SetHP()`, `UpdateVisuals()` |
| `PlayerStatusUI.cs` | プレイヤーステータス表示 | `SetPlayerCount()`, `UpdatePlayerBar()` |
| `TimingIndicatorUI.cs` | タイミング表示 | `ShowTiming()`, `AnimateBeat()` |
| `ResultScreenUI.cs` | 結果画面表示 | `ShowVictory()`, `ShowDefeat()`, `ShowScore()` |

#### **その他**

| スクリプト | 責務 | 主要メソッド |
|-----------|------|-----------|
| `GameConstants.cs` | ゲーム定数定義 | (定数のみ) |
| `DebugConsole.cs` | デバッグ表示 | `Log()`, `UpdateDebugInfo()` |

---

## 5. ゲーム状態遷移図

```
State Machine: GameManager.cs

[TITLE] 
  ↓ (OnStartGame)
[LOADING]
  ↓ (リソース読込完了)
[GAME_RUNNING]
  ├─→ [PHASE_PRACTICE] (0-5秒)
  │    - 雑魚敵登場
  │    - 通常攻撃パターン
  │    ↓ (時間経過)
  │   
  ├─→ [PHASE_BOSS_HIGH_DAMAGE] (5-20秒)
  │    - ボス登場
  │    - 常時ダメージ受け付け
  │    - タイミング表示のみ
  │    ↓ (時間経過)
  │    
  ├─→ [PHASE_BOSS_SYNC] (20-30秒)
  │    - 同期攻撃要求
  │    - タイミングウィンドウ表示
  │    - 同期成功で大ダメージ
  │    ↓ (時間経過 or ボスHP=0)
  │
  ├→ [VICTORY] (ボスHP ≤ 0)
  │
  └→ [DEFEAT] (時間切れ)
      ↓
    [RESULT]
      ↓ (Enterキー)
    [TITLE]
```

---

## 6. データフロー・通信仕様

### 6.1 SerialPort通信フォーマット

**入力（ESP32 → PC）:**
```
ChildID,ShakeCount,Acceleration\n
1,42,1250\n
2,38,980\n
3,45,1320\n
```

| フィールド | 型 | 範囲 | 説明 |
|-----------|-----|-----|------|
| ChildID | int | 1-10 | デバイスID |
| ShakeCount | int | 0-65535 | 加速度が閾値超過した回数 |
| Acceleration | float | 0-4096 | 現在の加速度値 |

**出力（PC → ESP32 - 将来対応）:**
```
LED_MODE,intensity\n
例: "GLOW,255\n" / "BLINK,128\n"
```

### 6.2 ゲーム内データ構造

#### **Player データ**
```csharp
public class PlayerData
{
    public int childId;
    public int shakeCount;
    public float acceleration;
    public float lastUpdateTime;
    public float energyLevel; // UI表示用
}
```

#### **Timing イベント**
```csharp
public class TimingEvent
{
    public float eventTime;      // ゲーム内時刻
    public TimingType type;      // NORMAL / SYNC / BOSS_ATTACK
    public float windowStart;    // 判定ウィンドウ開始
    public float windowEnd;      // 判定ウィンドウ終了
    public bool isSuccessful;    // 判定結果
}
```

#### **Boss データ**
```csharp
public class BossData
{
    public float maxHP;
    public float currentHP;
    public float damagePerShake;
    public float syncBonusMultiplier;
    public BossPhase currentPhase;
}
```

---

## 7. 最小機能版の実装スコープ

### ✅ 実装予定（MVP）

- [ ] **基本ゲームループ**
  - ゲーム開始→30秒カウント→結果表示
  - 状態遷移管理

- [ ] **入力システム**
  - SerialPort通信受信
  - CSV形式パース
  - フリフリイベント発火

- [ ] **ボス戦闘**
  - ボスモデル表示（3Dモデルまたはプレースホルダ画像）
  - HP管理・ダメージ計算
  - ダメージポップアップ表示

- [ ] **マルチプレイヤー**
  - 複数プレイヤーのデータ統合（最大10人）
  - プレイヤーステータスUI表示
  - 個別HPバー表示

- [ ] **タイミングシステム**
  - リズムビート生成（BPM = 120想定）
  - タイミングウィンドウ判定
  - 同期判定ロジック

- [ ] **UI**
  - ボスHP BAR
  - 残り時間表示
  - タイミングインジケータ（ビート表示）
  - プレイヤー個別エネルギーゲージ
  - 結果画面（勝利/敗北/スコア）

- [ ] **オーディオ**
  - BGM再生（30秒ループ）
  - シェイク検知SE
  - ボス攻撃SE

- [ ] **デバッグ機能**
  - キーボード入力でシェイク信号を人工生成
  - 画面上にシェイク回数・同期成功数を表示

### ⏸️ 未実装（フェーズ2以降）

- ボス第2形態・複数フェーズ
- ボスアニメーション（攻撃モーション）
- 練習フェーズの雑魚敵キャラ
- ESP32デバイスのLED制御（フィードバック）
- リプレイ機能
- ハイスコア保存
- SE/BGMのバリエーション
- エフェクト演出の拡充

---

## 8. ファイルツリー（実装イメージ）

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs (Singleton)
│   │   ├── GameConstants.cs
│   │   └── Enums.cs
│   │
│   ├── Input/
│   │   ├── InputManager.cs
│   │   ├── SerialPortHandler.cs
│   │   └── InputParser.cs
│   │
│   ├── Gameplay/
│   │   ├── PhaseController.cs
│   │   ├── BossController.cs
│   │   ├── PlayerManager.cs
│   │   ├── TimingSystem.cs
│   │   └── DamageCalculator.cs
│   │
│   ├── UI/
│   │   ├── UIManager.cs
│   │   ├── HPBarUI.cs
│   │   ├── PlayerStatusUI.cs
│   │   ├── TimingIndicatorUI.cs
│   │   ├── ResultScreenUI.cs
│   │   └── DebugConsole.cs
│   │
│   ├── Audio/
│   │   └── AudioManager.cs
│   │
│   └── Utilities/
│       ├── ObjectPool.cs
│       ├── DamagePopup.cs
│       └── Extensions.cs
│
├── Prefabs/
│   ├── Player/
│   │   └── PlayerStatusSlot.prefab
│   ├── Effects/
│   │   ├── DamagePopup.prefab
│   │   └── ShakeFeedback.prefab
│   └── UI/
│       └── ResultScreen.prefab
│
├── Scenes/
│   ├── TitleScene.unity
│   └── GameScene.unity
│
├── Audio/
│   ├── BGM/
│   │   └── boss_theme_120bpm_30s.mp3
│   ├── SFX/
│   │   ├── shake_detected.wav
│   │   ├── damage_hit.wav
│   │   ├── sync_success.wav
│   │   └── boss_attack.wav
│   └── Editor/ (テスト用)
│       └── 生成スクリプト.cs
│
├── Models/
│   └── Boss/
│       ├── Tottomusica.fbx (予定)
│       └── Tottomusica_Placeholder.png (MVP用)
│
├── Materials/
│   ├── BossMaterial.mat
│   └── EnergyBarMaterial.mat
│
└── Resources/
    ├── GameConfig.json
    └── TuningData.json (閾値・タイミング設定)
```

---

## 9. 実装優先度

### **Phase A: 基盤構築（Week 1）**
1. GameManager + 状態遷移
2. InputManager + SerialPort通信
3. GameScene基本構成
4. ボスモデル表示（プレースホルダ）

### **Phase B: ゲームロジック（Week 2）**
5. PhaseController
6. BossController + ダメージ計算
7. PlayerManager + マルチプレイヤー処理
8. TimingSystem

### **Phase C: UI/Audio（Week 3）**
9. UIManager + 各種UI要素
10. AudioManager + BGM/SE
11. Result画面

### **Phase D: デバッグ・最適化（Week 4）**
12. DebugConsole + キーボード入力
13. 調整・テスト
14. ビルド・納品

---

## 10. テスト戦略

### 単体テスト (Unit Tests)
- `TimingSystem.CheckTiming()` - タイミング判定ロジック
- `DamageCalculator.CalculateDamage()` - ダメージ計算
- `InputParser.ParseSerialData()` - CSV解析

### 統合テスト (Integration Tests)
- マルチプレイヤー同期テスト（1～10プレイヤー）
- SerialPort通信テスト（実機ESP32）
- ゲームフロー全体テスト

### 手動テスト (Manual Tests)
- キーボード入力での動作確認
- UI表示の正確性
- オーディオ再生タイミング
- 30秒計時の正確性

---

## 11. 次のステップ

1. **事前準備**
   - BGM素材（120 BPM, 30秒）の用意/購入
   - ボス3Dモデルの確認またはプレースホルダ画像作成
   - フォント・スプライト等の素材確認

2. **開発開始**
   - Sceneファイル作成 (`GameScene.unity`)
   - スクリプトフォルダ構成の構築
   - 最初のGameManagerスケッチ

3. **並行作業**
   - ESP32側のシリアル出力フォーマットの確認
   - Unityプロジェクト設定の最適化
   - 開発ビルド設定