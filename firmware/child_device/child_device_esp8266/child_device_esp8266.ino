#include <Wire.h>
#include <espnow.h>
#include <ESP8266WiFi.h>

// ===== ESP8266用設定 =====
// ESP8266のボード備え付けLED: GPIO16
// 注意: ESP8266の内蔵LEDはアクティブロー（LOW=点灯、HIGH=消灯）
// D0 や D1, D2 はピン定義がないため、GPIO番号で直接指定
const int LED_PIN = 16;   // GPIO16 - ESP8266 ボード備え付けの赤色LED
const int SDA_PIN = 4;    // GPIO4  - I2C SDA
const int SCL_PIN = 5;    // GPIO5  - I2C SCL
const int MPU_addr = 0x68;

int16_t AcX, AcY, AcZ;
int shakeCount = 0;
bool isShaking = false;

#define CHILD_ID 0
uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};

// ★ ジャーク検知用のパラメータ
float previousAccel = 0;
const float JERK_THRESHOLD = 20000.0;      // ジャーク（加速度の変化率）の閾値
const int DEBOUNCE_TIME = 0;               // デバウンス時間（ms）
unsigned long lastShakeTime = 0;
bool initialized = false;

// ★ 親機からのコマンドを受信
typedef struct {
  int command;  // 0 = OFF, 1 = ON
} CommandData;

// ★ フリフリ計測のON/OFF フラグ
bool shakeMeasurementEnabled = true;

typedef struct {
  int childID;
  int shakeCount;
  float acceleration;
} ShakeData;

ShakeData shakeData;

// ★ ESP-NOW送信完了コールバック
// ESP8266用の関数シグネチャ: void callback(uint8_t* macaddr, uint8_t status)
void OnDataSent(uint8_t *mac_addr, uint8_t status) {
  // 送信結果
  // status: 0 = 送信成功、1 = 送信失敗
}

// ★ コマンド受信コールバック
void OnDataRecv(uint8_t *mac_addr, uint8_t *incomingData, uint8_t len) {
  CommandData receivedCommand;
  memcpy(&receivedCommand, incomingData, sizeof(receivedCommand));
  
  if (receivedCommand.command == 0) {
    shakeMeasurementEnabled = false;
    Serial.println("Shake measurement: OFF");
  } 
  else if (receivedCommand.command == 1) {
    shakeMeasurementEnabled = true;
    Serial.println("Shake measurement: ON");
  }
}

void setup() {
  Serial.begin(115200);
  delay(2000);  // ESP8266は起動に時間がかかるため少し長めに待機
  
  Serial.println("\n\n=== ESP8266 Shake Detection ===");
  
  // ★ LED ピン設定
  pinMode(LED_PIN, OUTPUT);
  // ESP8266のLEDはアクティブロー（LOW=点灯、HIGH=消灯）
  digitalWrite(LED_PIN, HIGH);  // 初期状態は消灯
  
  // ★ I2C初期化
  // ESP8266のI2C: SDA=GPIO4, SCL=GPIO5
  Wire.begin(SDA_PIN, SCL_PIN);
  Wire.setClock(400000);  // I2Cクロック速度を400kHzに設定
  
  WiFi.mode(WIFI_STA);
  WiFi.disconnect();
  delay(100);
  
  Serial.print("Child #");
  Serial.print(CHILD_ID);
  Serial.print(" MAC Address: ");
  Serial.println(WiFi.macAddress());
  Serial.println("=== Jerk-based Shake Detection (ESP8266) ===");
  Serial.print("JERK_THRESHOLD: ");
  Serial.println(JERK_THRESHOLD);
  Serial.print("DEBOUNCE_TIME: ");
  Serial.println(DEBOUNCE_TIME);
  Serial.println("LED PIN: GPIO16 - Built-in Red LED (Active Low)");
  
  // ★ MPU-6050初期化
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);
  Wire.write(0);
  Wire.endTransmission(true);
  
  // ★ ESP-NOW初期化
  if (esp_now_init() != 0) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_set_self_role(ESP_NOW_ROLE_CONTROLLER);
  esp_now_register_send_cb(OnDataSent);
  esp_now_register_recv_cb(OnDataRecv);
  
  // ★ 親機を登録
  esp_now_add_peer(parentMAC, ESP_NOW_ROLE_SLAVE, 1, NULL, 0);
  
  Serial.print("ESP-NOW Child #");
  Serial.print(CHILD_ID);
  Serial.println(" Ready!");
  Serial.println("Waiting for shake...");
}

void loop() {
  // ★ MPU-6050からデータ読取
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x3B);
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 6, 1);  // 最後の引数を 1 に変更（ESP8266互換）
  
  AcX = Wire.read() << 8 | Wire.read();
  AcY = Wire.read() << 8 | Wire.read();
  AcZ = Wire.read() << 8 | Wire.read();
  
  // 合成加速度を計算
  float currentAccel = sqrt(pow((long)AcX, 2) +
                            pow((long)AcY, 2) +
                            pow((long)AcZ, 2));
  
  // ★ 改善版：ジャーク検知 + デバウンス
  if (shakeMeasurementEnabled) {
    // ★ 初期化時の誤検知を防ぐ
    if (!initialized) {
      previousAccel = currentAccel;
      initialized = true;
      Serial.print("Initialized with Accel: ");
      Serial.println(currentAccel);
    }
    
    // ジャーク（加速度の変化率）を計算
    float jerk = abs(currentAccel - previousAccel);
    
    // ★ デバッグ用：全ジャーク値を出力（コメント化）
    // Serial.print("Accel: ");
    // Serial.print(currentAccel, 0);
    // Serial.print(" | Jerk: ");
    // Serial.print(jerk, 0);
    // Serial.print(" | isShaking: ");
    // Serial.println(isShaking);
    
    // ジャーク検知 + デバウンス
    if (jerk > JERK_THRESHOLD && !isShaking) {
      // ★ デバウンス時間をチェック（0の場合はスキップ）
      if (DEBOUNCE_TIME == 0 || millis() - lastShakeTime > DEBOUNCE_TIME) {
        isShaking = true;
        shakeCount++;
        lastShakeTime = millis();
        
        // ★ LEDを点灯（ESP8266はアクティブロー）
        digitalWrite(LED_PIN, LOW);
        
        shakeData.childID = CHILD_ID;
        shakeData.shakeCount = shakeCount;
        shakeData.acceleration = currentAccel;
        
        // ★ ESP-NOW で親機に送信
        esp_now_send(parentMAC, (uint8_t *)&shakeData, sizeof(shakeData));
        
        Serial.print(">>> SHAKE! ID: ");
        Serial.print(CHILD_ID);
        Serial.print(" | Count: ");
        Serial.print(shakeCount);
        Serial.print(" | Jerk: ");
        Serial.print(jerk, 0);
        Serial.println(" | LED: ON");
      }
    }
    
    // ★ 改善版リセット条件：加速度が低下したら検知完了状態に戻す
    if (isShaking && currentAccel < 30000.0) {
      isShaking = false;
      // ★ LEDを消灯（ESP8266はアクティブロー）
      digitalWrite(LED_PIN, HIGH);
      Serial.println("Reset shake state | LED: OFF");
    }
  } else {
    // OFFの場合は状態をリセット
    isShaking = false;
    // ★ LEDを消灯
    digitalWrite(LED_PIN, HIGH);
  }
  
  // ★ 前フレームの加速度を保存
  previousAccel = currentAccel;
  
  delay(50);
}
