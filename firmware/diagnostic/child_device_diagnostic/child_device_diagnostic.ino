#include <Wire.h>

const int MPU_addr = 0x68;
int16_t AcX, AcY, AcZ;

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Wire.begin(21, 22);
  
  // MPU-6050 初期化
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x6B);  // PWR_MGMT_1
  Wire.write(0);     // Wake up
  Wire.endTransmission(true);
  
  Serial.println("=== Acceleration Diagnostic Mode ===");
  Serial.println("Format: AcX,AcY,AcZ");
  Serial.println("Sending data...");
}

void loop() {
  // MPU-6050 から加速度データを読み込む
  Wire.beginTransmission(MPU_addr);
  Wire.write(0x3B);  // ACCEL_XOUT_H
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_addr, 6, true);
  
  AcX = Wire.read() << 8 | Wire.read();
  AcY = Wire.read() << 8 | Wire.read();
  AcZ = Wire.read() << 8 | Wire.read();
  
  // CSV形式で送信
  Serial.print(AcX);
  Serial.print(",");
  Serial.print(AcY);
  Serial.print(",");
  Serial.println(AcZ);
  
  delay(50);  // 50ms ごと（20Hz）
}