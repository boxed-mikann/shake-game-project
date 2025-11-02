import processing.serial.*;

Serial myPort;
ArrayList<PlayerData> players = new ArrayList<PlayerData>();
final int MAX_PLAYERS = 20;
boolean portConnected = false;
String statusMessage = "Initializing...";
int connectionAttempts = 0;
final int MAX_CONNECTION_ATTEMPTS = 5;

// ★ UI ボタン
Button onButton;
Button offButton;
boolean shakeDetectionActive = true;

class PlayerData {
  int id;
  int shakeCount;
  float acceleration;
  
  PlayerData(int id) {
    this.id = id;
    this.shakeCount = 0;
    this.acceleration = 0;
  }
}

// ★ ボタンクラス
class Button {
  int x, y, w, h;
  String label;
  color bgColor, hoverColor;
  boolean isHovered = false;
  
  Button(int x, int y, int w, int h, String label, color bgColor, color hoverColor) {
    this.x = x;
    this.y = y;
    this.w = w;
    this.h = h;
    this.label = label;
    this.bgColor = bgColor;
    this.hoverColor = hoverColor;
  }
  
  void display() {
    // マウスホバー判定
    isHovered = mouseX > x && mouseX < x + w && mouseY > y && mouseY < y + h;
    
    // ボタン描画
    fill(isHovered ? hoverColor : bgColor);
    stroke(0);
    strokeWeight(2);
    rect(x, y, w, h, 5);
    
    // ラベル
    fill(255);
    textSize(14);
    textAlign(CENTER, CENTER);
    text(label, x + w/2, y + h/2);
  }
  
  boolean isClicked() {
    return isHovered && mousePressed;
  }
}

void setup() {
  size(1200, 650);
  
  // プレイヤーデータ初期化
  for (int i = 0; i < MAX_PLAYERS; i++) {
    players.add(new PlayerData(i));
  }
  
  // ★ ボタン初期化
  onButton = new Button(50, 60, 100, 40, "ON", color(0, 200, 0), color(0, 255, 0));
  offButton = new Button(160, 60, 100, 40, "OFF", color(200, 0, 0), color(255, 0, 0));
  
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
    connectionAttempts = 0;
    
  } catch (Exception e) {
    portConnected = false;
    connectionAttempts++;
    statusMessage = "Connection failed (Attempt " + connectionAttempts + "/" + MAX_CONNECTION_ATTEMPTS + ")";
    println(statusMessage);
    println("Error: " + e.getMessage());
    
    if (connectionAttempts < MAX_CONNECTION_ATTEMPTS) {
      println("Retrying in 3 seconds...");
      delay(3000);
      connectToPort();
    } else {
      statusMessage = "ERROR: Could not connect after " + MAX_CONNECTION_ATTEMPTS + " attempts";
      println(statusMessage);
    }
  }
}

void draw() {
  background(240);
  
  // ★ ボタン表示
  onButton.display();
  offButton.display();
  
  // ボタンクリック判定
  if (onButton.isClicked()) {
    sendCommand("ON");
    shakeDetectionActive = true;
    delay(200);  // チャタリング防止
  }
  if (offButton.isClicked()) {
    sendCommand("OFF");
    shakeDetectionActive = false;
    delay(200);
  }
  
  // ヘッダー
  fill(0);
  textSize(24);
  textAlign(LEFT);
  text("Shake Game - Real-time Display", 350, 80);
  
  // ステータス表示
  fill(100);
  textSize(12);
  textAlign(LEFT);
  if (portConnected) {
    fill(0, 150, 0);
    text("● " + statusMessage, 350, 100);
  } else {
    fill(200, 0, 0);
    text("● " + statusMessage, 350, 100);
  }
  
  // ★ 計測状態表示
  textSize(14);
  fill(shakeDetectionActive ? color(0, 150, 0) : color(150, 0, 0));
  text("Detection: " + (shakeDetectionActive ? "ON" : "OFF"), 350, 125);
  
  if (!portConnected) {
    fill(200, 0, 0);
    textSize(16);
    textAlign(CENTER);
    text("ポートに接続できません", width/2, height/2 - 20);
    text("Arduino IDE のシリアルモニタを閉じてください", width/2, height/2 + 20);
    return;
  }
  
  // グリッドで表示
  int cols = 10;
  int rows = 2;
  int boxWidth = width / cols;
  int boxHeight = (height - 140) / rows;
  
  for (int i = 0; i < MAX_PLAYERS; i++) {
    int row = i / cols;
    int col = i % cols;
    
    int x = col * boxWidth;
    int y = 140 + row * boxHeight;
    
    drawPlayerBox(x, y, boxWidth, boxHeight, players.get(i));
  }
}

void drawPlayerBox(int x, int y, int w, int h, PlayerData player) {
  stroke(100);
  fill(255);
  rect(x + 5, y + 5, w - 10, h - 10);
  
  fill(0);
  textSize(14);
  textAlign(LEFT);
  text("Player #" + player.id, x + 10, y + 20);
  
  fill(0, 200, 0);
  textSize(16);
  textAlign(CENTER);
  text("Count: " + player.shakeCount, x + w/2, y + h/2);
  
  fill(150);
  textSize(12);
  text("Accel: " + nf(player.acceleration, 0, 1), x + w/2, y + h - 15);
}

// ★ コマンド送信関数
void sendCommand(String command) {
  if (!portConnected) {
    println("Port not connected!");
    return;
  }
  
  myPort.write(command + "\n");
  println("Sent command: " + command);
}

void serialEvent(Serial myPort) {
  if (!portConnected) return;
  
  try {
    String inString = myPort.readStringUntil('\n');
    
    if (inString != null) {
      inString = trim(inString);
      
      // デバッグ行を無視
      if (inString.startsWith("Child #") || inString.startsWith("DEBUG") || inString.startsWith("Shake") || inString.startsWith("ESP-NOW")) {
        return;
      }
      
      // ★ コマンド応答を処理
      if (inString.startsWith("CMD")) {
        println("Command response received: " + inString);
        return;
      }
      
      // CSV形式のデータ処理
      String[] parts = split(inString, ',');
      
      if (parts.length >= 3) {
        try {
          int childID = int(parts[0]);
          int shakeCount = int(parts[1]);
          float acceleration = float(parts[2]);
          
          if (childID >= 0 && childID < MAX_PLAYERS) {
            players.get(childID).shakeCount = shakeCount;
            players.get(childID).acceleration = acceleration;
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

void keyPressed() {
  if (key == 'r' || key == 'R') {
    println("Reconnecting...");
    connectionAttempts = 0;
    connectToPort();
  }
}