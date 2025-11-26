#include <Wire.h>
#include <esp_now.h>
#include <WiFi.h>

// ★ デバッグモード設定（本番環境ではコメント化してUART無効化）
#define DEBUG

const int MPU_addr = 0x68;
const int LED_PIN = 13;  // ★ LED制御用GPIO 13
int16_t AcX, AcY, AcZ;
int shakeCount = 0;
bool isShaking = false;

#define CHILD_ID 0
uint8_t parentMAC[] = {0x08, 0x3A, 0xF2, 0x52, 0x9E, 0x54};

// ★ ベクトル内積判定用（シェイク状態時のみ使用）
int16_t prevAcX = 0, prevAcY = 0, prevAcZ = 0;  // 前フレーム加速度
//int16_t baseVecX = 0, baseVecY = 0, baseVecZ = 0;  // シェイク開始時の基準差分ベクトル（既に右シフト済み）
//const int VECTOR_SHIFT = 2;  // 右シフト量（÷4、精度±4）

// ★ ジャーク検知用のパラメータ
float previousAccel = 0;
bool initialized = false;                 // ★ 初期化フラグ
unsigned long lastShakeTime = 0;
float baseAccel = 0;

// ★ 調整用パラメータ
// シェイク検知
const float JERK_THRESHOLD = 17000.0;      // ジャーク（加速度の変化率）の閾値
const int DEBOUNCE_DELAY = 0;            // デバウンス用のdelay時間（ms）
//const int32_t DOT_PRODUCT_THRESHOLD = 0;  // 内積が0以下→振り戻し判定
const float ACCEL_THRESHOULD = 40000;

// シェイク状態解除検知
const int DURATON_TIME = 400;             //一定時間(ms)経過後shake状態を解除する
const float BACK_JERK_THRESHOULD = -11000;
const float BACK_ACCEL_THRESHOULD = 16000;

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

// ★ 検証用データ構造体 - 全データを毎フレーム送信
//typedef struct {
//  uint32_t frameCount;         // フレーム番号
//  int16_t acX, acY, acZ;       // センサー値（生値）
//  float totalAccel;            // 合成加速度
//  float jerk;                  // ジャークの大きさ
//  int32_t dotProduct;          // ベクトル内積
//  int shakeCount;              // シェイク検知数
//  uint8_t isShaking;           // シェイク状態（0 or 1）
//  int16_t baseVecX, baseVecY, baseVecZ;  // 基準ベクトル
//  int childID;
//} ValidationData;

//ValidationData validationData;
uint32_t frameCount = 0;

// ★ MPU-6050スリープ制御関数
void mpu6050Sleep() {
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);      // PWR_MGMT_1 レジスタ
  Wire.write(0x40);      // SLEEP ビットをセット
  Wire.endTransmission(true);
#ifdef DEBUG
  Serial.println("MPU-6050: Sleep mode");
#endif
}

void mpu6050Wake() {
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);      // PWR_MGMT_1 レジスタ
  Wire.write(0x00);      // Sleep ビットをクリア
  Wire.endTransmission(true);
  delay(30);             // MPU-6050復帰待機
  initialized = false;   // 再初期化フラグをリセット
#ifdef DEBUG
  Serial.println("MPU-6050: Wake mode");
#endif
}

void OnDataSent(const wifi_tx_info_t *tx_info, esp_now_send_status_t status) {
  // 送信結果
}

// ★ コマンド受信コールバック
void OnDataRecv(const esp_now_recv_info *recv_info, const uint8_t *incomingData, int len) {
  CommandData receivedCommand;
  memcpy(&receivedCommand, incomingData, sizeof(receivedCommand));
  
  if (receivedCommand.command == 0) {
    shakeMeasurementEnabled = false;
    // ★ OFF時に状態をリセット
    isShaking = false;
    //baseVecX = 0;
    //baseVecY = 0;
    //baseVecZ = 0;
    baseAccel = 0;
    mpu6050Sleep();        // ★ センサーをスリープ
#ifdef DEBUG
    Serial.println("Shake measurement: OFF");
#endif
  } 
  else if (receivedCommand.command == 1) {
    shakeMeasurementEnabled = true;
    mpu6050Wake();         // ★ センサーを復帰
#ifdef DEBUG
    Serial.println("Shake measurement: ON");
#endif
  }
}

void setup() {
#ifdef DEBUG
  Serial.begin(115200);
  delay(1000);
#endif
  
  // ★ LED ピン設定
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);  // 初期状態は消灯
  
  // ★ I2C初期化（プルアップ設定付き）
  // SDA: GPIO 23, SCL: GPIO 22 (wiring_diagram.md参照)
  Wire.begin(23, 22);
  Wire.setClock(400000);  // I2Cクロック速度を400kHzに設定
  
  WiFi.mode(WIFI_STA);
  delay(100);
  
#ifdef DEBUG
  Serial.print("Child #");
  Serial.print(CHILD_ID);
  Serial.print(" MAC Address: ");
  Serial.println(WiFi.macAddress());
  Serial.println("=== Jerk-based Shake Detection ===");
  Serial.print("JERK_THRESHOLD: ");
  Serial.println(JERK_THRESHOLD);
  Serial.print("DEBOUNCE_DELAY: ");
  Serial.print(DEBOUNCE_DELAY);
  Serial.println(" ms");
#endif
  
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);
  Wire.write(0);
  Wire.endTransmission(true);
    // ★ MPU-6050 加速度レンジ 初期化
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x1C);
  Wire.write(0x00);// ±2g //0x18); // +/- 16G
  Wire.endTransmission(true);
  
  if (esp_now_init() != ESP_OK) {
#ifdef DEBUG
    Serial.println("ESP-NOW init failed");
#endif
    return;
  }
  
  esp_now_register_send_cb(OnDataSent);
  esp_now_register_recv_cb(OnDataRecv);
  
  memcpy(peerInfo.peer_addr, parentMAC, 6);
  peerInfo.channel = 0;
  peerInfo.encrypt = false;
  
  if (esp_now_add_peer(&peerInfo) != ESP_OK) {
#ifdef DEBUG
    Serial.println("Failed to add peer");
#endif
    return;
  }
  
#ifdef DEBUG
  Serial.print("ESP-NOW Child #");
  Serial.print(CHILD_ID);
  Serial.println(" Ready!");
#endif
}

void loop() {
  // ★ OFF時はループをスキップして待機（ESP-NOWコールバックのみ受信）
  if (!shakeMeasurementEnabled) {
    delay(1000);
    return;
  }
  
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
  Serial.print("Accel: ");
  Serial.println(currentAccel);
  
  // ★ 初期化時の誤検知を防ぐ
  if (!initialized) {
    previousAccel = currentAccel;
    initialized = true;
#ifdef DEBUG
    Serial.print("Initialized with Accel: ");
    Serial.println(currentAccel);
#endif
  }
  
  // ジャーク（加速度の変化率）を計算
  float jerk = currentAccel - previousAccel;
  
  // ジャーク検知 + デバウンス
  if ((jerk > JERK_THRESHOLD || currentAccel > ACCEL_THRESHOULD) && !isShaking) {
    isShaking = true;
    shakeCount++;
    lastShakeTime = millis();
    
    // ★ シェイク開始時に基準ベクトルを保存（保存時から右シフト）
    //baseVecX = ((int16_t)(AcX - prevAcX)) >> VECTOR_SHIFT;
    //baseVecY = ((int16_t)(AcY - prevAcY)) >> VECTOR_SHIFT;
    //baseVecZ = ((int16_t)(AcZ - prevAcZ)) >> VECTOR_SHIFT;
    baseAccel = currentAccel;
    // ★ LEDを点灯
    digitalWrite(LED_PIN, HIGH);
    
    shakeData.childID = CHILD_ID;
    shakeData.shakeCount = shakeCount;
    shakeData.acceleration = currentAccel;
    
    esp_now_send(parentMAC, (uint8_t *) &shakeData, sizeof(shakeData));
    
#ifdef DEBUG
    Serial.print(">>> SHAKE! ID: ");
    Serial.print(CHILD_ID);
    Serial.print(" | Count: ");
    Serial.print(shakeCount);
    Serial.print(" | Jerk: ");
    Serial.println(jerk);
#endif
    
    // ★ デバウンス用のdelay
    delay(DEBOUNCE_DELAY);
  }
  // ★ ベクトル内積判定：シェイク状態中のみ実行
  else if (isShaking) {
    // 現在の差分ベクトルを計算（保存時から右シフト）
    //int16_t currentDeltaX = ((int16_t)(AcX - prevAcX)) >> VECTOR_SHIFT;
    //int16_t currentDeltaY = ((int16_t)(AcY - prevAcY)) >> VECTOR_SHIFT;
    //int16_t currentDeltaZ = ((int16_t)(AcZ - prevAcZ)) >> VECTOR_SHIFT;
    
    // 内積計算（baseVecも currentDeltaも既に右シフト済み）
    //int32_t dotProduct = (int32_t)baseVecX * currentDeltaX +
    //                    (int32_t)baseVecY * currentDeltaY +
    //                    (int32_t)baseVecZ * currentDeltaZ;
    
#ifdef DEBUG
    // Serial.print("Dot: "); Serial.print(dotProduct);
    // Serial.print(" | BaseVec: ("); Serial.print(baseVecX); Serial.print(","); Serial.print(baseVecY); Serial.print(","); Serial.print(baseVecZ); Serial.print(")");
    // Serial.print(" | CurrentDelta: ("); Serial.print(currentDeltaX); Serial.print(","); Serial.print(currentDeltaY); Serial.print(","); Serial.print(currentDeltaZ); Serial.println(")");
#endif
    
    // 振り戻し判定
    if (currentAccel <= BACK_ACCEL_THRESHOULD || millis() - lastShakeTime > DURATON_TIME || jerk <= BACK_JERK_THRESHOULD) {//dotProduct <= DOT_PRODUCT_THRESHOLD) {
      isShaking = false;
      // ★ LEDを消灯
      digitalWrite(LED_PIN, LOW);
// #ifdef DEBUG
//       Serial.print(">>> Reset shake state | Dot: "); Serial.println(dotProduct);
// #endif
    }
  }
  
  // ★ 前フレームの加速度を保存（enabledのみ）
  previousAccel = currentAccel;
  prevAcX = AcX;
  prevAcY = AcY;
  prevAcZ = AcZ;
  
  // ★★ ここから検証用データ送信（毎フレーム） ★★
  // ★ 毎フレーム内積を計算（シェイク判定に実際に使われている値）
  //int32_t currentDotProduct = 0;
  //if (isShaking) {
    // シェイク状態中：実際の振り戻し判定に使われている内積を計算
  //  int16_t currentDeltaX = ((int16_t)(AcX - prevAcX)) >> VECTOR_SHIFT;
  //  int16_t currentDeltaY = ((int16_t)(AcY - prevAcY)) >> VECTOR_SHIFT;
  //  int16_t currentDeltaZ = ((int16_t)(AcZ - prevAcZ)) >> VECTOR_SHIFT;
  //  currentDotProduct = (int32_t)baseVecX * currentDeltaX +
  //                      (int32_t)baseVecY * currentDeltaY +
  //                      (int32_t)baseVecZ * currentDeltaZ;
  //}
  
  // validationData.frameCount = frameCount++;
  // validationData.acX = AcX;
  // validationData.acY = AcY;
  // validationData.acZ = AcZ;
  // validationData.totalAccel = currentAccel;
  // validationData.jerk = jerk;
  // validationData.dotProduct = currentDotProduct;
  // validationData.shakeCount = shakeCount;
  // validationData.isShaking = isShaking ? 1 : 0;
  // validationData.baseVecX = baseVecX;
  // validationData.baseVecY = baseVecY;
  // validationData.baseVecZ = baseVecZ;
  // validationData.childID = CHILD_ID;
  
  // ESP-NOW で親機に送信
  //esp_now_send(parentMAC, (uint8_t *) &validationData, sizeof(validationData));
  
  delay(20);
}