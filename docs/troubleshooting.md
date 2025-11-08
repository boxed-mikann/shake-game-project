# トラブルシューティング

## よくある問題と解決方法

### Arduino IDE 関連

#### Q: ボードが認識されない

**症状:** "Board not found" または "Port is busy"

**原因:** 
- USB ドライバが未インストール
- 別のアプリケーションが同じポートを使用中
- USB ケーブルが不良

**解決方法:**
1. USB ケーブルを別のケーブルに変更
2. 別の USB ポート を試す
3. PC を再起動
4. CP2102 ドライバ を手動インストール

#### Q: "Compilation error" が出る

**症状:** コンパイルに失敗

**原因:** ライブラリが未インストール、またはバージョン不一致

**解決方法:**
```
Tools → Manage Libraries → "esp_now" と "Wire" をインストール
```

### 通信関連

#### Q: 親機が子機からのデータを受信できない

**症状:** 親機のシリアルモニタに "Child #X" が表示されない

**原因:**
- 親機と子機の MACアドレスが一致していない
- WiFi.mode(WIFI_STA) が設定されていない
- 子機のコードで `parentMAC[]` が正しく設定されていない

**解決方法:**
1. 親機の MACアドレスを確認
   ```cpp
   Serial.println(WiFi.macAddress());
   ```
2. 子機のコードで MACアドレスを更新
   ```cpp
   uint8_t parentMAC[] = {0xXX, 0xXX, 0xXX, 0xXX, 0xXX, 0xXX};
   ```

#### Q: "ESP-NOW init failed" エラー

**症状:** ESP-NOW が初期化失敗

**原因:** メモリ不足 または ESP32 ボード設定エラー

**解決方法:**
1. **Tools → Flash Size** を "4MB" に設定
2. **Tools → Partition Scheme** を "Default" に設定

### MPU-6050 関連

#### Q: MPU-6050 から値が読み込めない

**症状:** I2C communication error または 加速度値が 0

**原因:**
- GPIO 21, 22 の配線ミス
- MPU-6050 の GND が接続されていない
- I2C デバイスが複数あり、アドレス競合

**解決方法:**
1. 配線を確認（SDA=GPIO 21, SCL=GPIO 22, GND=GND）
2. デジタルマルチメータで導通確認
3. I2C アドレスをスキャン
   ```cpp
   // I2C スキャンコード例
   void setup() {
     Serial.begin(115200);
     Wire.begin(21, 22);
     for (int i = 0; i < 127; i++) {
       Wire.beginTransmission(i);
       if (Wire.endTransmission() == 0) {
         Serial.print("Device found at 0x");
         Serial.println(i, HEX);
       }
     }
   }
   ```

#### Q: フリフリが検知されない

**症状:** "SHAKE!" が表示されない

**原因:** しきい値が高すぎる、またはセンサー不良

**解決方法:**
1. しきい値を下げる
   ```cpp
   #define SHAKE_THRESHOLD 10000.0  // デフォルト: 15000
   ```
2. 現在の加速度値を確認
   ```cpp
   Serial.print("Accel: "); Serial.println(totalAccel);
   ```

### Processing 関連

#### Q: Processing が親機からのデータを受信できない

**症状:** グリッド表示が更新されない

**原因:**
- ポート番号が間違っている
- シリアル通信フォーマットが一致していない

**解決方法:**
1. ポート番号を確認
   ```processing
   println(Serial.list()); // 有効なポート一覧を表示
   ```
2. ボーレート を 115200 に確認
   ```processing
   myPort = new Serial(this, portName, 115200);
   ```

### I2C通信関連 ⭐ NEW

#### Q: I2C NACK エラーが出る

**症状:** 
```
E (11528) i2c.master: I2C hardware NACK detected
E (11528) i2c.master: I2C transaction unexpected nack detected
E (11528) i2c.master: s_i2c_synchronous_transaction(945): I2C transaction failed
```

**原因:**
- SDA/SCL のピン設定が配線と一致していない
- プルアップ抵抗が不足（I2C通信には10kΩのプルアップが必要）
- 配線が緩んでいる、または接触不良

**解決方法:**
1. **ピン設定を確認**（wiring_diagram.md参照）
   ```cpp
   // 正しい設定（SDA: GPIO 23, SCL: GPIO 22）
   Wire.begin(23, 22);  // (SDA, SCL)
   ```

2. **プルアップ抵抗を追加**
   - SDA と SCL それぞれに 10kΩ の抵抗を追加
   - 抵抗をGNDと3.3Vの間に接続
   ```
   3.3V
    ↓
   10kΩ (or 4.7kΩ)
    ↓
   SDA ─→ ESP32 GPIO 23
    
   3.3V
    ↓
   10kΩ (or 4.7kΩ)
    ↓
   SCL ─→ ESP32 GPIO 22
   ```

3. **配線を再確認**
   - 接点がしっかり接触しているか
   - はんだ付けが正しくできているか（特にGND）

4. **I2C通信速度を調整**（必要に応じて）
   ```cpp
   Wire.begin(23, 22);
   Wire.setClock(400000);  // 400kHz (標準値)
   // または低い速度で試す
   Wire.setClock(100000);  // 100kHz
   ```

### LED制御関連 ⭐ NEW

#### Q: LEDが点灯しない

**症状:** フリフリしてもLEDが光らない

**原因:**
- LEDの接続不良
- GPIO 13 の設定ミス
- LED のアノード/カソード接続が逆

**解決方法:**
1. **配線を確認**
   - GPIO 13 にアノード（長い足）を接続
   - GND にカソード（短い足）を接続
   - 抵抗（220Ω程度）を直列に接続

2. **コードの確認**
   ```cpp
   const int LED_PIN = 13;
   pinMode(LED_PIN, OUTPUT);
   digitalWrite(LED_PIN, HIGH);   // 点灯
   digitalWrite(LED_PIN, LOW);    // 消灯
   ```

3. **テストコード**
   ```cpp
   // setup() 内
   pinMode(LED_PIN, OUTPUT);
   
   // loop() 内で常に点灯テスト
   digitalWrite(LED_PIN, HIGH);
   delay(1000);
   ```

## サポート

上記で解決しない場合は、GitHub の Issues でお問い合わせください。
