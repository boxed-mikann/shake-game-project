#include <Wire.h>
#include <esp_now.h>
#include <WiFi.h>

const int MPU_addr = 0x68;
int16_t AcX, AcY, AcZ;
int shakeCount = 0;
bool isShaking = false;
#define SHAKE_THRESHOLD 15000.0

// ★ 各子機に異なるIDを割り当てる ★
#define CHILD_ID 0  // 子機1は0、子機2は1、...、子機20は19

// 親機のMACアドレス
uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};  // ← 親機のMACに修正

// 送信用のデータ構造体
typedef struct {
  int childID;
  int shakeCount;
  float acceleration;
} ShakeData;

ShakeData shakeData;
esp_now_peer_info_t peerInfo;

// ★ 新しいシグネチャに対応したコールバック関数 ★
void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // Serial.print("Send status: ");
  // Serial.println(status == ESP_NOW_SEND_SUCCESS ? "Success" : "Fail");
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Wire.begin(21, 22);
  
  // WiFi STA モード設定
  WiFi.mode(WIFI_STA);
  delay(100);
  
  Serial.print("Child #");
  Serial.print(CHILD_ID);
  Serial.print(" MAC Address: ");
  Serial.println(WiFi.macAddress());
  
  // MPU-6050初期化
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);
  Wire.write(0);
  Wire.endTransmission(true);
  
  // ESP-NOW初期化
  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_register_send_cb(OnDataSent);
  
  // 親機をピアとして登録
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
  
  if (totalAccel > SHAKE_THRESHOLD) {
    if (!isShaking) {
      isShaking = true;
      shakeCount++;
      
      // ★ childIDを構造体に追加 ★
      shakeData.childID = CHILD_ID;
      shakeData.shakeCount = shakeCount;
      shakeData.acceleration = totalAccel;
      
      // ESP-NOWで送信
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
  
  delay(50);
}