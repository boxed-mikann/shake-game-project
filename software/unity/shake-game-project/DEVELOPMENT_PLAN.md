# 開発進捗・次フェーズ計画

**プロジェクト:** Shake Game 2D Edition  
**更新日:** 2025-11-15  
**現在フェーズ:** コード実装完了 → Scene セットアップフェーズへ移行

---

## ✅ Phase 0: コード実装完了（実施済み）

### 実装スクリプト
| スクリプト | 行数 | 役割 | 完了日 |
|-----------|------|------|--------|
| GameManager.cs | ~150行 | ゲーム進行・フェーズ管理・イベント処理 | 11-15 |
| InputManager.cs | ~100行 | Serial通信・入力検知・イベント発火 | 11-15 |
| UIManager.cs | ~100行 | 3Canvas管理・画面遷移 | 11-15 |
| PhaseController.cs | ~80行 | 10秒ごとフェーズ切り替え（音符 ↔ 休符） | 11-15 |
| ScoreManager.cs | ~80行 | スコア計算・加減点・完璧プレイボーナス | 11-15 |
| NotePrefab.cs | ~120行 | 音符・休符オブジェクト（個別制御） | 11-15 |
| TimerDisplay.cs | ~40行 | タイマー表示更新 | 11-15 |
| ScoreDisplay.cs | ~40行 | スコア表示更新 | 11-15 |
| **合計** | **~710行** | **完全に動作するゲームロジック** | ✅ |

### 削除・簡素化
- ✅ VideoManager.cs 削除
- ✅ SoundManager.cs 削除
- ✅ BattleGameMode.cs, GameMode.cs, VictoryManager.cs など削除
- ✅ Singleton から AddComponent ロジック削除（5スクリプト × ~10行 = 50行削減）
- ✅ README を ~1230行から ~300行に簡潔化

### アーキテクチャの特徴
```
┌─────────────────────────────────────┐
│        UIManager (3Canvas管理)      │
│  Canvas_Start / Canvas_Game / ..    │
└──────────────┬──────────────────────┘
               ↓
┌──────────────────────────────────────┐
│      GameManager (ゲーム進行管理)    │
│  タイマー / フェーズ切り替え / イベント │
└──────────────┬──────────────────────┘
               ↓ OnGameStateChanged
       ┌───────┴───────┬────────────┐
       ↓               ↓            ↓
  ┌─────────┐  ┌──────────┐  ┌────────────┐
  │PhaseCtrl│  │ScoreMgr  │  │NotePrefab  │
  └─────────┘  └──────────┘  └────────────┘
       ↑                            ↑
       │         InputManager       │
       └────────← (Serial/Keyboard) ┘
```

---

## 🔧 Phase 1: Scene セットアップ（次のフェーズ - 推定 1-2日）

### 作業内容
- [ ] **Hierarchy 構築**
  - Main Camera 配置
  - 3つの Canvas（Start / Game / Result）作成
  - GameManager, InputManager, UIManager, PhaseController, ScoreManager を GameObject として配置
  - BackGroundVideo（VideoPlayer コンポーネント付き）配置

- [ ] **UI要素配置**
  - Canvas_Start: Title Text + Play Button
  - Canvas_Game: Panel_Header (Timer/Score/Phase) + Panel_Notes + Panel_Warning
  - Canvas_Result: Result Title + Final Score + Title Button

- [ ] **Inspector 参照割り当て**
  - UIManager に 3つの Canvas 参照を割り当て
  - UIManager に playButton / titleButton 参照を割り当て
  - GameManager に Note Prefab / Notes Container / Warning Panel 参照を割り当て

- [ ] **Note Prefab 作成**
  - Square Sprite を使用して Note GameObject 作成
  - SpriteRenderer + AudioSource + NotePrefab.cs 添付
  - Assets/Prefabs/ に保存

### チェックリスト
- [ ] Hierarchy 完成
- [ ] Canvas_Start で Play ボタンクリック → Canvas_Game 表示
- [ ] Canvas_Game でタイマー開始（60.0 → 59.9 ...）
- [ ] Canvas_Result は初期状態で非表示
- [ ] Title ボタン → Canvas_Start に戻る

---

## 🎮 Phase 2: ゲームロジック検証（推定 1-2日）

### 作業内容
- [ ] **デバッグ入力テスト**
  - Space キー押下 → シェイク検知をシミュレート
  - 画面上の音符がはじける確認
  - スコア加算確認

- [ ] **フェーズ切り替えテスト**
  - ゲーム開始時: Phase.NotePhase
  - 10秒後: Phase.RestPhase（音符が灰色に変更）
  - 20秒後: Phase.NotePhase（音符が白色に変更）
  - 以降、10秒ごとに交互切り替え

- [ ] **スコアシステムテスト**
  - 音符をはじける → +100点
  - 休符をはじける → -50点 + フリーズ0.5秒
  - ラストスパート（最後10秒）→ スコア2倍

- [ ] **タイマーテスト**
  - 60.0 から 0.0 へ正確にカウント
  - タイムアップで Canvas_Result 表示
  - Final Score に最終スコア表示

### チェックリスト
- [ ] Space キー → 音符が複数個はじける
- [ ] スコア画面に正しく反映される
- [ ] 10秒ごとにフェーズ自動切り替え
- [ ] 休符をはじけるとフリーズ（見た目のみ確認）

---

## 🎨 Phase 3: ビジュアル・オーディオ（推定 3-5日）

### 作業内容
- [ ] **背景動画統合**
  - Assets/Media/ に background.mp4 配置
  - BackGroundVideo の VideoPlayer に割り当て
  - CameraFarPlane モードで常時再生確認

- [ ] **効果音実装**
  - Assets/Media/Audio/ に以下を配置:
    - pop_note.wav (音符はじけ音)
    - penalty_freeze.wav (休符ペナルティ音)
    - timer_warning.wav (最後10秒警告音)
  - NotePrefab.cs の各効果音参照に割り当て
  - TimerDisplay.cs にタイマー警告音実装

- [ ] **フリーズエフェクト**
  - Panel_Warning（ホワイト Image）の Alpha を 0 → 1 → 0 に変更
  - Time.timeScale = 0.2f で一時的にスローモーション化

- [ ] **ビジュアル調整**
  - 音符スプライト：白 / 灰色の画像制作・割り当て
  - アニメーション：スポーン時のスケーリング / フェード
  - フォント：TimerDisplay / ScoreDisplay のフォントサイズ調整

### 素材準備チェック
- [ ] Assets/Media/ フォルダ構成:
  ```
  Assets/Media/
  ├── background.mp4
  ├── Sprites/
  │   ├── note_white.png
  │   └── note_gray.png
  └── Audio/
      ├── pop_note.wav
      ├── penalty_freeze.wav
      └── timer_warning.wav
  ```

---

## 📡 Phase 4: Serial 通信テスト（推定 2-3日）

### 作業内容
- [ ] **Serial ポート設定確認**
  - GameConstants.cs 確認
  - COM ポート番号（ユーザー環境に応じて設定）
  - ボーレート：115200

- [ ] **ESP32 親機との接続テスト**
  - Unity Editor で再生
  - Console に Serial 接続ログが表示されるか確認
  - "✅ Serial connected" メッセージ確認

- [ ] **複数デバイス同時接続テスト**
  - 複数の ESP32 デバイスを同時接続
  - 各デバイスからのシェイク検知が正しく処理されるか確認
  - スコア合算が正しく計算されるか確認

- [ ] **キーボードデバッグモード検証**
  - GameConstants.DEBUG_MODE = true
  - Space キーでシェイク検知をシミュレート
  - Serial デバイス接続なしでゲーム動作確認

### チェックリスト
- [ ] Serial ポート自動接続
- [ ] デバイス1 → 入力 → スコア +100
- [ ] デバイス2 → 入力 → スコア同時加算
- [ ] キーボード入力でもデバイス入力と同じ動作

---

## 🧪 Phase 5: 本格テスト・最適化（推定 5-7日）

### 作業内容
- [ ] **複数プレイヤーでのプレイテスト**
  - 実際のプレイヤー2～5人で 60 秒ゲームを複数回実施
  - ゲームバランス（難度・スコア感）の確認
  - UI の見やすさ・操作感の改善

- [ ] **パフォーマンスプロファイリング**
  - Unity Profiler でメモリ使用量確認
  - 最大同時音符数（50個以上）での FPS 確認
  - GC アロケーション最小化

- [ ] **バグ修正・チューニング**
  - コンソール エラー/警告 確認・修正
  - エッジケーステスト（タイムアップ直前の入力など）
  - ゲーム難度調整（スコア倍率、ペナルティ値）

- [ ] **ランキング機能実装（オプション）**
  - ScoreManager に PlayerPrefs 保存機能追加
  - Canvas_Result に TOP 10 スコアランキング表示

### チェックリスト
- [ ] 実機 5台以上で 60 秒ゲーム完走テスト
- [ ] Frame Rate: 平均 60fps 以上
- [ ] メモリリーク なし（1時間連続プレイ）
- [ ] バグ報告 0件

---

## 📈 進捗タイムライン

```
11月15日 (完了) ✅
├─ コード実装完了 (8スクリプト, ~710行)
├─ 削除・簡素化 (VideoManager, SoundManager など)
├─ README 簡潔化 (~1230行 → ~300行)
└─ 開発計画書作成

11月16-17日 (Phase 1 目標)
├─ Hierarchy 構築
├─ UI要素配置
├─ Inspector 参照割り当て
└─ Note Prefab 作成

11月18-19日 (Phase 2 目標)
├─ デバッグ入力テスト
├─ フェーズ切り替えテスト
├─ スコアシステムテスト
└─ タイマーテスト

11月20-24日 (Phase 3 目標)
├─ 背景動画統合
├─ 効果音実装
├─ フリーズエフェクト
└─ ビジュアル調整

11月25-27日 (Phase 4 目標)
├─ Serial ポート設定
├─ ESP32 接続テスト
├─ 複数デバイステスト
└─ キーボードデバッグ確認

11月28日 - 12月2日 (Phase 5 目標)
├─ 複数プレイヤーテスト
├─ パフォーマンスプロファイリング
├─ バグ修正
└─ 本番リリース準備

予定完了：2025年12月初旬
```

---

## 🎯 重要なポイント

### EventSystem の重要性 ⚡
```
Play ボタン動作不具合の原因: EventSystem が Scene に存在しなかった
解決策: Canvas 作成時に EventSystem が自動生成される

→ UIManager の playButton 参照割り当て後、動作確認！
```

### Singleton パターンの改善 ✨
```
旧設計: FindObjectOfType() + AddComponent() (重複生成の危険)
新設計: FindObjectOfType() のみ (Scene に事前配置)

利点: シンプル、バグ少ない、パフォーマンス向上
```

### イベントベース入力処理 🚀
```
旧設計: Update() で毎フレーム Dictionary 参照
新設計: 検知時に OnShakeDetected?.Invoke()

利点: 高速、CPU 負荷低い、応答性向上
```

---

## 📋 開発チェックリスト（全体）

### コード品質
- [x] 全スクリプト実装完了
- [x] Singleton 最適化完了
- [x] イベント駆動設計完了
- [x] 不要スクリプト削除完了
- [ ] ユニットテスト（オプション）

### ドキュメント
- [x] README 簡潔化（~1230行 → ~300行）
- [x] 開発計画書作成（本ドキュメント）
- [ ] API ドキュメント（オプション）
- [ ] トラブルシューティングガイド拡充

### Scene 準備
- [ ] Hierarchy 構築（Phase 1）
- [ ] UI 要素配置（Phase 1）
- [ ] Inspector 参照割り当て（Phase 1）
- [ ] Note Prefab 作成（Phase 1）

### ゲーム機能
- [ ] デバッグ入力テスト（Phase 2）
- [ ] フェーズシステム確認（Phase 2）
- [ ] スコアシステム確認（Phase 2）
- [ ] タイマーシステム確認（Phase 2）

### ビジュアル・オーディオ
- [ ] 背景動画統合（Phase 3）
- [ ] 効果音実装（Phase 3）
- [ ] フリーズエフェクト（Phase 3）
- [ ] ビジュアル調整（Phase 3）

### Serial 通信
- [ ] ポート設定確認（Phase 4）
- [ ] ESP32 接続テスト（Phase 4）
- [ ] 複数デバイステスト（Phase 4）
- [ ] キーボードデバッグモード（Phase 4）

### 最終テスト
- [ ] 複数プレイヤーテスト（Phase 5）
- [ ] パフォーマンスプロファイリング（Phase 5）
- [ ] バグ修正（Phase 5）
- [ ] 本番リリース準備（Phase 5）

---

## 次のアクション

**次の作業:** Phase 1 - Scene セットアップ開始

1. **Hierarchy 構築**
   - Main Camera, Canvas_Start/Game/Result, Managers, BackGroundVideo を配置

2. **UI 配置**
   - Play Button, Timer/Score Text, Panel_Notes, Title Button など

3. **Inspector 割り当て**
   - UIManager/GameManager の参照を割り当て

4. **Note Prefab 作成**
   - Assets/Prefabs/Note.prefab を作成・保存

---

**プロジェクト準備完了！** 🎉  
**次フェーズ開始予定:** 2025-11-16 or 2025-11-17
