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

## サポート

上記で解決しない場合は、GitHub の Issues でお問い合わせください。