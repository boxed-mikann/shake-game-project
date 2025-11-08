# 🚀 セットアップガイド

**最終更新:** 2025-11-08  
**対応環境:** Arduino IDE 2.0+, Processing 4.0+ or Unity 2021.3+

---

## 📋 目次

1. [ファームウェア開発環境](#ファームウェア開発環境)
2. [ハードウェア接続](#ハードウェア接続)
3. [ファームウェアの書き込み](#ファームウェアの書き込み)
4. [ソフトウェア環境](#ソフトウェア環境)
5. [動作確認](#動作確認)

---

## ファームウェア開発環境

### Arduino IDE のインストール

1. [Arduino公式サイト](https://www.arduino.cc/en/software) から **Arduino IDE 2.0以上** をダウンロード
2. インストーラーを実行

### ESP32 ボードパッケージのインストール

1. Arduino IDE を開く
2. **File → Preferences** を選択
3. "Additional Boards Manager URLs" に以下を追加:
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
4. **Tools → Board → Boards Manager** を開く
5. "esp32" で検索して "esp32 by Espressif Systems" をインストール

### 必要なライブラリ

Arduino IDE の **Manage Libraries** で以下をインストール：
- Wire (I2C 通信用、通常は組み込み)
- esp_now (ESP-NOW 通信用)

---

## ハードウェア接続

### 子機の配線（ESP32 + MPU-6050 + LED）

```
ESP32 Dev Board               外部デバイス
───────────────────────────────────────
GPIO 23 (SDA)  ──→ MPU-6050 (GY-521) SDA
GPIO 22 (SCL)  ──→ MPU-6050 (GY-521) SCL
GPIO 13        ──→ LED (+ 220Ω抵抗)
GND            ──→ MPU-6050 GND
GND            ──→ LED カソード
3.3V           ──→ MPU-6050 VCC
3.3V           ──→ I2C プルアップ抵抗（SDA, SCL用）
```

**プルアップ抵抗について:**
- I2C 通信には **SDA、SCL 各々に 10kΩ の抵抗** が必要
- 抵抗の一端を 3.3V、もう一端を SDA/SCL に接続

詳細は **[hardware/wiring_diagram.md](../hardware/wiring_diagram.md)** を参照

---

## ファームウェアの書き込み

### 事前確認

```bash
# 親機の MAC アドレスを確認
親機を書き込み後、シリアルモニタで表示される MAC アドレスをメモ
例: AA:BB:CC:DD:EE:FF
```

### 親機（ESP32）の書き込み

1. 親機の ESP32 を PC に接続
2. Arduino IDE で `firmware/parent_device/parent_device.ino` を開く
3. **Tools → Board** で "ESP32 Dev Module" を選択
4. **Tools → Port** で親機が接続されているポートを選択
5. **Sketch → Upload** でアップロード
6. シリアルモニタ（115200 baud）で以下を確認：
   ```
   Parent MAC Address: AA:BB:CC:DD:EE:FF
   ESP-NOW Parent Ready!
   ```

### 子機（ESP32 + MPU-6050）の書き込み

1. 子機の ESP32 を PC に接続
2. Arduino IDE で `firmware/child_device/child_device.ino` を開く
3. **コードを編集：**
   ```cpp
   #define CHILD_ID 0  // 子機の番号（1台目: 0、2台目: 1、...、20台目: 19）
   uint8_t parentMAC[] = {0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF};  // 親機のMAC
   ```
   親機の MAC アドレスを上記で確認した値に置き換え
4. **Tools → Port** で子機が接続されているポートを選択
5. **Sketch → Upload** でアップロード
6. シリアルモニタ（115200 baud）で以下を確認：
   ```
   Child #0 MAC Address: XX:XX:XX:XX:XX:XX
   === Jerk-based Shake Detection ===
   Child #0 Ready!
   ```
7. **複数の子機を接続する場合：**
   - `CHILD_ID` を変更（1, 2, 3, ...）
   - 手順3～6を繰り返す

**トラブル:** I2C エラーが出た場合は [docs/troubleshooting.md](./troubleshooting.md#i2c通信関連--new) を参照

---

## ソフトウェア環境

### ✅ 推奨: Unity（3D表示対応）

#### 環境要件
- **Unity Hub** 最新版
- **Unity 2021.3 LTS** 以上

#### インストール

```bash
1. Unity Hub をダウンロード・インストール
   https://unity.com/ja/download

2. Unity 2021.3 LTS をインストール
   Unity Hub → Install Editor → 2021.3 LTS

3. プロジェクトを開く
   software/unity/shake-game-3d/  (3D版)
   または
   software/unity/shake-game-project/  (2D試験版)
```

#### Serial 通信ライブラリ

Unity では C# で Serial 通信を実装：

```csharp
using System.IO.Ports;

SerialPort serialPort = new SerialPort("COM3", 115200);
serialPort.Open();
string data = serialPort.ReadLine();
```

---

### ⏳ 参考: Processing（2D表示版）

#### インストール

1. [Processing公式サイト](https://processing.org/download) から **Processing 4.0以上** をダウンロード
2. インストーラーを実行

#### ライブラリ

Processing IDE の **Sketch → Import Library → Manage Libraries** で以下をインストール：
- Serial（シリアル通信）
- Video（動画再生）
- Minim（音声生成）

#### 実行方法

```bash
1. software/processing/shake_game_display/shake_game_display.pde を Processing で開く

2. コードでポート設定を確認：
   String portName = Serial.list()[0];

3. ▶ ボタンで実行
```

**注意:** Processing 版は 2D表示のみで、リズムゲーム化等の拡張は限定的です。

---

## 動作確認

### ステップ1: 親機の起動

```
1. 親機（ESP32）を PC に接続
2. Arduino IDE のシリアルモニタで確認
   出力例:
   > Parent MAC Address: AA:BB:CC:DD:EE:FF
   > ESP-NOW Parent Ready!
   > Waiting for child devices...
```

### ステップ2: 子機の起動

```
1. 子機（ESP32 + MPU-6050）を PC に接続
2. Arduino IDE のシリアルモニタで確認
   出力例:
   > Child #0 MAC Address: 11:22:33:44:55:66
   > Child #0 Ready!

3. 子機をフリフリ
   出力例:
   > Accel: 12345 | Jerk: 8000 | isShaking: 0
   > >>> SHAKE! ID: 0 | Count: 1 | Jerk: 8000
   > (同時に LED が点灯)
```

### ステップ3: 親機でのデータ受信

```
1. 親機の Arduino IDE のシリアルモニタを確認
2. 子機をフリフリすると以下が表示される:
   > Received data - ID: 0 | Count: 1 | Accel: 12345
```

### ステップ4: ソフトウェアでの表示

**Unity を使う場合：**
```
1. Unity プロジェクトを開く
2. ポート設定を確認（Assets/Scripts/SerialManager.cs）
3. Play ボタンで実行
4. ゲーム画面にシェイク検知が反映される
```

**Processing を使う場合（参考）:**
```
1. Processing スケッチを実行
2. シリアルでグリッド表示が更新される
```

---

## ✅ すべて完了したら

- ✅ LED が フリフリ時に点灯する
- ✅ Unity（または Processing）で シェイク検知が表示される
- ✅ 複数子機での同時動作を確認

## 🆘 トラブルシューティング

よくある問題は [docs/troubleshooting.md](./troubleshooting.md) を参照してください。

---

## 📚 次のステップ

- **ゲーム開発:** `software/unity/shake-game-3d/` で 3D ゲーム開発開始
- **ハードウェア調整:** `docs/acceralation_tuning_guide.md` で加速度値のチューニング
- **詳細な設計:** `docs/system_design.md` でシステム全体の設計を確認
