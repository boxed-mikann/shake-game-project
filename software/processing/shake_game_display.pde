import processing.serial.*;

Serial myPort;
ArrayList<PlayerData> players = new ArrayList<PlayerData>();
final int MAX_PLAYERS = 20;

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
  
  // シリアルポート初期化
  String portName = Serial.list()[0]; // 最初のポートを使用
  myPort = new Serial(this, portName, 115200);
  myPort.bufferUntil('\n');
  
  // プレイヤーデータ初期化
  for (int i = 0; i < MAX_PLAYERS; i++) {
    players.add(new PlayerData(i));
  }
}

void draw() {
  background(240);
  
  // タイトル
  fill(0);
  textSize(24);
  text("Shake Game - Real-time Display", 20, 30);
  
  // グリッドで表示
  int cols = 10;
  int rows = 2;
  int boxWidth = width / cols;
  int boxHeight = (height - 50) / rows;
  
  for (int i = 0; i < MAX_PLAYERS; i++) {
    int row = i / cols;
    int col = i % cols;
    
    int x = col * boxWidth;
    int y = 50 + row * boxHeight;
    
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
  
  // スコア
  fill(0, 200, 0);
  textSize(16);
  textAlign(CENTER);
  text("Count: " + player.shakeCount, x + w/2, y + h/2);
  
  // 加速度
  fill(150);
  textSize(12);
  text("Accel: " + nf(player.acceleration, 0, 1), x + w/2, y + h - 15);
}

void serialEvent(Serial myPort) {
  String inString = myPort.readStringUntil('\n');
  
  if (inString != null) {
    inString = trim(inString);
    
    // データ形式: "CHILD_ID,shakeCount,acceleration"
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
}