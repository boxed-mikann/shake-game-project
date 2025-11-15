/// <summary>
/// ゲーム全体の定数管理
/// </summary>
public static class GameConstants
{
    // ===== Serial Communication =====
    public const string SERIAL_PORT_NAME = "COM3";
    public const int SERIAL_BAUD_RATE = 115200;
    
    // ===== Game Settings =====
    public const float GAME_DURATION = 60f;              // ゲーム制限時間（秒）
    public const float PHASE_DURATION = 10f;             // 各フェーズの継続時間（秒）
    public const int SPAWN_RATE_BASE = 5;               // 初期湧き出し数/秒
    public const float LAST_SPRINT_DURATION = 10f;      // ラストスパート期間（秒）
    public const float LAST_SPRINT_MULTIPLIER = 2f;     // ラストスパート時の倍率（湧き出し2倍、スコア2倍）
    
    // ===== Scoring =====
    public const int NOTE_SCORE = 100;                  // 音符をはじけた時のスコア
    public const int REST_PENALTY = -50;                // 休符をはじけた時のペナルティ
    public const int PERFECT_BONUS = 500;               // 完璧プレイボーナス（休符ノーミス）
    
    // ===== Visuals =====
    public const float FREEZE_DURATION = 0.5f;          // フリーズ時間（秒）
    public const float FREEZE_TIME_SCALE = 0.2f;        // フリーズ時のTime.timeScale倍率
    
    // ===== Debug =====
    public const bool DEBUG_MODE = true;                // デバッグモード（キーボード入力有効化）
    public const bool USE_KEYBOARD_INPUT = true;        // キーボード入力の使用
}