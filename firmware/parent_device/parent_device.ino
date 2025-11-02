#include <esp_now.h>
#include <WiFi.h>

// 受信用のデータ構造体
typedef struct {
  int childID;          // 子機ID
  int shakeCount;
  float acceleration;
} ShakeData;

// 複数子機のデータ保存（最大20台）
#define MAX_CHILDREN 20
ShakeData childData[MAX_CHILDREN];

// データ受信コールバック
void OnDataRecv(const esp_now_recv_info *recv_info, const uint8_t *incomingData, int len) {
  ShakeData receivedData;
  memcpy(&receivedData, incomingData, sizeof(receivedData));
  
  // 子機IDに基づいてデータ保存
  if (receivedData.childID >= 0 && receivedData.childID < MAX_CHILDREN) {
    childData[receivedData.childID] = receivedData;
    
    Serial.print("Child #");
    Serial.print(receivedData.childID);
    Serial.print(" (");
    for(int i = 0; i < 6; i++){
      Serial.print(recv_info->src_addr[i], HEX);
      if(i < 5) Serial.print(":");
    }
    Serial.print(") | Count: ");
    Serial.print(receivedData.shakeCount);
    Serial.print(" | Accel: ");
    Serial.println(receivedData.acceleration);
  }
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  // WiFi STA モード設定
  WiFi.mode(WIFI_STA);
  delay(100);
  
  Serial.print("Parent MAC Address: ");
  Serial.println(WiFi.macAddress());
  
  // ESP-NOW初期化
  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_register_recv_cb(OnDataRecv);
  
  Serial.println("ESP-NOW Parent Ready!");
  Serial.println("Waiting for child devices...");
}

void loop() {
  // 定期的に全子機のデータを表示（オプション）
  delay(1000);
  
  // 必要に応じて、ここで全子機のデータをPC（Processing）に送信
  // Serial.print("DATA,");
  // for(int i = 0; i < MAX_CHILDREN; i++) {
  //   Serial.print(childData[i].shakeCount);
  //   if(i < MAX_CHILDREN - 1) Serial.print(",");
  // }
  // Serial.println(",END");
}