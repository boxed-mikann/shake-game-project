/// <summary>
/// ゲーム全体の定数管理
/// </summary>
public static class GameConstants
{
    // ===== Serial Communication =====
    public const string SERIAL_PORT_NAME = "COM3";
    public const int SERIAL_BAUD_RATE = 115200;
    public const float SERIAL_RECONNECT_INTERVAL = 5f;   // シリアルポート再接続の間隔（秒）
    
    // ===== Game Settings =====
    public const float GAME_DURATION = 60f;              // ゲーム制限時間（秒）
    public const float NOTE_PHASE_DURATION = 10f;        // 音符フェーズ初期時間（秒）
    public const float REST_PHASE_DURATION = 5f;         // 休符フェーズ初期時間（秒）
    public const float PHASE_DURATION_MIN = 2f;          // フェーズの最小時間（秒）
    public const float PHASE_SHORTENING_RATE = 0.85f;    // フェーズ時間の短縮倍率（毎回x0.85）
    public const int SPAWN_RATE_BASE = 5;                // 初期湧き出し数/秒
    public const float LAST_SPRINT_DURATION = 10f;       // ラストスパート期間（秒）
    public const float LAST_SPRINT_MULTIPLIER = 2f;      // ラストスパート時の倍率（湧き出し2倍、スコア2倍）
    
    // ===== Scoring =====
    public const int NOTE_SCORE = 100;                  // 音符をはじけた時のスコア
    public const int REST_PENALTY = -50;                // 休符をはじけた時のペナルティ
    public const int PERFECT_BONUS = 500;               // 完璧プレイボーナス（休符ノーミス）
    
    // ===== Visuals =====
    public const float FREEZE_DURATION = 0.5f;          // フリーズ時間（秒）
    public const float FREEZE_TIME_SCALE = 0.2f;        // フリーズ時のTime.timeScale倍率
    public const int MAX_NOTE_COUNT = 100;              // 同時生成上限の音符数
    public const float INPUT_LOCK_DURATION = 3f;        // フリーズ中の入力無視時間（秒）
    public const float NOTE_ROTATION_MAX = 30f;         // 生成時の回転範囲（±度）
    
    // ===== Debug =====
    public const bool DEBUG_MODE = true;                // デバッグモード（キーボード入力有効化）
    public const bool USE_KEYBOARD_INPUT = true;        // キーボード入力の使用
}