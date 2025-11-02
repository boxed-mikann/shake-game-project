#include <esp_now.h>
#include <WiFi.h>

typedef struct {
  int childID;
  int shakeCount;
  float acceleration;
  int acX;
  int acY;
  int acZ;
} ShakeDataWithAccel;

typedef struct {
  int command;
} CommandData;

#define MAX_CHILDREN 20
ShakeDataWithAccel childData[MAX_CHILDREN];

uint8_t broadcastAddress[6] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
esp_now_peer_info_t broadcastPeerInfo;
int currentCommand = 1;

void OnDataRecv(const esp_now_recv_info *recv_info, const uint8_t *incomingData, int len) {
  ShakeDataWithAccel receivedData;
  memcpy(&receivedData, incomingData, sizeof(receivedData));
  
  if (receivedData.childID >= 0 && receivedData.childID < MAX_CHILDREN) {
    childData[receivedData.childID] = receivedData;
    
    // ★ Processing用 CSV形式（拡張版）
    // Format: childID,shakeCount,acceleration,acX,acY,acZ
    Serial.print(receivedData.childID);
    Serial.print(",");
    Serial.print(receivedData.shakeCount);
    Serial.print(",");
    Serial.print(receivedData.acceleration);
    Serial.print(",");
    Serial.print(receivedData.acX);
    Serial.print(",");
    Serial.print(receivedData.acY);
    Serial.print(",");
    Serial.println(receivedData.acZ);
  }
}

void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // 送信結果
}

void broadcastCommand(int command) {
  CommandData commandData;
  commandData.command = command;
  
  currentCommand = command;
  
  esp_now_send(broadcastAddress, (uint8_t *) &commandData, sizeof(commandData));
  
  Serial.print("CMD,");
  Serial.println(command);
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  WiFi.mode(WIFI_STA);
  delay(100);
  
  Serial.print("Parent MAC Address: ");
  Serial.println(WiFi.macAddress());
  
  if (esp_now_init() != ESP_OK) {
    Serial.println("ESP-NOW init failed");
    return;
  }
  
  esp_now_register_recv_cb(OnDataRecv);
  esp_now_register_send_cb(OnDataSent);
  
  memcpy(broadcastPeerInfo.peer_addr, broadcastAddress, 6);
  broadcastPeerInfo.channel = 0;
  broadcastPeerInfo.encrypt = false;
  
  if (esp_now_add_peer(&broadcastPeerInfo) != ESP_OK) {
    Serial.println("Failed to add broadcast peer");
    return;
  }
  
  Serial.println("ESP-NOW Parent Ready!");
  Serial.println("Waiting for commands from Processing...");
}

void loop() {
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    
    if (command == "ON") {
      broadcastCommand(1);
      Serial.println("DEBUG: Shake detection ON");
    } 
    else if (command == "OFF") {
      broadcastCommand(0);
      Serial.println("DEBUG: Shake detection OFF");
    }
  }
  
  delay(50);
}