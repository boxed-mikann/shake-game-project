# ハードウェア配線図

## 親機（ESP32）

親機は通信・制御のみで、外部センサーは不要です。

```
USB PC ─→ Micro USB ─→ ESP32 Dev Board
```

## 子機（ESP32 + MPU-6050）

```
ESP32 Dev Board
├─ GPIO 21 (SDA) ─→ MPU-6050 SDA
├─ GPIO 22 (SCL) ─→ MPU-6050 SCL
├─ GND ─────────→ MPU-6050 GND
├─ 3.3V ────────→ MPU-6050 VCC
└─ Micro USB (DC Power)
```

### MPU-6050 ピン配置

```
MPU-6050 (GY-521)
├─ VCC: 3.3V
├─ GND: GND
├─ SCL: GPIO 22
├─ SDA: GPIO 21
├─ AD0: GND (I2C Address 0x68)
└─ INT: 未使用
```

## 複数子機の接続

各子機を同じ親機にESP-NOWで接続します。物理的な配線は不要です。

```
   子機1
   ↓ (ESP-NOW)
親機 ← 子機2
|  ↑ (ESP-NOW)
|  子機20
↓ (USB Serial)
PC (Processing 表示)
```

## 注意事項(AI生成,未調査)

- ESP32のGPIO 21, 22 は I2C 用に予約されています
- I2C Address は MPU-6050 の AD0 ピンで変更できます（GND=0x68, VCC=0x69）