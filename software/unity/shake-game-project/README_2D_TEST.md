# 🎮 Shake Game 2D Edition (試験版)

**プロジェクト名:** shake-game-project  
**バージョン:** 0.2.0（試験版）  
**Unity バージョン:** 2021.3 LTS 以上  
**ステータス:** ⏳ Phase 2 進行中

---

## ⚠️ 重要なお知らせ

このプロジェクトは **2D での試験版実装** です。

**本体版は 3D 版です:**
→ `../shake-game-3d/` を参照してください。

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
- [x] 2チーム対戦ゲームロジック
- [x] ゲージシステム
- [x] フェーズシステム（Charge/Resist）
- [x] 2D UI 表示
- [ ] 効果音・SE（実装予定）
- [ ] ビジュアル改善（実装予定）

---

## 📁 プロジェクト構成

```
shake-game-project/
├── Assets/
│   ├── Scenes/
│   │   └── Game.unity             # メインゲームシーン（2D）
│   │
│   ├── Scripts/
│   │   ├── SerialManager.cs        # Serial 通信
│   │   ├── GameManager.cs          # ゲーム進行管理
│   │   └── UIManager.cs            # UI 管理
│   │
│   └── UI/                         # UI 素材
│
├── ProjectSettings/
├── Packages/
└── README.md（このファイル）
```

---

## 🚀 セットアップと実行

### 初期設定

#### 1. Serial ポート設定
```csharp
// Assets/Scripts/SerialManager.cs
[SerializeField] private string portName = "COM3";  // 親機が接続されているポート
```

#### 2. ゲーム設定
```csharp
// Assets/Scripts/GameManager.cs
[SerializeField] private float gaugeThreshold = 100.0f;
[SerializeField] private int phaseDurationSeconds = 10;
```

#### 3. 実行
```
Play ボタンを押してゲーム開始
```

---

## 🎮 操作方法

### 実際のプレイ
```
1. ESP32 子機をフリフリ
2. 画面にゲージが増える
3. ゲージが 100% に達したら勝利
```

### キーボード (デバッグモード)
```
Space: シェイク検知をシミュレート
```

---

## ⚠️ 既知の問題と制限事項

### 現在の制限

| 項目 | 状況 | 説明 |
|------|------|------|
| **フレームレート** | ⚠️ 制限なし | 60fps 安定 |
| **音声同期** | ❌ 未実装 | 効果音・BGM 実装予定 |
| **3D グラフィックス** | ❌ 非対応 | 3D 版を参照（shake-game-3d） |
| **複数デバイス表示** | ✅ 対応 | 最大20プレイヤー |
| **LED フィードバック** | ✅ 対応 | ESP32 側で実装済み |

---

## 🔄 Phase 2 の進捗

```
2025-11月上旬:
  ✅ 基本プロジェクト構築
  ✅ Serial 通信実装
  ✅ ゲームロジック実装
  
2025-11月中旬～:
  ⏳ UI 改善
  ⏳ 効果音・SE 実装
  
【後続の Phase 3（3D版）に移行】
```

---

## 📚 関連ドキュメント

- [プロジェクト全体の開発履歴](../../docs/DEVELOPMENT.md)
- [セットアップガイド](../../docs/SETUP.md)
- **本体版（3D）:** `../shake-game-3d/README.md`

---

## 📝 開発者向けメモ

### このプロジェクトから 3D 版への移植のポイント

**1. Serial 通信** ✅
- `SerialManager.cs` はそのまま使用可能
- 変更不要

**2. ゲームロジック** 🔄
- `GameManager.cs` の基本ロジックは共用
- 3D 版では以下を追加：
  - `PlayerController.cs` - 3D モデル操作
  - `GaugeSystem.cs` - 3D ゲージ表現

**3. UI** 🔄
- 2D Canvas から 3D Canvas に変更
- 位置情報は World Space に変更

**4. 効果音** 🔄
- `AudioManager.cs` を新規作成
- AudioSource での再生管理

---

## ✅ テストチェックリスト

ゲームリリース前に確認してください：

- [ ] Serial 通信が正常に接続される
- [ ] 子機をフリフリするとゲージが増える
- [ ] フェーズが 10 秒ごとに切り替わる
- [ ] ゲージが 100% で勝利画面が表示される
- [ ] 複数の子機から同時にデータが受信される
- [ ] UI が正しく表示される

---

## 🆘 トラブルシューティング

**Serial ポートに接続できない:**
```
→ Arduino IDE のシリアルモニタを閉じてください
→ ポート番号が正しいか確認してください
```

**ゲージが増えない:**
```
→ SerialManager.cs でデータが受信されているか確認
→ Console に Parse Error が出ていないか確認
```

---

## 📞 ご質問・バグ報告

問題が発生した場合は、GitHub Issues で報告してください。

---

## 🚀 次のステップ

**Phase 3（3D 本体版）への移行:**

→ `software/unity/shake-game-3d/` で 3D ゲーム開発を開始

---

**作成日:** 2025-11月  
**更新日:** 2025-11-08  
**作成者:** GitHub Copilot  
**ステータス:** ⏳ 試験版進行中
