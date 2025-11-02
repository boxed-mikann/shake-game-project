#include <esp_now.h>
#include <WiFi.h>

// 受信用のデータ構造体
typedef struct {
  int childID;
  int shakeCount;
  float acceleration;
} ShakeData;

// ★ ブロードキャスト用のコマンド構造体
typedef struct {
  int command;  // 0 = OFF, 1 = ON
} CommandData;

#define MAX_CHILDREN 20
ShakeData childData[MAX_CHILDREN];

// ブロードキャストアドレス（全子機へ送信）
uint8_t broadcastAddress[6] = {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
esp_now_peer_info_t broadcastPeerInfo;
int currentCommand = 1;  // デフォルト: ON

void OnDataRecv(const esp_now_recv_info *recv_info, const uint8_t *incomingData, int len) {
  ShakeData receivedData;
  memcpy(&receivedData, incomingData, sizeof(receivedData));
  
  if (receivedData.childID >= 0 && receivedData.childID < MAX_CHILDREN) {
    childData[receivedData.childID] = receivedData;
    
    // Processing用 CSV形式
    Serial.print(receivedData.childID);
    Serial.print(",");
    Serial.print(receivedData.shakeCount);
    Serial.print(",");
    Serial.println(receivedData.acceleration);
  }
}

// データ送信コールバック
void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // Serial.print("Broadcast status: ");
  // Serial.println(status == ESP_NOW_SEND_SUCCESS ? "Success" : "Fail");
}

void broadcastCommand(int command) {
  CommandData commandData;
  commandData.command = command;
  
  currentCommand = command;
  
  // 全子機にブロードキャスト
  esp_now_send(broadcastAddress, (uint8_t *) &commandData, sizeof(commandData));
  
  // ★ Processing に状態を返信
  Serial.print("CMD,");
  Serial.println(command);  // 0=OFF, 1=ON
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
  
  // ブロードキャストピアを登録
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
  // Processing からのコマンド受信
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