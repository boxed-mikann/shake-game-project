# 🎮 Shake Game 3D Edition

**プロジェクト名:** shake-game-3d  
**バージョン:** 0.1.0（開発中）  
**Unity バージョン:** 2021.3 LTS 以上  
**ステータス:** 🚀 Phase 3 開発開始（2025-11-08）

---

## 📋 プロジェクト概要

このプロジェクトは、Shake Game シリーズの **3D版本体実装** です。

**特徴:**
- ✨ 3D グラフィックス対応
- 🎵 リズムゲーム化への基盤構築
- 👥 複数プレイヤー対応（最大20人）
- 🔌 ESP32との Serial/ESP-NOW 通信対応
- 💡 LED フィードバック対応

---

## 🎯 開発目標

### Phase 3-1: 基本実装（11月中旬目標）
- [ ] Serial 通信の C# 実装
- [ ] 3D シーン構築（カメラ、ライティング）
- [ ] プレイヤーキャラクター配置（最大20人）
- [ ] ゲージシステムの 3D 表現

### Phase 3-2: ゲームロジック（11月下旬目標）
- [ ] 2チーム対戦ロジック実装
- [ ] ジェスチャー検知の 3D アニメーション連動
- [ ] 勝利・敗北演出（3D アニメーション）
- [ ] UI/UX 改善

### Phase 3-3: リズムゲーム化（12月上旬目標）
- [ ] AudioSource からの周波数解析
- [ ] ビート検出ロジック
- [ ] ビート時の VFX フィードバック
- [ ] リズムゲーム基本ロジック

---

## 📁 プロジェクト構成

```
shake-game-3d/
├── Assets/
│   ├── Scenes/
│   │   ├── MainGame.unity       # メインゲームシーン
│   │   ├── StartMenu.unity      # スタートメニュー
│   │   └── ResultScreen.unity   # 結果表示画面
│   │
│   ├── Scripts/
│   │   ├── Hardware/
│   │   │   ├── SerialManager.cs      # Serial 通信管理
│   │   │   └── DeviceController.cs   # デバイス制御
│   │   │
│   │   ├── Game/
│   │   │   ├── GameManager.cs        # ゲーム進行管理
│   │   │   ├── PlayerController.cs   # プレイヤー制御
│   │   │   └── GaugeSystem.cs        # ゲージシステム
│   │   │
│   │   ├── UI/
│   │   │   ├── ScoreBoard.cs         # スコアボード
│   │   │   └── UIManager.cs          # UI管理
│   │   │
│   │   └── Audio/
│   │       └── AudioManager.cs       # 音声・効果音管理
│   │
│   ├── Models/                  # 3D モデル
│   │   └── (3D FBX/prefab files)
│   │
│   ├── Prefabs/
│   │   ├── Player.prefab             # プレイヤー Prefab
│   │   ├── GaugeBar.prefab           # ゲージ Prefab
│   │   └── ParticleEffect.prefab     # パーティクル Prefab
│   │
│   ├── Materials/                # マテリアル
│   ├── Sounds/                   # 音声ファイル
│   └── Animations/               # アニメーション
│
├── ProjectSettings/              # Unity プロジェクト設定
├── Packages/                     # パッケージ管理
└── README.md                     # このファイル
```

---

## 🚀 セットアップと実行

### 前提条件
- Unity 2021.3 LTS 以上がインストール済み
- Arduino IDE でファームウェアが既に書き込まれている
- ESP32 親機が USB で PC に接続されている

### プロジェクトの開く方法

#### 方法1: Unity Hub から
```
1. Unity Hub を開く
2. 「プロジェクトを開く」 → このフォルダを指定
3. Unity 2021.3 以上を選択して開く
```

#### 方法2: Visual Studio から
```
1. shake-game-3d.sln を Visual Studio で開く
2. Unity Editor を起動
3. プロジェクトが自動で読み込まれる
```

### 初期設定

#### 1. Serial ポート設定
```
Assets/Scripts/Hardware/SerialManager.cs を開く

[SerializeField] private string portName = "COM3";  // 親機が接続されているポート
[SerializeField] private int baudRate = 115200;

// ポート番号を環境に合わせて変更
```

#### 2. ゲーム設定
```
Assets/Scripts/Game/GameManager.cs を開く

public int maxPlayers = 20;              // 最大プレイヤー数
public float gaugeThreshold = 100.0f;    // ゲージ満タン時の値
public int phaseDurationSeconds = 10;    // フェーズ継続時間
```

#### 3. 再生
```
1. Assets/Scenes/MainGame.unity を開く
2. Play ボタンを押す
3. シリアルモニタから Serial 接続が確認される
```

---

## 🎮 操作方法（テスト用）

### 実際のプレイ
```
1. ESP32 子機をフリフリ
2. LED が光る
3. Unity ゲーム画面にリアルタイムで反映される
4. ゲージが増える
5. 100% に達したら勝利
```

### キーボードテスト（デバッグモード）
```
キー             機能
─────────────────────────────
Space            子機1のシェイクをシミュレート
A                子機2のシェイクをシミュレート
B                子機3のシェイクをシミュレート
... (計20台対応)

上下矢印キー      テスト用ゲージ操作
```

---

## 📝 開発メモ

### Serial 通信フォーマット

ESP32 親機から受信するデータ:
```
childID,shakeCount,acceleration
0,1,12345
0,2,15678
1,1,14000
```

### 3D 表現について

- **プレイヤーキャラクター**: 立方体や球などシンプルなジオメトリで表現（予定）
- **ゲージ**: 3D バー型（世界座標で配置）または 2D UI で表現
- **パーティクル**: シェイク検知時に VFX 演出

### リズムゲーム化への基盤

AudioManager クラスで以下を実装予定:
```csharp
// 周波数解析（FFT）
float[] frequencyBands = new float[64];
audioSource.GetSpectrumData(frequencyBands, 0, FFTWindow.BlackmanHarris);

// ビート検出ロジック
bool isBeat = DetectBeat(frequencyBands);
```

---

## 🛠️ トラブルシューティング

### Serial ポート接続エラー
```
「SerialPortInUseException」が出た場合:
1. Arduino IDE のシリアルモニタを閉じる
2. Task Manager から javaw.exe を終了
3. Unity を再起動
```

### 3D シーンが真っ暗
```
ライティング設定を確認:
1. Directional Light の設定を確認
2. Skybox の設定を確認
3. Camera の Far Clip Plane を増やす
```

### ゲージが表示されない
```
Canvas の設定を確認:
1. Canvas Renderer が有効か確認
2. Image コンポーネントが正しく設定されているか確認
3. レイアウト グループの Preferred Size を確認
```

---

## 📚 関連ドキュメント

- [プロジェクト全体の開発履歴](../../docs/DEVELOPMENT.md)
- [セットアップガイド](../../docs/SETUP.md)
- [システム設計](../../docs/system_design.md)
- [ハードウェア配線図](../../hardware/wiring_diagram.md)

---

## 📞 ご質問・バグ報告

問題が発生した場合は、GitHub Issues で報告してください。

---

## 🚀 次のステップ

1. ✅ プロジェクト構造作成（2025-11-08 完了予定）
2. ⏳ Serial 通信実装（2025-11月中旬）
3. ⏳ 3D シーン構築（2025-11月下旬）
4. ⏳ ゲームロジック統合（2025-12月上旬）

---

**作成日:** 2025-11-08  
**作成者:** GitHub Copilot
