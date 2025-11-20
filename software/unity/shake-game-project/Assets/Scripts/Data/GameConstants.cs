/// <summary>
/// ========================================
/// GameConstants（ゲーム全体の定数管理）
/// ========================================
/// 
/// 新アーキテクチャ版
/// 既存の GameConstants.cs から定数値を継承しつつ、
/// 新しいイベント駆動設計に対応
/// 
/// ◎ 配置区分
///   - Phase Configuration：フェーズ定義（PHASE_SEQUENCE）
///   - Serial Communication：シリアル通信設定
///   - Game Settings：ゲーム進行設定（時間、生成速度）
///   - Scoring：スコアシステム（加点、ペナルティ）
///   - Freeze：フリーズエフェクト
///   - Visuals：ビジュアル設定（最大数、回転範囲）
///   - Debug：デバッグ設定
/// 
/// ========================================
/// </summary>
public static class GameConstants
{
    // ===== Phase Configuration =====
    /// <summary>
    /// フェーズ設定構造体
    /// </summary>
    [System.Serializable]
    public struct PhaseConfig
    {
        public Phase phase;
        public float duration;
        
        public override string ToString()
        {
            return $"{phase} ({duration}s)";
        }
    }
    
    /// <summary>
    /// フェーズシーケンス設定（配列型）
    /// 
    /// 用途：
    ///   - PhaseManager が展開して順次実行
    ///   - PHASE_SEQUENCE のすべての duration 合計が実質的な GAME_DURATION
    ///   - LastSprintPhase を明示的に配列に含める
    /// 
    /// 例：PHASE_SEQUENCE = [10s Note, 5s Rest, ..., 15s LastSprint] で合計=65s
    /// </summary>
    public static readonly PhaseConfig[] PHASE_SEQUENCE = new[]
    {
        new PhaseConfig { phase = Phase.NotePhase, duration = 10f },
        new PhaseConfig { phase = Phase.RestPhase, duration = 5f },
        new PhaseConfig { phase = Phase.NotePhase, duration = 10f },
        new PhaseConfig { phase = Phase.RestPhase, duration = 5f },
        new PhaseConfig { phase = Phase.NotePhase, duration = 10f },
        new PhaseConfig { phase = Phase.RestPhase, duration = 5f },
        new PhaseConfig { phase = Phase.LastSprintPhase, duration = 15f }
    };
    
    /// <summary>
    /// ゲーム制限時間（秒）
    /// PHASE_SEQUENCE のすべての duration を合計して動的に計算
    /// </summary>
    public static float GAME_DURATION
    {
        get
        {
            float total = 0f;
            foreach (var config in PHASE_SEQUENCE)
            {
                total += config.duration;
            }
            return total;
        }
    }
    
    // ===== Serial Communication =====
    public const string SERIAL_PORT_NAME = "COM3";
    public const int SERIAL_BAUD_RATE = 115200;
    public const float SERIAL_RECONNECT_INTERVAL = 5f;   // シリアルポート再接続の間隔（秒）
    
    // ===== Game Settings =====
    /// <summary>初期湧き出し数 (/秒)</summary>
    public const int SPAWN_RATE_BASE = 5;
    
    /// <summary>ラストスパント時の倍率（湧き出し × 2）</summary>
    public const float LAST_SPRINT_MULTIPLIER = 2f;
    
    /// <summary>
    /// 基本音符湧き出し頻度（秒間隔）
    /// spawnFrequency = 1 / SPAWN_RATE_BASE
    /// </summary>
    public static float BASE_SPAWN_FREQUENCY
    {
        get { return 1f / SPAWN_RATE_BASE; }
    }
    
    // ===== Scoring =====
    public const int NOTE_SCORE = 1;                  // 通常音符のスコア
    public const int LAST_SPRINT_SCORE = 2;           // ラストスパート時のスコア
    public const int REST_PENALTY = -1;               // 休符ペナルティ
    public const int PERFECT_BONUS = 50;              // 完璧プレイボーナス（休符ノーミス）
    
    // ===== High Score =====
    public const string HIGH_SCORE_KEY = "HighScore"; // PlayerPrefs用のハイスコアキー
    
    // ===== Freeze（フリーズ - フェーズとは別管理） =====
    /// <summary>
    /// 入力ロック時間（秒）
    /// 用途：Rest フェーズで音符を叩いた時のペナルティ
    /// </summary>
    public const float INPUT_LOCK_DURATION = 3f;
    
    // ===== Visuals =====
    public const int MAX_NOTE_COUNT = 100;              // 同時生成上限の音符数
    public const float NOTE_ROTATION_MAX = 30f;         // 生成時の回転範囲（±度）
    
    /// <summary>
    /// 音符生成範囲のマージン（画面サイズに対する比率）
    /// 0.9f = 画面サイズの90%以内に生成
    /// </summary>
    public const float NOTE_SPAWN_MARGIN = 0.9f;
    
    // ===== Effects =====
    public const int EFFECT_POOL_INITIAL_SIZE = 50;     // エフェクトプールの初期サイズ
    
    // ===== Debug =====
    public const bool DEBUG_MODE = true;                // デバッグモード（キーボード入力有効化）
    public const bool USE_KEYBOARD_INPUT = true;        // キーボード入力の使用
}