#include <Arduino.h>
#include <Wire.h>
#include <ESP8266WiFi.h>
#include <math.h> // sqrt() のために必要

// --- 設定 ---
// LEDを接続しているピン (D5ピン = GPIO14)
const int LED_PIN = 14; 

// MPU6050のI2Cアドレス
const int MPU_ADDR = 0x68;

// --- しきい値 (Raw値) ---
// ※この値を調整していきます
const long RAW_SHAKE_THRESHOLD = 5200; // 振ったと認識する強さ
const long RAW_CALM_THRESHOLD  = 2500; // 静かになったと認識する強さ (1G強)

// --- グローバル変数 ---
int shakeCount = 0;        // フリフリの回数
bool isShaking = false;    // 現在シェイク中かどうかの状態

// センサーから読み取るための変数
int16_t ax, ay, az; // 16ビットの生データ

// MPU6050のレジスタ（設定場所）
const byte PWR_MGMT_1   = 0x6B; // 電源管理レジスタ
const byte ACCEL_CONFIG = 0x1C; // 加速度センサー設定レジスタ
const byte ACCEL_XOUT_H = 0x3B; // 加速度データ（X軸の上位バイト）

// --- MPU6050に1バイト書き込むための関数 ---
void writeMPURegister(byte reg, byte value) {
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(reg);   // レジスタのアドレス
  Wire.write(value); // 書き込むデータ
  Wire.endTransmission();
}

// ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
// この void setup() { ... } が必ず必要です
// ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
void setup() {
  WiFi.mode(WIFI_OFF);
  WiFi.forceSleepBegin();
  delay(1); 

  Serial.begin(115200);
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW); 

  Serial.println("フリフリゲーム (しきい値 診断モード)");
  Serial.println("センサーの初期化を開始...");

  Wire.begin(4, 5); 

  // MPU6050の初期化
  writeMPURegister(PWR_MGMT_1, 0x00);
  delay(100);
  
  writeMPURegister(ACCEL_CONFIG, 0x18); // +/- 16G
  delay(100);

  Serial.println("MPU6050 準備完了！");
  Serial.println("現在の「強さ」の数値を表示します...");
}

// ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
// この void loop() { ... } が必ず必要です
// ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
void loop() {
  // --- センサーから加速度データ（6バイト）を読み取る ---
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(ACCEL_XOUT_H); 
  Wire.endTransmission(false); 
  Wire.requestFrom(MPU_ADDR, 6, true); 

  ax = (Wire.read() << 8) | Wire.read(); 
  ay = (Wire.read() << 8) | Wire.read(); 
  az = (Wire.read() << 8) | Wire.read(); 

  // --- フリフリのロジック ---
  long magnitude_raw = sqrt( (long)ax * ax + (long)ay * ay + (long)az * az );

  // ★★★ 追加した診断コード ★★★
  // 現在の「強さ」の数値をシリアルモニタに表示
  Serial.println(magnitude_raw);
  // ★★★ ここまで ★★★

  if (magnitude_raw > RAW_SHAKE_THRESHOLD && !isShaking) {
    isShaking = true; 
    shakeCount++;     

    // (カウントが増えた時だけ、"フリフリ！"と表示)
    Serial.print("フリフリ！ カウント: ");
    Serial.println(shakeCount);
    digitalWrite(LED_PIN, HIGH);
  }
  else if (magnitude_raw < RAW_CALM_THRESHOLD && isShaking) {
    isShaking = false; 
    digitalWrite(LED_PIN, LOW);
  }

  delay(50); 
}