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
  
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);
  Wire.write(0);
  Wire.endTransmission(true);
  
  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_register_send_cb(OnDataSent);
  // ★ コマンド受信コールバック登録
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
  
  float totalAccel = sqrt(pow((long)AcX, 2) +
                          pow((long)AcY, 2) +
                          pow((long)AcZ, 2));
  
  // ★ 計測がONの場合のみフリフリを検知
  if (shakeMeasurementEnabled) {
    if (totalAccel > SHAKE_THRESHOLD) {
      if (!isShaking) {
        isShaking = true;
        shakeCount++;
        
        shakeData.childID = CHILD_ID;
        shakeData.shakeCount = shakeCount;
        shakeData.acceleration = totalAccel;
        
        esp_now_send(parentMAC, (uint8_t *) &shakeData, sizeof(shakeData));
        
        Serial.print("SHAKE! ID: ");
        Serial.print(CHILD_ID);
        Serial.print(" | Count: ");
        Serial.print(shakeCount);
        Serial.print(" | Accel: ");
        Serial.println(totalAccel);
      }
    } else {
      isShaking = false;
    }
  } else {
    // OFFの場合は状態をリセット
    isShaking = false;
  }
  
  delay(50);
}