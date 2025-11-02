#include <Wire.h>
#include <esp_now.h>
#include <WiFi.h>

const int MPU_addr = 0x68;
int16_t AcX, AcY, AcZ;
int shakeCount = 0;
bool isShaking = false;
#define SHAKE_THRESHOLD 15000.0

#define CHILD_ID 0
uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};

typedef struct {
  int command;
} CommandData;

bool shakeMeasurementEnabled = true;

// ★ 親機へ常時送信するデータ構造体
typedef struct {
  int childID;
  int shakeCount;
  float acceleration;
  int acX;
  int acY;
  int acZ;
} ShakeDataWithAccel;

ShakeDataWithAccel shakeData;
esp_now_peer_info_t peerInfo;

// ★ 送信タイミング用
unsigned long lastSendTime = 0;
const int SEND_INTERVAL = 50;  // 50ms間隔

void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // 送信結果
}

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
  
  lastSendTime = millis();
}

void loop() {
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x3B);
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 6, true);
  AcX = Wire.read() << 8 | Wire.read();
  AcY = Wire.read() << 8 | Wire.read();
  AcZ = Wire.read() << 8 | Wire.read();
  
  float totalAccel = sqrt(pow((long)AcX, 2) +
                          pow((long)AcY, 2) +
                          pow((long)AcZ, 2));
  
  // ★ フリフリ判定（ただしカウント増加は1回だけ）
  if (shakeMeasurementEnabled && totalAccel > SHAKE_THRESHOLD) {
    if (!isShaking) {
      isShaking = true;
      shakeCount++;
      Serial.print("SHAKE! ID: ");
      Serial.print(CHILD_ID);
      Serial.print(" | Count: ");
      Serial.println(shakeCount);
    }
  } else {
    isShaking = false;
  }
  
  // ★ 50ms間隔で常時データを送信
  unsigned long currentTime = millis();
  if (currentTime - lastSendTime >= SEND_INTERVAL) {
    lastSendTime = currentTime;
    
    shakeData.childID = CHILD_ID;
    shakeData.shakeCount = shakeCount;
    shakeData.acceleration = totalAccel;
    shakeData.acX = AcX;
    shakeData.acY = AcY;
    shakeData.acZ = AcZ;
    
    // 親機に送信
    esp_now_send(parentMAC, (uint8_t *) &shakeData, sizeof(shakeData));
  }
  
  delay(5);  // 短い遅延（ポーリング用）
}