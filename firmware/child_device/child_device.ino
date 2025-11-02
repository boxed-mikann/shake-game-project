#include <Wire.h>
#include <esp_now.h>
#include <WiFi.h>

const int MPU_addr = 0x68;
int16_t AcX, AcY, AcZ;
int shakeCount = 0;
bool isShaking = false;

#define CHILD_ID 0
uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};

// ★ ジャーク検知用のパラメータ
float previousAccel = 0;
const float JERK_THRESHOLD = 20000.0;      // ジャーク（加速度の変化率）の閾値
const int DEBOUNCE_TIME = 0;            // デバウンス時間（ms）
unsigned long lastShakeTime = 0;
bool initialized = false;                 // ★ 初期化フラグ

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
esp_now_peer_info_t peerInfo;

void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // 送信結果
}

// ★ コマンド受信コールバック
void OnDataRecv(const esp_now_recv_info *recv_info, const uint8_t *incomingData, int len) {
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
  delay(1000);
  
  Wire.begin(21, 22);
  
  WiFi.mode(WIFI_STA);
  delay(100);
  
  Serial.print("Child #");
  Serial.print(CHILD_ID);
  Serial.print(" MAC Address: ");
  Serial.println(WiFi.macAddress());
  Serial.println("=== Jerk-based Shake Detection ===");
  Serial.print("JERK_THRESHOLD: ");
  Serial.println(JERK_THRESHOLD);
  Serial.print("DEBOUNCE_TIME: ");
  Serial.println(DEBOUNCE_TIME);
  
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);
  Wire.write(0);
  Wire.endTransmission(true);
  
  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_register_send_cb(OnDataSent);
  esp_now_register_recv_cb(OnDataRecv);
  
  memcpy(peerInfo.peer_addr, parentMAC, 6);
  peerInfo.channel = 0;
  peerInfo.encrypt = false;
  
  if (esp_now_add_peer(&peerInfo) != ESP_OK) {
    Serial.println("Failed to add peer");
    return;
  }
  
  Serial.print("ESP-NOW Child #");
  Serial.print(CHILD_ID);
  Serial.println(" Ready!");
}

void loop() {
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x3B);
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 6, true);
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
    
    // ★ デバッグ用：全ジャーク値を出力
    Serial.print("Accel: ");
    Serial.print(currentAccel, 0);
    Serial.print(" | Jerk: ");
    Serial.print(jerk, 0);
    Serial.print(" | isShaking: ");
    Serial.println(isShaking);
    
    // ジャーク検知 + デバウンス
    if (jerk > JERK_THRESHOLD && !isShaking) {
      // ★ デバウンス時間をチェック（0の場合はスキップ）
      if (DEBOUNCE_TIME == 0 || millis() - lastShakeTime > DEBOUNCE_TIME) {
        isShaking = true;
        shakeCount++;
        lastShakeTime = millis();
        
        shakeData.childID = CHILD_ID;
        shakeData.shakeCount = shakeCount;
        shakeData.acceleration = currentAccel;
        
        esp_now_send(parentMAC, (uint8_t *) &shakeData, sizeof(shakeData));
        
        Serial.print(">>> SHAKE! ID: ");
        Serial.print(CHILD_ID);
        Serial.print(" | Count: ");
        Serial.print(shakeCount);
        Serial.print(" | Jerk: ");
        Serial.println(jerk);
      }
    }
    
    // ★ 改善版リセット条件：加速度が低下したら検知完了状態に戻す
    // CSVデータから、フリフリ後は加速度が30000未満に戻る
    if (isShaking && currentAccel < 30000.0) {
      isShaking = false;
      Serial.println("Reset shake state");
    }
  } else {
    // OFFの場合は状態をリセット
    isShaking = false;
  }
  
  // ★ 前フレームの加速度を保存
  previousAccel = currentAccel;
  
  delay(50);
}