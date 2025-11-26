# 🎮 Shake Game 2D Edition

**プロジェクト名:** shake-game-project  
**バージョン:** 0.3.0  
**Unity バージョン:** 2021.3 LTS 以上  
**ステータス:** ✅ コード実装完了 → Scene セットアップフェーズ

## 概要

Processing での実装を **Unity C#** に移植した 2D シェイクゲーム。複数プレイヤーが協力して 60 秒間で高スコアを目指します。

---

## ✅ 実装済みスクリプト（8個）

| スクリプト | 役割 | ステータス |
|-----------|------|-----------|
| **Core/GameManager.cs** | ゲーム進行・フェーズ管理 | ✅ 完了 |
| **Core/InputManager.cs** | Serial通信・入力イベント | ✅ 完了 |
| **UI/UIManager.cs** | 3Canvas表示切り替え | ✅ 完了 |
| **Game/PhaseController.cs** | 10秒ごと：音符 ↔ 休符 | ✅ 完了 |
| **Game/ScoreManager.cs** | スコア計算・加点・ペナルティ | ✅ 完了 |
| **Game/NotePrefab.cs** | 音符・休符オブジェクト | ✅ 完了 |
| **UI/TimerDisplay.cs** | タイマー表示更新 | ✅ 完了 |
| **UI/ScoreDisplay.cs** | スコア表示更新 | ✅ 完了 |

## 🗑️ 削除完了

✅ **VideoManager.cs** / **SoundManager.cs** (背景動画・効果音は直接制御)  
✅ **2チーム対戦関連** (BattleGameMode, GameMode, VictoryManager など)  
✅ **Singleton AddComponent ロジック** (5つの Manager から削除)

---

## 🎮 ゲームメカニクス

### ゲームフロー
```
[スタート画面] → Play ボタン → [プレイ画面] → 60秒経過 → [リザルト画面] → Title ボタン → [スタート画面]
```

### フェーズシステム
| フェーズ | 動作 | 効果 |
|---------|------|------|
| **音符フェーズ** (10秒) | 音符（♪）生成 → シェイクではじける | +100点/個 |
| **休符フェーズ** (10秒) | 音符が灰色に変わる → シェイク誤検知 | -50点/個 + フリーズ0.5秒 |

### スコアリング
- 音符をはじける: **+100 点/個**
- 休符をはじける: **-50 点/個** + フリーズペナルティ
- ラストスパート（最後10秒）: **×2 倍率**

---

## 🔧 Scene セットアップ（必須作業）

### Hierarchy 構造
```
Game.unity
├── Main Camera
├── Canvas_Start (スタート画面)
│   ├── Title Text
│   └── Play Button
├── Canvas_Game (プレイ画面)
│   ├── Panel_Header
│   │   ├── Timer Text
│   │   ├── Score Text
│   │   └── Phase Text
│   ├── Panel_Notes (音符表示エリア)
│   └── Panel_Warning (フリーズエフェクト)
├── Canvas_Result (リザルト画面)
│   ├── Result Title Text
│   ├── Final Score Text
│   └── Title Button (タイトルへ)
├── BackGroundVideo (GameObject + VideoPlayer)
└── Managers (GameObjects)
    ├── InputManager
    ├── GameManager
    ├── UIManager
    ├── PhaseController
    └── ScoreManager
```

### セットアップ手順

**1. Managers を Scene に配置**
```
GameObject > Create Empty → リネーム
- InputManager
- GameManager
- UIManager
- PhaseController
- ScoreManager

各 Manager に対応するスクリプトを AddComponent
```

**2. 3つの Canvas を作成**
```
GameObject > UI > Canvas → リネーム
- Canvas_Start
- Canvas_Game
- Canvas_Result

各 Canvas に以下を配置：
```

**Canvas_Start:**
- Title Text ("SHAKE GAME")
- Play Button → OnClick → UIManager.ShowGameScreen()

**Canvas_Game:**
- Panel_Header (Image/Panel)
  - Timer Text (表示内容: "60.0")
  - Score Text (表示内容: "0")
  - Phase Text (表示内容: "♪ NOTES")
- Panel_Notes (RectTransform: 音符生成エリア)
- Panel_Warning (Image: 半透明白, 初期 Alpha=0)

**Canvas_Result:**
- Result Title Text ("RESULT")
- Final Score Text (表示内容: "0")
- Title Button → OnClick → UIManager.ShowStartScreen()

**3. Inspector で参照を割り当て**

**UIManager:**
- Canvas_Start: Canvas_Start GameObject
- Canvas_Game: Canvas_Game GameObject
- Canvas_Result: Canvas_Result GameObject
- Play Button: Canvas_Start > Play Button
- Title Button: Canvas_Result > Title Button

**GameManager:**
- Note Prefab: Assets/Prefabs/Note.prefab
- Notes Container: Canvas_Game > Panel_Notes
- Warning Panel: Canvas_Game > Panel_Warning

**4. Note Prefab を作成**

GameObject > Sprite > Square → 表示→ リネーム "Note"
- Add Component: SpriteRenderer
- Add Component: AudioSource
- Add Component: NotePrefab.cs
- Assets/Prefabs/ フォルダにドラッグして保存

**NotePrefab.cs Inspector:**
- Note Sprite: (白い音符画像)
- Rest Sprite: (灰色の休符画像)
- Burst Sound Clip: (pop音)
- Penalty Sound Clip: (freeze音)

**5. BackGroundVideo を配置**

GameObject > Create Empty → "BackGroundVideo"
- Add Component: VideoPlayer
- Render Mode: Camera Far Plane
- Target Camera: Main Camera
- Video Clip: Assets/Media/background.mp4
- Loop: ON / Play On Awake: ON

---

## ⚡ 動作確認手順

**1. Unity Editor で再生**
```
Play → Canvas_Start が表示される
Play ボタン クリック → Canvas_Game が表示される
タイマーが 60.0 から 59.9... へ カウント開始
```

**2. デバッグ入力（キーボード）**
```
Space キー: シェイク検知をシミュレート
→ 画面上の音符がはじける
→ スコア加算 + 破裂エフェクト
```

**3. 60秒経過**
```
タイマーが 0.0 に達する
Canvas_Game が非表示
Canvas_Result が表示
Final Score に最終スコアが表示
```

**4. Title ボタン**
```
Title ボタン クリック
→ Canvas_Result が非表示
→ Canvas_Start が表示（ゲーム終了）
```

---

## 📋 次の開発フェーズ

### Phase 1: Scene セットアップ
- [ ] 上記 Hierarchy を作成
- [ ] 各 Canvas に UI要素を配置
- [ ] Inspector で Manager スクリプト参照を割り当て
- [ ] Note Prefab を作成・保存

### Phase 2: ゲームロジック検証
- [ ] デバッグ入力でゲーム進行確認
- [ ] 音符が正しく湧き出すか確認
- [ ] スコア計算が正しく動作するか確認
- [ ] フェーズ切り替えが 10 秒ごと行われるか確認

### Phase 3: ビジュアル・オーディオ
- [ ] 背景動画統合
- [ ] 効果音 SE (pop, freeze, timer warning)
- [ ] フリーズエフェクト（ホワイトフラッシュ）
- [ ] ビジュアル調整（色、アニメーション）

### Phase 4: Serial 通信テスト
- [ ] Serial ポート接続確認
- [ ] 複数デバイス同時接続テスト
- [ ] 実機（ESP32）からの入力処理確認

### Phase 5: 本格テスト
- [ ] 複数プレイヤーでプレイテスト
- [ ] パフォーマンスプロファイリング
- [ ] バグ修正・チューニング

---

## 🆘 トラブルシューティング

**Play ボタンをクリックしてもゲームが開始しない**
```
→ UIManager の playButton 参照が割り当てられているか確認
→ EventSystem が Scene に存在するか確認（標準 Canvas 作成時に自動生成）
→ Button の OnClick イベントにリスナーが登録されているか確認
```

**音符が湧き出さない**
```
→ GameManager の Note Prefab が割り当てられているか確認
→ Canvas_Game が Active か確認
→ Panel_Notes（音符生成エリア）が割り当てられているか確認
```

**スコアが表示されない**
```
→ ScoreManager が Scene にあるか確認
→ ScoreDisplay.cs が Canvas_Game の Score Text に割り当てられているか確認
```

**フリーズが動作しない**
```
→ Panel_Warning が Canvas_Game に存在するか確認
→ GameManager の Warning Panel 参照が割り当てられているか確認
```

---

## 📞 開発者向けメモ

### コード品質の向上点（v0.3.0）
- ✅ Singleton に不要な AddComponent ロジック削除（50行以上削減）
- ✅ Manager オブジェクトを Scene に事前配置して FindObjectOfType 使用
- ✅ イベントベースの入力処理（高速・シンプル）
- ✅ 重複スクリプト削除（VideoManager, SoundManager など）

### 次バージョンでの改善予定
- [ ] ランキング機能（PlayerPrefs または JSON 保存）
- [ ] 複数難易度設定
- [ ] オプション画面（音量、ゲーム時間調整）
- [ ] パーティクルエフェクトの充実

---

**最終更新:** 2025-11-15  
**ステータス:** ✅ コード実装完了 → Scene セットアップ開始  
**次の作業:** Hierarchy 構築 + Inspector 参照割り当て
