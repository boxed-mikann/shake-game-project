# セットアップガイド

## 1. 開発環境のセットアップ

### Arduino IDE のインストール

1. [Arduino公式サイト](https://www.arduino.cc/en/software) から Arduino IDE 2.0以上をダウンロード
2. インストーラーを実行

### ESP32 ボードパッケージのインストール

1. Arduino IDE を開く
2. **File → Preferences** を選択
3. "Additional Boards Manager URLs" に以下を追加:
   ```
   https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json
   ```
   (やらなくても良かった。)
4. **Tools → Board → Boards Manager** を開く
5. "esp32" で検索して "esp32 by Espressif Systems" をインストール

### Processing のインストール

1. [Processing公式サイト](https://processing.org/download) から Processing 4.0以上をダウンロード
2. インストーラーを実行

## 2. ハードウェア接続

### 子機の配線（ESP32 + MPU-6050）

```
ESP32        MPU-6050 (GY-521)
─────────────────────────────
D21 ──→ SDA
D22 ──→ SCL
GND     ──→ GND
VIN    ──→ VCC
```

詳細は [hardware/wiring_diagram.md](../hardware/wiring_diagram.md) を参照

## 3. ファームウェアの書き込み

### 親機（ESP32）の書き込み

1. `firmware/parent_device/parent_device.ino` を Arduino IDE で開く
2. **Tools → Board → ESP32 Dev Module** を選択
3. **Tools → Port** で親機が接続されているポートを選択
4. **Sketch → Upload** でアップロード
5. シリアルモニタで "Parent MAC Address: XX:XX:XX:XX:XX:XX" を確認

### 子機（ESP32）の書き込み

1. `firmware/child_device/child_device.ino` を Arduino IDE で開く
2. **コードの編集:**
   ```cpp
   #define CHILD_ID 0  // 子機1は0、子機2は1、... 子機20は19
   ```
3. **Tools → Port** で子機が接続されているポートを選択
4. **Sketch → Upload** でアップロード
5. シリアルモニタで "Child #X Ready!" を確認
6. **別の子機の場合、`CHILD_ID`を変更して繰り返す**

## 4. Processing の実行

1. `software/processing/shake_game_display/shake_game_display.pde` を Processing で開く
2. **ポート設定の確認:**
   ```processing
   String portName = Serial.list()[0]; // 親機が接続されているポート
   ```
3. ▶ ボタンで実行
4. グリッド表示でリアルタイムデータが表示されることを確認

## 5. 動作確認

### ステップ1: 親機の起動

```
1. 親機（ESP32）を PC に接続
2. Arduino IDE のシリアルモニタで確認
   → "ESP-NOW Parent Ready!"
   → "Waiting for child devices..."
```

### ステップ2: 子機の起動

```
1. 子機（ESP32 + MPU-6050）を PC に接続
2. Arduino IDE のシリアルモニタで確認
   → "Child #0 Ready!"
3. 子機をフリフリ
   → "SHAKE! ID: 0 | Count: 1 | Accel: XXXXX"
```

### ステップ3: 親機でのデータ受信

```
1. 親機の Arduino IDE のシリアルモニタを確認
2. 子機をフリフリすると以下が表示される:
   → "Child #0 (...) | Count: 1 | Accel: XXXXX"
```

### ステップ4: Processing での表示

```
1. 親機を PC に USB で接続
2. Processing を実行
3. グリッド表示にプレイヤーデータが表示される
4. 子機をフリフリして、カウント数が更新されることを確認
```

## トラブルシューティング

### Q: "Board not found" エラーが出る

A: 
- USB ドライバがインストールされているか確認
- ボードを USB ポートに接続しなおす
- 別の USB ポートを試す

### Q: シリアルモニタに何も表示されない

A:
- ボーレート設定を 115200 に確認
- Tools → Port で正しいポートを選択
- USB ケーブルを別のケーブルに変更

### Q: Processing で "Port not found" エラー

A:
- `Serial.list()[0]` を `Serial.list()[1]` など別のポート番号に変更
- Processing のコンソールに出力される有効なポートを確認

詳細は [docs/troubleshooting.md](./troubleshooting.md) を参照