import processing.serial.*;
import java.io.FileWriter;
import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

Serial myPort;
ArrayList<Integer> acxHistory = new ArrayList<Integer>();
ArrayList<Integer> acyHistory = new ArrayList<Integer>();
ArrayList<Integer> aczHistory = new ArrayList<Integer>();
ArrayList<Float> totalAccelHistory = new ArrayList<Float>();

final int MAX_DATA_POINTS = 300;  // 6秒分のデータ（20Hz × 6s）
boolean portConnected = false;
String statusMessage = "Initializing...";

// 現在の値
int currentAcX = 0, currentAcY = 0, currentAcZ = 0;
float currentTotalAccel = 0;
float accelThreshold = 15000.0;

// ★ 一時停止機能
boolean isPaused = false;
int pausedDataCount = 0;

void setup() {
  size(1200, 800);
  
  println("=== Available Serial Ports ===");
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
  
  int portIndex = 0;
  
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

void draw() {
  background(30);
  
  // ヘッダー
  fill(255);
  textSize(20);
  textAlign(LEFT);
  text("Acceleration Diagnostic Plotter", 20, 30);
  
  // ステータス
  fill(portConnected ? color(0, 200, 0) : color(200, 0, 0));
  textSize(12);
  text("● " + statusMessage, 20, 50);
  
  // 現在値表示
  fill(255);
  textSize(14);
  text("AcX: " + currentAcX + "  AcY: " + currentAcY + "  AcZ: " + currentAcZ, 20, 75);
  text("Total Accel: " + nf(currentTotalAccel, 0, 2), 20, 95);
  
  // ★ 一時停止状態表示
  if (isPaused) {
    fill(255, 200, 0);  // 黄色
    textSize(14);
    text("[PAUSED] Data Points: " + pausedDataCount, 20, 115);
  } else {
    fill(200);
    textSize(11);
    text("Use +/- keys to adjust threshold (" + nf(accelThreshold, 0, 0) + ")", 20, 115);
  }
  
  // ★ コントロールガイド
  fill(150);
  textSize(11);
  text("SPACE: Pause/Resume | 'e': Export CSV | 'c': Clear | 'r': Reconnect", 20, 135);
  
  if (!portConnected) {
    fill(200, 0, 0);
    textSize(16);
    textAlign(CENTER);
    text("ポートに接続できません", width/2, height/2);
    return;
  }
  
  // グラフ領域
  int graphX = 20;
  int graphY = 160;
  int graphWidth = width - 40;
  int graphHeight = 250;
  
  // グラフ背景
  fill(50);
  rect(graphX, graphY, graphWidth, graphHeight);
  
  // グリッド線
  stroke(80);
  strokeWeight(1);
  for (int i = 0; i <= 5; i++) {
    int y = graphY + (graphHeight / 5) * i;
    line(graphX, y, graphX + graphWidth, y);
  }
  
  // グラフのタイトル
  fill(255);
  textSize(12);
  textAlign(LEFT);
  text("X (Red) | Y (Green) | Z (Blue) [Threshold: " + nf(accelThreshold, 0, 0) + "]", graphX, graphY - 5);
  
  // グラフを描画
  drawAccelerationGraphs(graphX, graphY, graphWidth, graphHeight);
  
  // 全体加速度グラフ
  int totalGraphY = graphY + graphHeight + 30;
  fill(50);
  rect(graphX, totalGraphY, graphWidth, graphHeight);
  
  stroke(200);
  line(graphX, totalGraphY + graphHeight/2, graphX + graphWidth, totalGraphY + graphHeight/2);  // 中央線
  
  // しきい値ライン
  stroke(150, 100, 100);
  strokeWeight(2);
  float thresholdY = totalGraphY + graphHeight/2 - map(accelThreshold, -30000, 30000, 0, graphHeight/2);
  line(graphX, thresholdY, graphX + graphWidth, thresholdY);
  
  fill(255);
  textSize(12);
  textAlign(LEFT);
  text("Total Acceleration (Threshold Line: Red)", graphX, totalGraphY - 5);
  
  drawTotalAccelerationGraph(graphX, totalGraphY, graphWidth, graphHeight);
  
  // 情報表示
  fill(200);
  textSize(11);
  textAlign(LEFT);
  text("Data points: " + acxHistory.size(), 20, height - 10);
}

void drawAccelerationGraphs(int x, int y, int w, int h) {
  if (acxHistory.size() < 2) return;
  
  // X軸（赤）
  stroke(255, 100, 100);
  strokeWeight(2);
  for (int i = 0; i < acxHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = y + h/2 - map(acxHistory.get(i), -30000, 30000, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = y + h/2 - map(acxHistory.get(i+1), -30000, 30000, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Y軸（緑）
  stroke(100, 255, 100);
  strokeWeight(2);
  for (int i = 0; i < acyHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = y + h/2 - map(acyHistory.get(i), -30000, 30000, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = y + h/2 - map(acyHistory.get(i+1), -30000, 30000, 0, h/2);
    line(x1, y1, x2, y2);
  }
  
  // Z軸（青）
  stroke(100, 100, 255);
  strokeWeight(2);
  for (int i = 0; i < aczHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = y + h/2 - map(aczHistory.get(i), -30000, 30000, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = y + h/2 - map(aczHistory.get(i+1), -30000, 30000, 0, h/2);
    line(x1, y1, x2, y2);
  }
}

void drawTotalAccelerationGraph(int x, int y, int w, int h) {
  if (totalAccelHistory.size() < 2) return;
  
  stroke(255, 200, 100);
  strokeWeight(2);
  for (int i = 0; i < totalAccelHistory.size() - 1; i++) {
    float x1 = x + map(i, 0, MAX_DATA_POINTS, 0, w);
    float y1 = y + h/2 - map(totalAccelHistory.get(i), 0, 30000, 0, h/2);
    float x2 = x + map(i+1, 0, MAX_DATA_POINTS, 0, w);
    float y2 = y + h/2 - map(totalAccelHistory.get(i+1), 0, 30000, 0, h/2);
    line(x1, y1, x2, y2);
  }
}

void serialEvent(Serial myPort) {
  if (!portConnected) return;
  
  try {
    String inString = myPort.readStringUntil('\n');
    
    if (inString != null) {
      inString = trim(inString);
      
      // 診断ヘッダーを無視
      if (inString.startsWith("===") || inString.startsWith("Format") || inString.startsWith("Sending")) {
        return;
      }
      
      // CSV形式: AcX,AcY,AcZ
      String[] parts = split(inString, ',');
      
      if (parts.length >= 3) {
        try {
          int acX = int(parts[0]);
          int acY = int(parts[1]);
          int acZ = int(parts[2]);
          
          // 現在値を保存
          currentAcX = acX;
          currentAcY = acY;
          currentAcZ = acZ;
          
          // 全体加速度を計算
          currentTotalAccel = sqrt(pow(acX, 2) + pow(acY, 2) + pow(acZ, 2));
          
          // ★ 一時停止中は新データを追加しない
          if (!isPaused) {
            // 履歴に追加
            acxHistory.add(acX);
            acyHistory.add(acY);
            aczHistory.add(acZ);
            totalAccelHistory.add(currentTotalAccel);
            
            // 最大データ数を超えたら古い方を削除
            if (acxHistory.size() > MAX_DATA_POINTS) {
              acxHistory.remove(0);
              acyHistory.remove(0);
              aczHistory.remove(0);
              totalAccelHistory.remove(0);
            }
          }
          
        } catch (Exception e) {
          println("Parse error: " + inString);
        }
      }
    }
  } catch (Exception e) {
    println("Serial error: " + e.getMessage());
    portConnected = false;
    statusMessage = "Serial connection lost!";
  }
}

// ★ CSV ファイルにエクスポート（修正版）
void exportDataToCSV() {
  if (acxHistory.size() == 0) {
    println("No data to export!");
    return;
  }
  
  try {
    // タイムスタンプ付きファイル名
    LocalDateTime now = LocalDateTime.now();
    DateTimeFormatter formatter = DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss");
    String filename = "acceleration_" + now.format(formatter) + ".csv";
    
    // ファイルパス（Processingのスケッチフォルダ内）
    String filepath = sketchPath(filename);
    
    // CSVを作成
    FileWriter writer = new FileWriter(filepath);
    
    // ヘッダー
    writer.append("Time(ms),AcX,AcY,AcZ,TotalAccel\n");
    
    // データ
    for (int i = 0; i < acxHistory.size(); i++) {
      long timeMs = i * 50;  // 50ms間隔
      
      // ★ すべてを String に変換してから append
      writer.append(String.valueOf(timeMs));
      writer.append(",");
      writer.append(String.valueOf(acxHistory.get(i)));
      writer.append(",");
      writer.append(String.valueOf(acyHistory.get(i)));
      writer.append(",");
      writer.append(String.valueOf(aczHistory.get(i)));
      writer.append(",");
      writer.append(String.valueOf(totalAccelHistory.get(i)));
      writer.append("\n");
    }
    
    writer.flush();
    writer.close();
    
    println("Data exported to: " + filepath);
    println("Exported " + acxHistory.size() + " data points");
    
  } catch (IOException e) {
    println("Export failed: " + e.getMessage());
  }
}

void keyPressed() {
  // ★ SPACE キーで一時停止/再開
  if (key == ' ') {
    isPaused = !isPaused;
    pausedDataCount = acxHistory.size();
    if (isPaused) {
      println("=== PAUSED ===");
      println("Data points captured: " + pausedDataCount);
    } else {
      println("=== RESUMED ===");
    }
  }
  
  // ★ 'e' キーでCSVエクスポート
  if (key == 'e' || key == 'E') {
    println("Exporting data to CSV...");
    exportDataToCSV();
  }
  
  // ★ 'c' キーでクリア
  if (key == 'c' || key == 'C') {
    acxHistory.clear();
    acyHistory.clear();
    aczHistory.clear();
    totalAccelHistory.clear();
    isPaused = false;
    println("Data cleared");
  }
  
  if (key == 'r' || key == 'R') {
    println("Reconnecting...");
    connectToPort();
  }
  
  // +/- キーでしきい値を調整
  if (key == '+' || key == '=') {
    accelThreshold += 500;
    println("Threshold: " + accelThreshold);
  }
  if (key == '-' || key == '_') {
    accelThreshold -= 500;
    accelThreshold = max(0, accelThreshold);
    println("Threshold: " + accelThreshold);
  }
}