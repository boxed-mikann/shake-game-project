import processing.serial.*;

Serial myPort;
ArrayList<PlayerData> players = new ArrayList<PlayerData>();
final int MAX_PLAYERS = 20;
boolean portConnected = false;
String statusMessage = "Initializing...";
int connectionAttempts = 0;
final int MAX_CONNECTION_ATTEMPTS = 5;

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

void setup() {
  size(1200, 600);
  
  // プレイヤーデータ初期化
  for (int i = 0; i < MAX_PLAYERS; i++) {
    players.add(new PlayerData(i));
  }
  
  // 使用可能なシリアルポート一覧を表示
  println("=== Available Serial Ports ===");
  String[] ports = Serial.list();
  for (int i = 0; i < ports.length; i++) {
    println("[" + i + "] " + ports[i]);
  }
  println("==============================");
  
  // ポート接続試行
  connectToPort();
}

void connectToPort() {
  String[] ports = Serial.list();
  
  if (ports.length == 0) {
    statusMessage = "ERROR: No serial ports found!";
    println(statusMessage);
    return;
  }
  
  // ユーザーが指定したポート（デフォルト: 最初のポート）
  // ここを変更して別のポートを試すことができます
  int portIndex = 0;  // ★ ここを変更: 0, 1, 2, ... など
  
  try {
    // 既に接続しているポートがあれば閉じる
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
      println("Please check:");
      println("1. Arduino IDE のシリアルモニタが閉じているか");
      println("2. 親機が USB で接続されているか");
      println("3. ボーレートが 115200 に設定されているか");
    }
  }
}

void draw() {
  background(240);
  
  // ヘッダー
  fill(0);
  textSize(24);
  textAlign(LEFT);
  text("Shake Game - Real-time Display", 20, 30);
  
  // ステータス表示
  fill(100);
  textSize(12);
  textAlign(LEFT);
  if (portConnected) {
    fill(0, 150, 0);  // 緑
    text("● " + statusMessage, 20, 50);
  } else {
    fill(200, 0, 0);  // 赤
    text("● " + statusMessage, 20, 50);
  }
  
  if (!portConnected) {
    // ポート未接続時の表示
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
  int boxHeight = (height - 80) / rows;
  
  for (int i = 0; i < MAX_PLAYERS; i++) {
    int row = i / cols;
    int col = i % cols;
    
    int x = col * boxWidth;
    int y = 70 + row * boxHeight;
    
    drawPlayerBox(x, y, boxWidth, boxHeight, players.get(i));
  }
}

void drawPlayerBox(int x, int y, int w, int h, PlayerData player) {
  // 背景
  stroke(100);
  fill(255);
  rect(x + 5, y + 5, w - 10, h - 10);
  
  // プレイヤーID
  fill(0);
  textSize(14);
  textAlign(LEFT);
  text("Player #" + player.id, x + 10, y + 20);
  
  // スコア（緑）
  fill(0, 200, 0);
  textSize(16);
  textAlign(CENTER);
  text("Count: " + player.shakeCount, x + w/2, y + h/2);
  
  // 加速度（灰色）
  fill(150);
  textSize(12);
  text("Accel: " + nf(player.acceleration, 0, 1), x + w/2, y + h - 15);
}

void serialEvent(Serial myPort) {
  if (!portConnected) return;
  
  try {
    String inString = myPort.readStringUntil('\n');
    
    if (inString != null) {
      inString = trim(inString);
      
      // ★ デバッグ行を無視
      if (inString.startsWith("Child #") || inString.startsWith("[DEBUG]")) {
        return;  // この行をスキップ
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
  // キーボード操作でポート再接続（'R'キー）
  if (key == 'r' || key == 'R') {
    println("Reconnecting...");
    connectionAttempts = 0;
    connectToPort();
  }
}