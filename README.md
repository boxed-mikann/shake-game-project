# 🎮 Shake Game Project

複数人でできるシェイクゲームシステム - Arduino ESP32を使用した無線通信プロジェクト

## 📋 プロジェクト概要

このプロジェクトは、複数のプレイヤーが同時にシェイク（振る）動作でゲームに参加できるシステムです。
ESP32とMPU-6050加速度センサーを使用した低遅延の無線通信（ESP-NOW）で実現されています。

## 🎯 システム構成

### ハードウェア

| 役割 | 台数 | ボード | センサー | 通信 |
|------|------|--------|---------|------|
| 親機（司令塔） | 1台 | ESP32 Dev Board | なし | ESP-NOW |
| 子機（プレイヤー） | 20台 | ESP32 Dev Board | MPU-6050 | ESP-NOW |
| 表示 | 1台 | PC | なし | USB Serial |

### ソフトウェア

- **ファームウェア**: Arduino IDE + C++
- **表示**: Processing
- **通信**: ESP-NOW（無線）+ USB Serial

## 🚀 クイックスタート

### 1. ハードウェア準備

[hardware/wiring_diagram.md](./hardware/wiring_diagram.md) を参照して配線してください。

### 2. ファームウェア書き込み

```bash
# 親機
firmware/parent_device/parent_device.ino をArduino IDEで開き、ESP32に書き込み

# 子機
firmware/child_device/child_device.ino をArduino IDEで開き、各ESP32に書き込み
```

### 3. Processing で表示

```bash
software/processing/shake_game_display/shake_game_display.pde をProcessingで実行
```

## 📂 ディレクトリ構成

```
firmware/        # Arduino ファームウェア
├── parent_device/    # 親機プログラム
└── child_device/     # 子機プログラム

software/        # PC側ソフトウェア
├── processing/       # Processing 表示プログラム

hardware/        # ハードウェア関連
├── wiring_diagram.md # 配線図
└── BOM.md           # 部品リスト

docs/            # ドキュメント
├── system_design.md  # システム設計
├── setup_guide.md    # セットアップガイド
├── game_design.md    # ゲーム設計（未定）
└── troubleshooting.md # トラブルシューティング
```

## 🔧 開発環境

- **Arduino IDE**: 2.0以上
- **ボードパッケージ**: esp32 by Espressif Systems
- **Processing**: 4.0以上
- **Git**: バージョン管理用

## 📚 ドキュメント

- [セットアップガイド](./docs/setup_guide.md)
- [システム設計](./docs/system_design.md)
- [ハードウェア配線](./hardware/wiring_diagram.md)
- [トラブルシューティング](./docs/troubleshooting.md)

## 🎮 現在の進捗

- [x] 子機のフリフリ検知（MPU-6050）
- [x] ESP-NOW通信の実装
- [x] 親機のデータ受信
- [ ] Processing での表示
- [ ] ゲームロジックの実装
- [ ] 複数子機での同時動作確認
- [ ] ハードウェア統合（LED制御など）
- [ ] 本番環境での動作確認

## 📝 今後の予定

- ゲーム設計の決定
- Processing による リアルタイム表示
- LED制御ロジックの実装
- UI/UX の改善

## 🤝 貢献

バグ報告や機能提案は Issues で受け付けています。


## 📞 お問い合わせ

質問や提案がある場合は、Issues を作成してください。