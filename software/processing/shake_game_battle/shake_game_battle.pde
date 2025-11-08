import processing.serial.*;
import processing.video.*;
import ddf.minim.*;
import ddf.minim.ugens.*;

Serial myPort;
ArrayList<PlayerData> players = new ArrayList<PlayerData>();
final int MAX_PLAYERS = 20;
boolean portConnected = false;
String statusMessage = "Initializing...";
int connectionAttempts = 0;
final int MAX_CONNECTION_ATTEMPTS = 5;

// ゲーム状態管理
final int STATE_IDLE = 0;
final int STATE_PLAYING = 1;
final int STATE_WIN = 2;

final int PHASE_CHARGE = 1;
final int PHASE_RESIST = 2;

int gameState = STATE_IDLE;
int currentPhase = PHASE_CHARGE;
float phaseStartTime = 0;
final float PHASE_DURATION = 10000;  // 10秒
float[] teamGauge = {0, 0};  // Team A(0), Team B(1)
final float GOAL_GAUGE = 100.0;

// ゲージ計算パラメータ
final float ACCEL_THRESHOLD = 5000;
final float GAUGE_COEFFICIENT = 0.00005;

// フェーズ変更通知
int phaseNotificationTime = 0;
final int PHASE_NOTIFICATION_DURATION = 1500;

// 勝利表示
int winTeam = -1;
int winStartTime = 0;
final int WIN_SCREEN_DURATION = 5000;

// ★ ビデオ関連
Movie gameplayVideo;
Movie victoryVideo;
boolean videosLoaded = false;
String videoErrorMessage = "";

// ★ Minim オーディオ
Minim minim;
AudioOutput out;

class PlayerData {
  int id;
  int shakeCount;
  float acceleration;
  
  PlayerData(int id) {
    this.id = id;
    this.shakeCount = 0;
    this.acceleration = 0;
  }
  
  int getTeamID() {
    return id < 10 ? 0 : 1;
  }
}

void setup() {
  size(1600, 900);
  
  // ★ Minim 初期化
  minim = new Minim(this);
  out = minim.getLineOut();
  
  // ★ ビデオ読み込み
  loadVideos();
  
  // フォント設定（メイリオ）
  PFont meiryoFont = createFont("Meiryo", 16);
  textFont(meiryoFont);
  
  println("=== Available Serial Ports ===");
  String[] ports = Serial.list();
  for (int i = 0; i < ports.length; i++) {
    println("[" + i + "] " + ports[i]);
  }
  println("==============================");
  
  // プレイヤーデータ初期化
  for (int i = 0; i < MAX_PLAYERS; i++) {
    players.add(new PlayerData(i));
  }
  
  connectToPort();
}

// ★ ビデオ読み込み関数
void loadVideos() {
  try {
    // プレイ中の動画
    if (new File(dataPath("gameplay.mp4")).exists()) {
      gameplayVideo = new Movie(this, "gameplay.mp4");
      gameplayVideo.loop();
      println("gameplay.mp4 loaded");
    } else {
      videoErrorMessage += "gameplay.mp4 not found! ";
      println("WARNING: gameplay.mp4 not found in data folder");
    }
    
    // 勝利時の動画
    if (new File(dataPath("victory.mp4")).exists()) {
      victoryVideo = new Movie(this, "victory.mp4");
      println("victory.mp4 loaded");
    } else {
      videoErrorMessage += "victory.mp4 not found! ";
      println("WARNING: victory.mp4 not found in data folder");
    }
    
    if (gameplayVideo != null && victoryVideo != null) {
      videosLoaded = true;
      println("All videos loaded successfully");
    }
  } catch (Exception e) {
    videoErrorMessage = "Video load error: " + e.getMessage();
    println(videoErrorMessage);
  }
}

// ★ ビデオフレーム更新コールバック
void movieEvent(Movie m) {
  m.read();
}

void connectToPort() {
  String[] ports = Serial.list();
  
  if (ports.length == 0) {
    statusMessage = "ERROR: ポートが見つかりません";
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
    statusMessage = "接続完了";
    println(statusMessage);
    connectionAttempts = 0;
    gameState = STATE_PLAYING;
    phaseStartTime = millis();
    phaseNotificationTime = millis();
    
  } catch (Exception e) {
    portConnected = false;
    connectionAttempts++;
    statusMessage = "接続失敗";
    println(statusMessage);
    
    if (connectionAttempts < MAX_CONNECTION_ATTEMPTS) {
      println("再試行中...");
      delay(3000);
      connectToPort();
    }
  }
}

void draw() {
  background(10, 15, 30);
  
  if (!portConnected) {
    drawConnectionError();
    return;
  }
  
  // ゲーム状態管理
  updateGameState();
  
  // ★ 背景ビデオ描画（最背面）
  if (videosLoaded && gameplayVideo != null && gameState == STATE_PLAYING) {
    drawGameplayVideo();
  } else {
    drawGameBackground();
  }
  
  // ゲージ描画
  drawGauges();
  
  // フェーズ情報
  drawPhaseInfo();
  
  // フェーズ変更通知
  drawPhaseNotification();
  
  // プレイヤー情報
  drawPlayerInfo();
  
  // 状態表示
  drawStatus();
  
  // 勝利画面
  if (gameState == STATE_WIN) {
    drawWinScreen();
  }
}

// ★ プレイ中ビデオ描画
// ★ V11 の drawGameplayVideo() を置き換え

void drawGameplayVideo() {
  // フレームを読む（毎フレーム）
  if (gameplayVideo.available()) {
    gameplayVideo.read();
  }
  
  // ★ 最適化：スケール描画（set() より高速）
  pushMatrix();
  imageMode(CORNER);
  image(gameplayVideo, 0, 0, width, height);
  popMatrix();
  
  // グラデーション暗転
  fill(0, 0, 0, 80);
  rect(0, 0, width, height);
}


void updateGameState() {
  if (gameState != STATE_PLAYING) return;
  
  float elapsedTime = millis() - phaseStartTime;
  
  if (elapsedTime > PHASE_DURATION) {
    switchPhase();
  }
  
  // 勝利条件チェック
  if (teamGauge[0] >= GOAL_GAUGE) {
    gameState = STATE_WIN;
    winTeam = 0;
    winStartTime = millis();
    println("チームAが勝利!");
    playWinSound();
    
    // ★ 勝利動画開始
    if (videosLoaded && victoryVideo != null) {
      victoryVideo.play();
      victoryVideo.jump(0);
    }
  } else if (teamGauge[1] >= GOAL_GAUGE) {
    gameState = STATE_WIN;
    winTeam = 1;
    winStartTime = millis();
    println("チームBが勝利!");
    playWinSound();
    
    // ★ 勝利動画開始
    if (videosLoaded && victoryVideo != null) {
      victoryVideo.play();
      victoryVideo.jump(0);
    }
  }
}

void switchPhase() {
  if (currentPhase == PHASE_CHARGE) {
    currentPhase = PHASE_RESIST;
    println("フェーズ切り替え: 抵抗フェーズへ");
  } else {
    currentPhase = PHASE_CHARGE;
    println("フェーズ切り替え: チャージフェーズへ");
  }
  phaseStartTime = millis();
  phaseNotificationTime = millis();
}

void drawConnectionError() {
  fill(200, 0, 0);
  textSize(32);
  textAlign(CENTER);
  text("ポート接続エラー", width/2, height/2 - 20);
  textSize(20);
  text("USBケーブルを確認してください", width/2, height/2 + 30);
}

void drawGameBackground() {
  for (int i = 0; i < height; i++) {
    float inter = map(i, 0, height, 0, 1);
    int c = lerpColor(color(10, 15, 30), color(30, 20, 50), inter);
    stroke(c);
    strokeWeight(1);
    line(0, i, width, i);
  }
}

void drawGauges() {
  int centerX = width / 2;
  int gaugeY = 150;
  int gaugeWidth = 80;
  int gaugeHeight = 400;
  int gaugeDistance = 320;
  
  drawTeamGauge(centerX - gaugeDistance, gaugeY, gaugeWidth, gaugeHeight, 0, color(255, 80, 80));
  drawTeamGauge(centerX + gaugeDistance, gaugeY, gaugeWidth, gaugeHeight, 1, color(80, 150, 255));
}

void drawTeamGauge(int x, int y, int w, int h, int teamID, color gaugeColor) {
  float gauge = teamGauge[teamID];
  float ratio = gauge / GOAL_GAUGE;
  int filledHeight = (int)(h * ratio);
  
  stroke(100, 100, 150);
  strokeWeight(4);
  fill(20, 20, 40);
  rect(x, y, w, h);
  
  fill(gaugeColor);
  rect(x, y + h - filledHeight, w, filledHeight);
  
  PFont meiryoFont = createFont("Meiryo", 36);
  textFont(meiryoFont);
  fill(255, 255, 200);
  textSize(36);
  textAlign(CENTER);
  text(nf(gauge, 0, 1), x + w/2, y + h + 50);
  
  fill(gaugeColor);
  textSize(24);
  text(teamID == 0 ? "チームA" : "チームB", x + w/2, y - 25);
}

void drawPhaseInfo() {
  fill(0, 0, 0, 220);
  rect(0, 0, width, 130);
  
  PFont meiryoFont = createFont("Meiryo", 54);
  textFont(meiryoFont);
  fill(255);
  textSize(54);
  textAlign(CENTER);
  
  if (currentPhase == PHASE_CHARGE) {
    fill(100, 255, 100);
    text("フェーズ1: チャージ!", width/2, 65);
    fill(150, 255, 150);
    textSize(24);
    textFont(createFont("Meiryo", 24));
    text("振ってゲージを増やそう!", width/2, 100);
  } else {
    fill(255, 100, 100);
    text("フェーズ2: 危険!", width/2, 65);
    fill(255, 150, 150);
    textSize(24);
    textFont(createFont("Meiryo", 24));
    text("振るとゲージが減少します!", width/2, 100);
  }
  
  float elapsedTime = millis() - phaseStartTime;
  float remainingTime = max(0, PHASE_DURATION - elapsedTime) / 1000.0;
  fill(255, 255, 150);
  textSize(28);
  textAlign(RIGHT);
  textFont(createFont("Meiryo", 28));
  text(nf(remainingTime, 0, 1) + "秒", width - 40, 70);
}

void drawPhaseNotification() {
  float elapsed = millis() - phaseNotificationTime;
  
  if (elapsed < PHASE_NOTIFICATION_DURATION) {
    float alpha = map(elapsed, 0, PHASE_NOTIFICATION_DURATION, 255, 0);
    
    fill(0, 0, 0, alpha * 0.8);
    rect(0, 0, width, height);
    
    PFont meiryoFont = createFont("Meiryo", 72);
    textFont(meiryoFont);
    fill(255, 255, 100, alpha);
    textSize(72);
    textAlign(CENTER);
    
    if (currentPhase == PHASE_CHARGE) {
      text("チャージフェーズ開始!", width/2, height/2);
    } else {
      text("抵抗フェーズ開始!", width/2, height/2);
    }
  }
}

void drawPlayerInfo() {
  int cols = 10;
  int rows = 2;
  int boxWidth = width / cols;
  int boxHeight = 120;
  
  for (int i = 0; i < MAX_PLAYERS; i++) {
    int row = i / cols;
    int col = i % cols;
    
    int x = col * boxWidth;
    int y = height - (2 - row) * boxHeight;
    
    drawPlayerBox(x, y, boxWidth, boxHeight, players.get(i));
  }
}

void drawPlayerBox(int x, int y, int w, int h, PlayerData player) {
  color teamColor = player.getTeamID() == 0 ? color(255, 100, 100) : color(100, 150, 255);
  
  stroke(teamColor);
  strokeWeight(2);
  fill(20, 20, 40);
  rect(x + 3, y + 3, w - 6, h - 6);
  
  PFont meiryoFont = createFont("Meiryo", 14);
  textFont(meiryoFont);
  fill(teamColor);
  textSize(14);
  textAlign(CENTER);
  text("P" + player.id, x + w/2, y + 20);
  
  fill(255, 255, 100);
  textSize(22);
  textFont(createFont("Meiryo", 22));
  text(player.shakeCount, x + w/2, y + h/2 + 5);
  
  fill(150);
  textSize(10);
  textFont(createFont("Meiryo", 10));
  text(nf(player.acceleration, 0, 0), x + w/2, y + h - 10);
}

void drawStatus() {
  PFont meiryoFont = createFont("Meiryo", 12);
  textFont(meiryoFont);
  fill(100);
  textSize(12);
  textAlign(LEFT);
  if (portConnected) {
    fill(0, 200, 0);
    text("ステータス: " + statusMessage, 10, 20);
  } else {
    fill(200, 0, 0);
    text("ステータス: " + statusMessage, 10, 20);
  }
  
  // ★ ビデオロード状態表示
  if (!videosLoaded && videoErrorMessage.length() > 0) {
    fill(255, 150, 0);
    text("警告: " + videoErrorMessage, 10, 35);
  } else if (videosLoaded) {
    fill(0, 200, 0);
    text("動画: OK", 10, 35);
  }
}

// ★ 勝利画面（ビデオ背景付き）
void drawWinScreen() {
  float elapsedWinTime = millis() - winStartTime;
  
  // ★ 勝利ビデオを背景として表示
  if (videosLoaded && victoryVideo != null) {
    if (victoryVideo.available()) {
      victoryVideo.read();
    }
    image(victoryVideo, 0, 0, width, height);
  }
  
  // フェード背景
  float bgAlpha = map(elapsedWinTime, 0, 1000, 0, 180);
  fill(0, 0, 0, bgAlpha);
  rect(0, 0, width, height);
  
  // メインテキスト
  float textAlpha = map(elapsedWinTime, 0, 500, 0, 255);
  PFont meiryoFont = createFont("Meiryo", 96);
  textFont(meiryoFont);
  
  fill(255, 255, 100, textAlpha);
  textSize(96);
  textAlign(CENTER);
  
  if (winTeam == 0) {
    text("チームAの勝利!", width/2, height/2 - 80);
  } else {
    text("チームBの勝利!", width/2, height/2 - 80);
  }
  
  textSize(48);
  textFont(createFont("Meiryo", 48));
  text("おめでとうございます!", width/2, height/2 + 80);
  
  if (elapsedWinTime > WIN_SCREEN_DURATION) {
    resetGame();
  }
}

void resetGame() {
  println("ゲームをリセット");
  gameState = STATE_PLAYING;
  teamGauge[0] = 0;
  teamGauge[1] = 0;
  winTeam = -1;
  currentPhase = PHASE_CHARGE;
  phaseStartTime = millis();
  phaseNotificationTime = millis();
  
  // ★ 勝利動画を停止
  if (videosLoaded && victoryVideo != null) {
    victoryVideo.stop();
  }
}

void serialEvent(Serial myPort) {
  if (!portConnected) return;
  
  try {
    String inString = myPort.readStringUntil('\n');
    
    if (inString != null) {
      inString = trim(inString);
      
      if (inString.startsWith("Child #") || inString.startsWith("DEBUG") || 
          inString.startsWith("Shake") || inString.startsWith("ESP-NOW") ||
          inString.startsWith("Parent") || inString.startsWith("Ready") ||
          inString.startsWith("===") || inString.startsWith("Jerk") ||
          inString.startsWith("Accel") || inString.startsWith("Initialized") ||
          inString.startsWith(">>>") || inString.startsWith("フェーズ") ||
          inString.startsWith("Reset")) {
        return;
      }
      
      if (inString.startsWith("CMD")) {
        return;
      }
      
      String[] parts = split(inString, ',');
      
      if (parts.length >= 3) {
        try {
          int childID = int(parts[0]);
          int shakeCount = int(parts[1]);
          float acceleration = float(parts[2]);
          
          if (childID >= 0 && childID < MAX_PLAYERS) {
            PlayerData player = players.get(childID);
            
            if (shakeCount > player.shakeCount && gameState == STATE_PLAYING) {
              int teamID = player.getTeamID();
              
              float gaugeIncrease = acceleration * GAUGE_COEFFICIENT;
              if (currentPhase == PHASE_RESIST) {
                gaugeIncrease = -gaugeIncrease;
              }
              
              teamGauge[teamID] += gaugeIncrease;
              teamGauge[teamID] = constrain(teamGauge[teamID], 0, GOAL_GAUGE);
              
              println("チーム" + (teamID == 0 ? "A" : "B") + " - Accel: " + acceleration + " - Gauge増加: " + gaugeIncrease);
              
              if (currentPhase == PHASE_CHARGE) {
                playCoinSound();
              } else {
                playPenaltySound();
              }
            }
            
            player.shakeCount = shakeCount;
            player.acceleration = acceleration;
          }
        } catch (Exception e) {
          println("解析エラー: " + inString);
        }
      }
    }
  } catch (Exception e) {
    println("シリアルエラー: " + e.getMessage());
    portConnected = false;
    statusMessage = "接続切断";
  }
}

// オーディオ生成関数
void playCoinSound() {
  thread("playCoinSoundThread");
}

void playCoinSoundThread() {
  try {
    playToneWithMinim(600, 80);
    delay(50);
    playToneWithMinim(1000, 120);
  } catch (Exception e) {
  }
}

void playPenaltySound() {
  thread("playPenaltySoundThread");
}

void playPenaltySoundThread() {
  try {
    playToneWithMinim(800, 80);
    delay(100);
    playToneWithMinim(400, 120);
  } catch (Exception e) {
  }
}

void playWinSound() {
  thread("playWinSoundThread");
}

void playWinSoundThread() {
  try {
    playToneWithMinim(800, 100);
    delay(150);
    playToneWithMinim(1000, 100);
    delay(150);
    playToneWithMinim(1200, 200);
  } catch (Exception e) {
  }
}

void playToneWithMinim(float frequency, int duration) {
  Oscillator osc = new Oscillator(frequency, 0.3, Oscillator.SINE);
  osc.patch(out);
  
  delay(duration);
  
  osc.unpatch(out);
}

class Oscillator extends UGen {
  float frequency;
  float amplitude;
  int waveform;
  float phase = 0;
  
  final static int SINE = 0;
  final static int SQUARE = 1;
  final static int SAW = 2;
  
  Oscillator(float frequency, float amplitude, int waveform) {
    this.frequency = frequency;
    this.amplitude = amplitude;
    this.waveform = waveform;
  }
  
  protected void uGenerate(float[] channels) {
    for (int i = 0; i < channels.length; i++) {
      phase += TWO_PI * frequency / sampleRate();
      if (phase > TWO_PI) {
        phase -= TWO_PI;
      }
      channels[i] = sin(phase) * amplitude;
    }
  }
}

void stop() {
  out.close();
  minim.stop();
  super.stop();
}

void keyPressed() {
  if (key == 'r' || key == 'R') {
    println("再接続中...");
    connectionAttempts = 0;
    connectToPort();
  }
  
  if (key == ' ') {
    switchPhase();
  }
}
