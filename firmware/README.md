# ファームウェア（Arduino）

親機と子機のソースコードを管理するディレクトリです。

---

## 📁 ディレクトリ構成

```
firmware/
├── parent_device/
│   └── parent_device.ino           # 親機プログラム（ESP32用）
│
└── child_device/
    ├── child_device.ino            # 子機プログラム（ESP32用）
    ├── child_device_esp8266.ino    # 子機プログラム（ESP8266用） ⭐ NEW
    └── ESP8266_SETUP_GUIDE.md      # ESP8266セットアップガイド
```

---

## 🎯 各ファイルの用途

### 親機（親機用ESP32）

| ファイル | 説明 | 対応ボード |
|---------|------|----------|
| `parent_device.ino` | 複数子機からのデータ受信・制御 | **ESP32** ✅ |

**役割:**
- 複数の子機（ESP32またはESP8266）からデータを受信
- PC への Serial 出力
- 子機への制御コマンド送信（LED ON/OFF など）

---

### 子機（プレイヤー用ボード）

| ファイル | 説明 | 対応ボード | 状態 | 用途 |
|---------|------|----------|------|------|
| `child_device.ino` | 子機プログラム（標準） | **ESP32** ✅ | ✅ 本番対応 | 試験・量産初期版 |
| `child_device_esp8266.ino` | 子機プログラム（低消費電力版） | **ESP8266** ✅ | ⏳ 試験中 | **本番用（推奨）** |

---

## 🚀 使い分けガイド

### 試験段階（現在）
```
親機:     parent_device.ino     (ESP32)
子機1:    child_device.ino      (ESP32)
子機2:    child_device_esp8266.ino (ESP8266) ← 新規
```

→ **ESP32と ESP8266 の両方を試験して動作確認**

### 本番段階（予定）
```
親機:     parent_device.ino     (ESP32)
子機N:    child_device_esp8266.ino (ESP8266) × 20台
```

→ **全子機を ESP8266 に統一**

---

## 📋 セットアップ手順（クイック）

### ESP32 子機
```
1. firmware/child_device/child_device.ino を開く
2. CHILD_ID と parentMAC を設定
3. Tools → Board: "ESP32 Dev Module" を選択
4. Upload
```

詳細: `../../docs/SETUP.md`

### ESP8266 子機（新）
```
1. firmware/child_device/child_device_esp8266.ino を開く
2. CHILD_ID と parentMAC を設定
3. Tools → Board: "Generic ESP8266 Module" を選択
4. Upload
```

詳細: `ESP8266_SETUP_GUIDE.md` ⭐

---

## ⚙️ 主な相違点

### ESP32 vs ESP8266

| 項目 | ESP32 | ESP8266 |
|------|-------|---------|
| **ボード名** | ESP32 Dev Module | Generic ESP8266 Module |
| **I2C ピン** | GPIO 23, 22 | D2(GPIO4), D1(GPIO5) |
| **LED ピン** | GPIO 13 | D0(GPIO16) |
| **LED 動作** | アクティブハイ（HIGH=点灯） | **アクティブロー（LOW=点灯）** |
| **ボードパッケージ** | esp32 by Espressif | **esp8266 by ESP8266 Community** |
| **消費電力** | 標準 | **低消費電力** ✅ |
| **コスト** | 標準 | **安い** ✅ |

---

## 🔌 ハードウェア配線

### ESP32 子機

```
ESP32 Dev Board
├─ GPIO 23 (SDA) ─→ MPU-6050 SDA
├─ GPIO 22 (SCL) ─→ MPU-6050 SCL
├─ GPIO 13 ─────→ 外部LED (+抵抗220Ω)
├─ GND ──────────→ MPU-6050 GND
├─ 3.3V ─────────→ MPU-6050 VCC
└─ (USB ケーブルで電源供給)
```

### ESP8266 子機

```
ESP8266 Board
├─ D2 (GPIO4) SDA ──→ MPU-6050 SDA
├─ D1 (GPIO5) SCL ──→ MPU-6050 SCL
├─ D0 (GPIO16) ─────→ 赤色LED（ボード備え付け）✅
├─ GND ─────────────→ MPU-6050 GND
├─ 3.3V ────────────→ MPU-6050 VCC
└─ (USB ケーブルで電源供給)
```

詳細: `../../hardware/wiring_diagram.md`

---

## 📝 開発メモ

### LED 点灯の違い

**ESP32（GPIO 13）:**
```cpp
digitalWrite(LED_PIN, HIGH);  // 点灯
digitalWrite(LED_PIN, LOW);   // 消灯
```

**ESP8266（D0/GPIO16、アクティブロー）:**
```cpp
digitalWrite(LED_PIN, LOW);   // 点灯 ⚠️
digitalWrite(LED_PIN, HIGH);  // 消灯 ⚠️
```

### I2C ピン設定

**ESP32:**
```cpp
Wire.begin(23, 22);  // SDA, SCL
```

**ESP8266:**
```cpp
Wire.begin(D2, D1);  // SDA(GPIO4), SCL(GPIO5)
```

### ESP-NOW API の違い

**ESP32:**
```cpp
esp_now_send(parentMAC, data, len);
```

**ESP8266:**
```cpp
esp_now_send(parentMAC, data, len);  // 同じ
```

---

## 🧪 テスト・検証

### テスト項目

- [ ] ESP8266 で I2C 通信が正常に機能するか
- [ ] ESP-NOW で親機にデータが送信されるか
- [ ] ボード備え付けの赤色LED が点灯するか
- [ ] 複数の ESP8266 で同時動作するか
- [ ] バッテリー駆動での動作時間

### 既知の問題

- ESP8266 の起動時間は ESP32 より長め（2秒程度）
- WiFi 接続切り替え時に一時的に ESP-NOW が中断される可能性

---

## 📚 参考資料

- [Arduino IDE 2.0+ ダウンロード](https://www.arduino.cc/en/software)
- [ESP32 セットアップ](https://docs.espressif.com/projects/arduino-esp32/en/latest/getting_started.html)
- [ESP8266 セットアップ](https://arduino-esp8266.readthedocs.io/)
- [ESP-NOW プロトコル](https://docs.espressif.com/projects/esp-idf/en/stable/esp32/api-reference/network/esp_now.html)

---

## 🆘 サポート

問題が発生した場合：

1. 対応するセットアップガイドを確認
2. `../../docs/troubleshooting.md` を参照
3. GitHub Issues で報告

---

**最終更新:** 2025-11-10  
**作成者:** GitHub Copilot
