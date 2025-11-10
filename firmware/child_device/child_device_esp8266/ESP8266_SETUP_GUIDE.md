# ESP8266 子機セットアップガイド

**対象:** ESP8266 Development Board  
**目的:** 本番用子機の試験実装  
**ファイル:** `child_device_esp8266.ino`  
**LED:** ボード備え付けの赤色LED（D0ピン / GPIO16）でテスト

---

## ⚠️ 重要な注意事項

### ESP8266 と ESP32 の主な違い

| 項目 | ESP32 | ESP8266 |
|------|-------|---------|
| **ボードマネージャー** | esp32 by Espressif | esp8266 by ESP8266 Community |
| **I2C ピン** | GPIO 23 (SDA), GPIO 22 (SCL) | D2 (GPIO4) SDA, D1 (GPIO5) SCL |
| **LED 動作** | アクティブハイ | **アクティブロー** ⚠️ |
| **LED ピン** | GPIO 13 | D0 (GPIO16) 内蔵 |
| **起動時間** | 短め | **2秒程度** ⚠️ |
| **消費電力** | 標準 | **低消費電力** ✅ |

---

## 🔧 Arduino IDE でのセットアップ手順

### ステップ1: ESP8266 ボードパッケージをインストール

1. **Arduino IDE を開く**

2. **File → Preferences** を選択

3. **"Additional Boards Manager URLs"** に以下を追加：
   ```
   http://arduino.esp8266.com/stable/package_esp8266_index.json
   ```

4. **OK** をクリック

5. **Tools → Board → Boards Manager** を開く

6. **"esp8266"** で検索

7. **"esp8266 by ESP8266 Community"** をインストール

   **バージョン:** 3.0.0 以上推奨

---

### ステップ2: ボード設定

1. **Tools → Board → ESP8266 Boards** を展開

2. **"Generic ESP8266 Module"** を選択

   または **"NodeMCU 1.0 (ESP-12E Module)"** 等、お持ちのボードに応じて選択

3. **Tools** メニューで以下を設定：

   ```
   Board: Generic ESP8266 Module
   Flash Mode: DIO
   Flash Size: 4M (1M SPIFFS)
   CPU Frequency: 80 MHz
   Upload Speed: 115200
   Port: COM3 (または接続されているポート)
   ```

---

### ステップ3: 必要なライブラリをインストール

1. **Sketch → Include Library → Manage Libraries** を開く

2. 以下を検索してインストール：

   - **"ESP8266WiFi"** - 通常は自動で含まれる
   - **"esp_now"** - または **"espnow"** で検索（ESP8266用）
   
   ※ ESP8266 Community ボードパッケージをインストールすると、これらは自動で含まれます

---

### ステップ4: コードの編集と設定

1. **`child_device_esp8266.ino` を開く**

2. **以下の設定を環境に合わせて変更：**

   ```cpp
   // ファイルの先頭付近
   #define CHILD_ID 0  // 子機の番号（1台目: 0, 2台目: 1, ...）
   
   // 親機のMACアドレスを設定
   uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};
   // ↑ 親機の実際のMAC アドレスに置き換えてください
   ```

3. **I2C ピン設定の確認：**

   ```cpp
   Wire.begin(D2, D1);  // SDA=D2, SCL=D1
   ```
   
   ※ MPU-6050 の配線が以下のように接続されていることを確認：
   - MPU-6050 SDA → ESP8266 D2 (GPIO4)
   - MPU-6050 SCL → ESP8266 D1 (GPIO5)
   - MPU-6050 GND → ESP8266 GND
   - MPU-6050 VCC → ESP8266 3.3V

---

## 📤 Arduino IDE でのアップロード手順

### 手順1: ボードをUSB接続

1. ESP8266 ボードを USB ケーブルで PC に接続
2. Windows で **"CH340 USB UART" などが認識されることを確認**

### 手順2: ポート選択

1. **Tools → Port** を開く
2. **"COM3"** など、ESP8266が接続されているポートを選択

### 手順3: コンパイルと書き込み

1. **Sketch → Upload** をクリック
   
   または **Ctrl + U** を押す

2. **"Compiling..."** と表示され、コンパイルが開始

3. **"Uploading..."** と表示され、書き込み開始

4. **シリアルモニタで確認：**
   ```
   === ESP8266 Shake Detection ===
   Child #0 MAC Address: XX:XX:XX:XX:XX:XX
   === Jerk-based Shake Detection (ESP8266) ===
   JERK_THRESHOLD: 20000
   DEBOUNCE_TIME: 0
   LED PIN: D0 (GPIO16) - Built-in Red LED
   ESP-NOW Child #0 Ready!
   Waiting for shake...
   ```

---

## 🧪 動作確認手順

### 準備

1. ✅ 親機（ESP32）が起動している
2. ✅ 親機がシリアルで親機のポートを確認
3. ✅ 子機（ESP8266）がボード備え付けLED点灯状態で待機中

### テスト手順

#### テスト1: LED 点灯確認

```
【期待動作】
1. ESP8266 の赤色LED が最初は消灯している
2. デバイスを "フリフリ" する
3. LED が点灯する
4. フリフリを止めると LED が消灯する
```

**コンソール出力例：**
```
>>> SHAKE! ID: 0 | Count: 1 | Jerk: 25000 | LED: ON
Reset shake state | LED: OFF
```

#### テスト2: シリアルデータ確認

```
【期待動作】
1. シリアルモニタで ">>> SHAKE!" というメッセージが表示される
2. カウントが増える
```

#### テスト3: 親機でのデータ受信確認

```
【親機のシリアルモニタで確認】
Received from Child #0 | Count: 1 | Accel: XXXXX
Received from Child #0 | Count: 2 | Accel: YYYYY
```

---

## 🆘 トラブルシューティング

### 問題1: ボードが認識されない

**症状:** "Board not found" エラー

**解決方法:**
```
1. USB ドライバをインストール
   → https://github.com/nodemcu/nodemcu-devkit/wiki/Getting-Started-on-Windows
   
2. 別の USB ケーブルを試す

3. PC を再起動

4. Device Manager で "CH340 USB UART" が表示されているか確認
```

### 問題2: アップロードが失敗する

**症状:** 
```
error: espcomm_upload_ram failed
esptool.py v1.3 - Uploading 0 bytes for 0x00000000
```

**解決方法:**
```
1. ボード設定を確認：
   Tools → Upload Speed: 115200

2. GPIO0 ボタンを押しながら USB 接続
   （一部のボードでは必須）

3. Flash Mode を "DIO" から "QIO" に変更してみる
```

### 問題3: I2C エラー（MPU-6050 が認識されない）

**症状:**
```
E (11528) i2c.master: I2C transaction failed
```

**解決方法:**
```
1. 配線を確認：
   - SDA → D2 (GPIO4)
   - SCL → D1 (GPIO5)
   - GND → GND
   - VCC → 3.3V

2. プルアップ抵抗（10kΩ）を SDA、SCL に追加

3. I2C クロック速度を低下：
   Wire.setClock(100000);  // 100kHz に変更
```

### 問題4: LED が点灯しない

**症状:** フリフリしても赤色LED が光らない

**解決方法:**
```
1. ピン設定を確認：
   const int LED_PIN = D0;  // GPIO16

2. ESP8266 はアクティブロー：
   digitalWrite(LED_PIN, LOW);   // 点灯
   digitalWrite(LED_PIN, HIGH);  // 消灯

3. LED が正常に接続されているか確認
   （ボード備え付けの場合は通常 OK）
```

### 問題5: シェイク検知されない

**症状:** デバイスを振っても ">>> SHAKE!" が表示されない

**解決方法:**
```
1. 加速度値を確認：
   コード内の Serial.println(currentAccel); を有効化
   → デバッグ用コメントを外す

2. JERK_THRESHOLD を下げてみる：
   const float JERK_THRESHOLD = 15000.0;  // デフォルト: 20000

3. MPU-6050 が正しく初期化されているか確認：
   シリアルで "Initialized with Accel:" が表示されているか
```

---

## 📝 デバッグのポイント

### シリアルモニタの表示内容

**正常な起動時：**
```
=== ESP8266 Shake Detection ===
Child #0 MAC Address: 12:34:56:78:9A:BC
=== Jerk-based Shake Detection (ESP8266) ===
JERK_THRESHOLD: 20000
DEBOUNCE_TIME: 0
LED PIN: D0 (GPIO16) - Built-in Red LED
Initialized with Accel: 12345
ESP-NOW Child #0 Ready!
Waiting for shake...
```

**フリフリ時：**
```
>>> SHAKE! ID: 0 | Count: 1 | Jerk: 25000 | LED: ON
Reset shake state | LED: OFF
>>> SHAKE! ID: 0 | Count: 2 | Jerk: 22000 | LED: ON
Reset shake state | LED: OFF
```

### 加速度値が異常な場合

**デバッグコード挿入：**

```cpp
// loop() 内の該当箇所を有効化
Serial.print("Accel: ");
Serial.print(currentAccel, 0);
Serial.print(" | Jerk: ");
Serial.println(jerk, 0);
```

出力を確認：
- 静止時: 8000～12000 程度
- フリフリ時: 15000～40000 程度

---

## 📋 複数の ESP8266 子機を接続する場合

### 手順

1. **2台目の ESP8266 をUSB接続**

2. **コードを編集：**
   ```cpp
   #define CHILD_ID 1  // 1に変更
   ```

3. **アップロード**

4. **シリアルモニタで確認：**
   ```
   Child #1 MAC Address: AA:BB:CC:DD:EE:FF
   ESP-NOW Child #1 Ready!
   ```

5. **繰り返す** (CHILD_ID = 2, 3, ... 最大19)

---

## ✅ 完成チェックリスト

アップロード後、以下を確認してください：

- [ ] シリアルモニタに初期化メッセージが表示される
- [ ] 赤色LED が初期状態で消灯している
- [ ] デバイスをフリフリすると LED が点灯する
- [ ] LED が消灯する（フリフリ終了後）
- [ ] シリアルモニタに "SHAKE!" メッセージが表示される
- [ ] 複数回フリフリするとカウントが増える
- [ ] 親機がデータを受信している

すべて OK なら、本番環境での使用準備完了です！

---

## 📚 参考資料

- [ESP8266 Arduino Core ドキュメント](https://arduino-esp8266.readthedocs.io/)
- [ESP-NOW プロトコル](https://docs.espressif.com/projects/esp-idf/en/latest/esp8266/api-reference/wifi/esp_now.html)
- [MPU-6050 データシート](https://www.invensense.com/wp-content/uploads/2015/02/MPU-6000-Datasheet1.pdf)

---

**作成日:** 2025-11-10  
**対応バージョン:** Arduino IDE 2.0+, ESP8266 Community Board 3.0.0+
