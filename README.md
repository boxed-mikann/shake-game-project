# 🎮 Shake Game Project

複数人でできるシェイクゲームシステム - Arduino ESP32を使用した無線通信プロジェクト(注：AI生成ドキュメントを多用しているので、ウソや未検証事項が含まれています。)

## 📋 プロジェクト概要

このプロジェクトは、複数のプレイヤーが同時にシェイク（振る）動作でゲームに参加できるシステムです。
ESP32とMPU-6050加速度センサーを使用した低遅延の無線通信（ESP-NOW）で実現されています。

## 🎯 システム構成

### ハードウェア

| 役割 | 台数 | ボード | センサー | 通信 |
|------|------|--------|---------|------|
| 親機（司令塔） | 1台 | ESP32 Dev Board | なし | ESP-NOW |
| 子機（プレイヤー） | 10台 | ESP32 Dev Board | MPU-6050 | ESP-NOW |
| 表示 | 1台 | PC | なし | USB Serial |

### ソフトウェア

- **ファームウェア**: Arduino IDE + C++
- **表示**: **Unity (2021.3+)** - 2D試験版・3D本体版対応 ✨
- ~~Processing~~ (参考実装、2D版のみ)
- **通信**: ESP-NOW（無線）+ USB Serial

## 🚀 クイックスタート

### 1. ハードウェア準備

[hardware/wiring_diagram.md](./hardware/wiring_diagram.md) を参照して配線してください。

### 2. ファームウェア書き込み

```bash
# 親機
firmware/parent_device/parent_device.ino をArduino IDEで開き、ESP32に書き込み

# 子機（複数台対応）
firmware/child_device/child_device.ino をArduino IDEで開き、各ESP32に書き込み
CHILD_ID と parentMAC を各子機用に変更
```

### 3. Unity で表示

**推奨: 3D版（Phase 3）**
```bash
software/unity/shake-game-3d/shake-game-3d.sln をVisual Studioで開く
または Unity Hub から開く
```

**試験版: 2D版（Phase 2）**
```bash
software/unity/shake-game-project/shake-game-project.sln をVisual Studioで開く
```

**参考: Processing での表示（Phase 1）**
```bash
software/processing/shake_game_display/shake_game_display.pde をProcessingで実行
```

詳細は **[docs/SETUP.md](./docs/SETUP.md)** を参照

## 📂 ディレクトリ構成

```
firmware/        # Arduino ファームウェア
├── parent_device/         # 親機プログラム
└── child_device/          # 子機プログラム（LED対応）

software/        # PC側ソフトウェア
├── processing/            # Processing 表示プログラム（参考実装）
│   ├── shake_game_display/
│   └── shake_game_battle/
└── unity/                 # Unity ゲーム
    ├── shake-game-project/   # 2D試験版（Phase 2）
    └── shake-game-3d/        # 3D本体版（Phase 3）※新規予定

hardware/        # ハードウェア関連
├── wiring_diagram.md      # 配線図（LED対応版）
└── BOM.md                 # 部品リスト

docs/            # ドキュメント（統合版）
├── DEVELOPMENT.md         # 開発履歴・進捗（統合）
├── SETUP.md               # セットアップガイド
├── system_design.md       # システム設計
├── shake_detection_algorithm.md
├── acceralation_tuning_guide.md
├── troubleshooting.md     # トラブルシューティング
└── game_design.md         # ゲーム設計（検討中）
```

## 🔧 開発環境

- **Arduino IDE**: 2.0以上
- **ボードパッケージ**: esp32 by Espressif Systems
- ~~**Processing**: 4.0以上~~
- **Git**: バージョン管理用

## 📚 ドキュメント

- [開発履歴](./docs/DEVELOPMENT.md) - プロジェクトの進捗と技術決定
- [セットアップガイド](./docs/SETUP.md) - 環境構築・初期設定
- [システム設計](./docs/system_design.md) - システム全体の設計
- [ハードウェア配線](./hardware/wiring_diagram.md) - 配線図（LED対応版）
- [加速度チューニング](./docs/acceralation_tuning_guide.md) - センサー値の調整
- [ジャーク検知アルゴリズム](./docs/shake_detection_algorithm.md) - 検知手法の詳細
- [トラブルシューティング](./docs/troubleshooting.md) - よくある問題と解決方法
  
注：ドキュメントはAI生成が多く含まれています。未検証事項が存在する可能性があります。詳細は各ファイルを参照してください。

## 🎮 現在の進捗

### Phase 1: Processing（2D表示） ✅ 完了
- [x] 子機のフリフリ検知（MPU-6050）
- [x] ESP-NOW通信の実装
- [x] 親機のデータ受信
- [x] Processing での表示
- [x] 親機のデータ送信(LED,ふりふりタイミング指示情報)
- [x] 効果音・動画再生機能

### Phase 2: Unity 2D試験版 ⏳ 進行中
- [x] 基本プロジェクト構築
- [x] Serial 通信実装
- [x] 2チーム対戦ゲームロジック
- [ ] UI 改善
- [ ] 効果音・SE 実装

### Phase 3: Unity 3D本体版（LED対応） 🚀 開始予定
- [x] **子機にLED制御機能を追加（フリフリ時に点灯）** ✨ NEW
- [ ] 3D プロジェクト構築
- [ ] 3D ビジュアル実装
- [ ] リズムゲーム化の基盤構築
- [ ] 複数プレイヤー対応の UI

### その他
- [ ] フリフリ検知閾値改善→検討継続
- [ ] 複数子機での同時動作確認
- [ ] 本番環境での動作確認

## 📝 今後の予定

- ✅ LED制御機能実装（2025-11-08 完了）
- ✅ ドキュメント整理（2025-11-08 完了）
- 🚀 Unity 3D プロジェクト開始（2025-11-08～）
- ⏳ 3D ビジュアル実装（予定: 11月中旬）
- ⏳ リズムゲーム化の検討（予定: 11月下旬～）
- ⏳ 複数デバイス統合テスト（予定: 12月）

## 🤝 貢献

バグ報告や機能提案は Issues で受け付けています。


## 📞 お問い合わせ

質問や提案がある場合は、Issues を作成してください。