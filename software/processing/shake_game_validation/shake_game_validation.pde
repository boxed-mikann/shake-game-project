import processing.serial.*;
import java.io.FileWriter;
import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

Serial myPort;

// ★ 検証用データ履歴（複数フレーム分を保持）
class FrameData {
  int frameCount;
  int childID;
  int acX, acY, acZ;
  float totalAccel;
  float jerk;
  int dotProduct;
  int shakeCount;
  int isShaking;
  int baseVecX, baseVecY, baseVecZ;
  
  FrameData(int fc, int cid, int ax, int ay, int az, float ta, float j, int dp, int sc, int s, int bvx, int bvy, int bvz) {
    frameCount = fc;
    childID = cid;
    acX = ax;
    acY = ay;
    acZ = az;
    totalAccel = ta;
    jerk = j;
    dotProduct = dp;
    shakeCount = sc;
    isShaking = s;
    baseVecX = bvx;
    baseVecY = bvy;
    baseVecZ = bvz;
  }
}

ArrayList<FrameData> dataHistory = new ArrayList<FrameData>();
final int MAX_DATA_POINTS = 500;

boolean portConnected = false;
String statusMessage = "Initializing...";

int selectedChildID = 0;
boolean isPaused = false;

// ★ グラフ表示用選択肢
String[] displayModes = {
  "AcX, AcY, AcZ",
  "Total Accel & Jerk",
  "Shake Count & State",
  "Dot Product (Shake State)",
  "Base Vector Components",
  "Dot Product History (Detailed)"
};
int currentDisplayMode = 0;

// ★ スケーリング設定（各値の表示範囲）
final float[] SCALE_RANGES = {
  30000.0,  // Accel X, Y, Z (-30000 to 30000)
  30000.0,  // Total Accel (0 to 30000)
  100.0,    // Jerk (0 to 100000)
  50000.0,  // Dot Product (-50000 to 50000)
  100.0,    // Base Vector components (-500 to 500)
  100000.0  // Dot Product History (-100000 to 100000)
};

void setup() {
  size(1400, 900);
  
  println("=== Shake Detection Validation System ===");
  println("Available Serial Ports:");
  String[] ports = Serial.list();
  for (int i = 0; i < ports.length; i++) {
    println("[" + i + "] " + ports[i]);
  }
  println("==============================");
  
  connectToPort();
}

void connectToPort() {
  String[] ports = Serial.list();
  
  if (ports.length == 0) {
    statusMessage = "ERROR: No serial ports found!";
    println(statusMessage);
    return;
  }
  
  int portIndex = 0;  // Usually the parent device
  
  try {
    if (myPort != null) {
      myPort.stop();
      delay(500);
    }
    
    println("Attempting to connect to: " + ports[portIndex]);
    myPort = new Serial(this, ports[portIndex], 115200);
    myPort.bufferUntil('\n');
    
    portConnected = true;
    statusMessage = "Connected to: " + ports[portIndex];
    println(statusMessage);
    
  } catch (Exception e) {
    portConnected = false;
    statusMessage = "Connection failed: " + e.getMessage();
    println(statusMessage);
  }
}

void serialEvent(Serial p) {
  String inString = p.readStringUntil('\n');
  
  if (inString == null) return;
  
  inString = inString.trim();
  
  // ★ VDATA形式のパース
  if (inString.startsWith("VDATA,")) {
    try {
      String[] values = split(inString.substring(6), ',');
      
      if (values.length < 13) return;
      
      int frameCount = int(values[0]);
      int childID = int(values[1]);
      int acX = int(values[2]);
      int acY = int(values[3]);
      int acZ = int(values[4]);
      float totalAccel = float(values[5]);
      float jerk = float(values[6]);
      int dotProduct = int(values[7]);
      int shakeCount = int(values[8]);
      int isShaking = int(values[9]);
      int baseVecX = int(values[10]);
      int baseVecY = int(values[11]);
      int baseVecZ = int(values[12]);
      
      // フィルター：選択した子機のデータのみ
      if (childID == selectedChildID) {
        if (!isPaused) {
          dataHistory.add(new FrameData(frameCount, childID, acX, acY, acZ, totalAccel, jerk, dotProduct, shakeCount, isShaking, baseVecX, baseVecY, baseVecZ));
          
          // データ数を制限
          if (dataHistory.size() > MAX_DATA_POINTS) {
            dataHistory.remove(0);
          }
        }
      }
    } catch (Exception e) {
      println("Parse error: " + e.getMessage());
    }
  }
  // CMD形式の受信
  else if (inString.startsWith("CMD,")) {
    String cmdStatus = inString.substring(4);
    println("Command status: " + cmdStatus);
  }
}

void draw() {
  background(30);
  
  // ★ ヘッダー表示
  fill(255);
  textSize(24);
  textAlign(LEFT);
  text("Shake Detection Validation System", 20, 30);
  
  fill(portConnected ? color(0, 200, 0) : color(200, 0, 0));
  textSize(14);
  text("● " + statusMessage, 20, 55);
  
  fill(255);
  textSize(12);
  text("Child ID: " + selectedChildID + " | Display Mode: " + displayModes[currentDisplayMode], 20, 75);
  text("Data points: " + dataHistory.size() + " / " + MAX_DATA_POINTS, 20, 90);
  
  if (isPaused) {
    fill(255, 200, 0);
    textSize(12);
    text("[PAUSED]", 20, 105);
  }
  
  fill(150);
  textSize(10);
  text("Keys: SPACE=Pause | M=Change Mode | 0-9=Select Child | E=Export CSV | C=Clear | R=Reconnect | UP/DOWN=Scale Adjust", 20, 120);
  
  if (!portConnected) {
    fill(200, 0, 0);
    textSize(16);
    textAlign(CENTER);
    text("ポートに接続できません", width/2, height/2);
    return;
  }
  
  // ★ メイングラフ表示
  int graphX = 20;
  int graphY = 135;
  int graphWidth = width - 40;
  int graphHeight = 500;
  
  drawGraph(graphX, graphY, graphWidth, graphHeight, currentDisplayMode);
  
  // ★ 統計情報表示
  int statsY = graphY + graphHeight + 20;
  drawStats(20, statsY);
}

void drawGraph(int x, int y, int w, int h, int mode) {
  if (dataHistory.size() < 2) {
    fill(150);
    textSize(12);
    textAlign(LEFT);
    text("Waiting for data...", x + 10, y + h/2);
    return;
  }
  
  // グラフ背景
  fill(50);
  rect(x, y, w, h);
  
  // グリッドライン
  stroke(80);
  strokeWeight(1);
  for (int i = 0; i <= 5; i++) {
    float gridY = y + (h / 5.0) * i;
    line(x, gridY, x + w, gridY);
  }
  
  // タイトル
  fill(255);
  textSize(12);
  textAlign(LEFT);
  text(displayModes[mode], x + 10, y - 5);
  
  // ★ 各表示モード別の描画
  switch(mode) {
    case 0: drawAccelerationMode(x, y, w, h); break;
    case 1: drawTotalAccelMode(x, y, w, h); break;
    case 2: drawShakeCountMode(x, y, w, h); break;
    case 3: drawDotProductMode(x, y, w, h); break;
    case 4: drawBaseVectorMode(x, y, w, h); break;
    case 5: drawDotProductHistoryMode(x, y, w, h); break;
  }
}

// モード0: AcX, AcY, AcZ の表示
void drawAccelerationMode(int x, int y, int w, int h) {
  float scale = SCALE_RANGES[0];
  float centerY = y + h / 2.0;
  
  // ★ シェイク状態の背景表示
  drawShakeStateBackground(x, y, w, h);
  
  // X軸（赤）
  stroke(255, 100, 100);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).acX, -scale, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).acX, -scale, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Y軸（緑）
  stroke(100, 255, 100);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).acY, -scale, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).acY, -scale, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Z軸（青）
  stroke(100, 100, 255);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).acZ, -scale, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).acZ, -scale, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // 凡例
  fill(255, 100, 100);
  text("X", x + 10, y + 20);
  fill(100, 255, 100);
  text("Y", x + 10, y + 40);
  fill(100, 100, 255);
  text("Z", x + 10, y + 60);
  
  // スケール情報
  fill(200);
  textSize(11);
  text("Scale: ±" + (int)scale, x + w - 150, y + 20);
}

// モード1: 合成加速度とジャーク
void drawTotalAccelMode(int x, int y, int w, int h) {
  float scale = SCALE_RANGES[1];
  float centerY = y + h / 2.0;
  
  // ★ シェイク状態の背景表示
  drawShakeStateBackground(x, y, w, h);
  
  // 合成加速度（黄色）
  stroke(255, 255, 100);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).totalAccel, 0, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).totalAccel, 0, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // 中央ライン
  stroke(100);
  line(x, centerY, x + w, centerY);
  
  // 凡例
  fill(255, 255, 100);
  text("Total Accel", x + 10, y + 20);
  
  fill(200);
  textSize(11);
  text("Scale: 0-" + (int)scale, x + w - 150, y + 20);
}

// モード2: シェイク検知数とシェイク状態
void drawShakeCountMode(int x, int y, int w, int h) {
  float centerY = y + h / 2.0;
  
  // シェイク状態表示（背景）
  for (int i = 0; i < dataHistory.size(); i++) {
    if (dataHistory.get(i).isShaking == 1) {
      float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
      float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
      fill(100, 100, 150, 100);
      noStroke();
      rect(x1, y, x2 - x1, h);
    }
  }
  
  // シェイクカウント（緑）
  stroke(100, 255, 100);
  strokeWeight(2);
  float maxShakeCount = 10;
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = y + h - map(dataHistory.get(i).shakeCount, 0, maxShakeCount, 0, h);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = y + h - map(dataHistory.get(i+1).shakeCount, 0, maxShakeCount, 0, h);
    line(x1, y1, x2, y2);
  }
  
  fill(100, 255, 100);
  text("Shake Count", x + 10, y + 20);
  fill(100, 100, 150);
  text("Shaking (blue bg)", x + 10, y + 40);
}

// モード3: ベクトル内積
void drawDotProductMode(int x, int y, int w, int h) {
  float scale = SCALE_RANGES[3];
  float centerY = y + h / 2.0;
  
  // ★ シェイク状態の背景表示
  drawShakeStateBackground(x, y, w, h);
  
  // 内積ライン
  stroke(200, 150, 255);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).dotProduct, -scale, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).dotProduct, -scale, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // ゼロラインと閾値ライン
  stroke(100);
  line(x, centerY, x + w, centerY);
  stroke(150, 100, 100);
  float thresholdY = centerY - map(0, -scale, scale, 0, h/2);  // DOT_PRODUCT_THRESHOLD = 0
  line(x, thresholdY, x + w, thresholdY);
  
  fill(200, 150, 255);
  text("Dot Product", x + 10, y + 20);
  fill(150, 100, 100);
  text("Threshold (red)", x + 10, y + 40);
  
  fill(200);
  textSize(11);
  text("Scale: ±" + (int)scale, x + w - 150, y + 20);
}

// モード4: 基準ベクトルコンポーネント
void drawBaseVectorMode(int x, int y, int w, int h) {
  float centerY = y + h / 2.0;
  
  // ★ シェイク状態の背景表示
  drawShakeStateBackground(x, y, w, h);
  
  // ★ スケーリング調整：実際のbaseVecの値に応じて自動調整
  float maxBaseVec = 20;  // デフォルトスケール
  for (FrameData data : dataHistory) {
    maxBaseVec = max(maxBaseVec, abs(data.baseVecX));
    maxBaseVec = max(maxBaseVec, abs(data.baseVecY));
    maxBaseVec = max(maxBaseVec, abs(data.baseVecZ));
  }
  maxBaseVec = ceil(maxBaseVec * 1.2);  // 余裕を持たせる
  
  // X成分（赤）
  stroke(255, 100, 100);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).baseVecX, -maxBaseVec, maxBaseVec, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).baseVecX, -maxBaseVec, maxBaseVec, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Y成分（緑）
  stroke(100, 255, 100);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).baseVecY, -maxBaseVec, maxBaseVec, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).baseVecY, -maxBaseVec, maxBaseVec, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Z成分（青）
  stroke(100, 100, 255);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).baseVecZ, -maxBaseVec, maxBaseVec, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).baseVecZ, -maxBaseVec, maxBaseVec, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  fill(255, 100, 100);
  text("X", x + 10, y + 20);
  fill(100, 255, 100);
  text("Y", x + 10, y + 40);
  fill(100, 100, 255);
  text("Z", x + 10, y + 60);
  
  fill(200);
  textSize(11);
  text("Scale: ±" + nf(maxBaseVec, 0, 1), x + w - 200, y + 20);
}

// モード5: 内積履歴の詳細表示（時間推移）
void drawDotProductHistoryMode(int x, int y, int w, int h) {
  float scale = SCALE_RANGES[5];
  float centerY = y + h / 2.0;
  
  // ★ シェイク状態の背景表示
  drawShakeStateBackground(x, y, w, h);
  
  // グリッドラインの閾値マーク
  stroke(100);
  line(x, centerY, x + w, centerY);  // ゼロライン
  
  // 閾値ライン（赤）- DOT_PRODUCT_THRESHOLD = 0の部分を強調
  stroke(150, 100, 100);
  strokeWeight(2);
  line(x, centerY, x + w, centerY);
  
  // 補助的な閾値ライン（-25000, 25000, 75000）
  stroke(80);
  strokeWeight(1);
  float thresholdLine1 = centerY - map(25000, -scale, scale, 0, h/2);
  float thresholdLine2 = centerY - map(75000, -scale, scale, 0, h/2);
  float thresholdLine3 = centerY - map(-25000, -scale, scale, 0, h/2);
  line(x, thresholdLine1, x + w, thresholdLine1);
  line(x, thresholdLine2, x + w, thresholdLine2);
  line(x, thresholdLine3, x + w, thresholdLine3);
  
  // ★ 内積ラインメイン表示（紫）
  stroke(200, 100, 255);
  strokeWeight(2);
  for (int i = 0; i < dataHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = centerY - map(dataHistory.get(i).dotProduct, -scale, scale, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = centerY - map(dataHistory.get(i+1).dotProduct, -scale, scale, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // ★ シェイク状態時の内積ポイントを円でマーク
  for (int i = 0; i < dataHistory.size(); i++) {
    if (dataHistory.get(i).isShaking == 1) {
      float px = x + map(i, 0, MAX_DATA_POINTS, 0, w);
      float py = centerY - map(dataHistory.get(i).dotProduct, -scale, scale, 0, h/2);
      
      fill(255, 150, 100);  // オレンジ色
      noStroke();
      circle(px, py, 4);
    }
  }
  
  // 凡例
  fill(200, 100, 255);
  text("Dot Product", x + 10, y + 20);
  fill(255, 150, 100);
  text("● During Shake", x + 10, y + 40);
  fill(150, 100, 100);
  text("Threshold (0)", x + 10, y + 60);
  
  fill(200);
  textSize(11);
  text("Scale: ±" + (int)scale, x + w - 150, y + 20);
  text("Negative→振り戻し判定", x + w - 200, y + 40);
}

// ★ ヘルパー関数：シェイク状態の背景を描画
void drawShakeStateBackground(int x, int y, int w, int h) {
  // シェイク状態時の背景をハイライト
  for (int i = 0; i < dataHistory.size(); i++) {
    if (dataHistory.get(i).isShaking == 1) {
      float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
      float x2 = x + map(i + 1, 0, MAX_DATA_POINTS, 0, w);
      
      fill(100, 150, 200, 60);  // 薄い青色の半透明
      noStroke();
      rect(x1, y, x2 - x1, h);
    }
  }
}

void drawStats(int x, int y) {
  if (dataHistory.size() == 0) return;
  
  FrameData latest = dataHistory.get(dataHistory.size() - 1);
  
  fill(255);
  textSize(11);
  textAlign(LEFT);
  
  text("Frame: " + latest.frameCount, x, y);
  text("AcX: " + latest.acX + "  AcY: " + latest.acY + "  AcZ: " + latest.acZ, x, y + 15);
  text("Total Accel: " + nf(latest.totalAccel, 0, 1), x + 400, y);
  text("Jerk: " + nf(latest.jerk, 0, 1), x + 400, y + 15);
  text("Shake Count: " + latest.shakeCount + "  State: " + (latest.isShaking == 1 ? "SHAKING" : "IDLE"), x + 800, y);
  text("Dot Product: " + latest.dotProduct, x + 800, y + 15);
  text("Base Vec: (" + latest.baseVecX + ", " + latest.baseVecY + ", " + latest.baseVecZ + ")", x, y + 35);
  
  // ★ dotProductを監視するための詳細表示
  if (latest.isShaking == 1) {
    fill(255, 150, 100);
    textSize(12);
    text(">> SHAKING | DotProduct: " + latest.dotProduct + " (Threshold: 0)", x, y + 55);
  }
}

void keyPressed() {
  if (key == ' ') {
    // スペースキー：一時停止
    isPaused = !isPaused;
  } 
  else if (key == 'm' || key == 'M') {
    // Mキー：表示モード変更
    currentDisplayMode = (currentDisplayMode + 1) % displayModes.length;
  } 
  else if (key >= '0' && key <= '9') {
    // 数字キー：子機ID選択
    selectedChildID = key - '0';
    dataHistory.clear();
  } 
  else if (key == 'e' || key == 'E') {
    // Eキー：CSV出力
    exportToCSV();
  } 
  else if (key == 'c' || key == 'C') {
    // Cキー：データクリア
    dataHistory.clear();
  } 
  else if (key == 'r' || key == 'R') {
    // Rキー：再接続
    connectToPort();
  }
}

void exportToCSV() {
  if (dataHistory.size() == 0) {
    println("No data to export!");
    return;
  }
  
  try {
    LocalDateTime now = LocalDateTime.now();
    DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss");
    String filename = "validation_" + now.format(formatter) + ".csv";
    
    FileWriter writer = new FileWriter(filename);
    
    // ★ CSVヘッダー
    writer.append("FrameCount,ChildID,AcX,AcY,AcZ,TotalAccel,Jerk,DotProduct,ShakeCount,IsShaking,BaseVecX,BaseVecY,BaseVecZ\n");
    
    // ★ データ行
    for (FrameData data : dataHistory) {
      writer.append(data.frameCount + ",");
      writer.append(data.childID + ",");
      writer.append(data.acX + ",");
      writer.append(data.acY + ",");
      writer.append(data.acZ + ",");
      writer.append(String.format("%.2f", data.totalAccel) + ",");
      writer.append(String.format("%.2f", data.jerk) + ",");
      writer.append(data.dotProduct + ",");
      writer.append(data.shakeCount + ",");
      writer.append(data.isShaking + ",");
      writer.append(data.baseVecX + ",");
      writer.append(data.baseVecY + ",");
      writer.append(data.baseVecZ + "\n");
    }
    
    writer.flush();
    writer.close();
    
    println("=== CSV Export Complete ===");
    println("Filename: " + filename);
    println("Total data points: " + dataHistory.size());
    println("File saved to: " + new java.io.File(".").getAbsolutePath());
    
  } catch (IOException e) {
    println("Export failed: " + e.getMessage());
  }
}
