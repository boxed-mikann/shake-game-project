/// <summary>
/// ========================================
/// GameConstants（ゲーム全体の定数管理）
/// ========================================
/// 
/// ◎ 配置区分
///   - Phase Configuration：フェーズ定義（PHASE_SEQUENCE）
///   - Serial Communication：シリアル通信設定
///   - Game Settings：ゲーム進行設定（時間、生成速度）
///   - Scoring：スコアシステム（加点、ペナルティ）
///   - Freeze：フリーズエフェクト（フェーズとは独立）
///   - Visuals：ビジュアル設定（最大数、回転範囲）
///   - Debug：デバッグ設定
/// 
/// ◎ 設定方針
///   - ゲーム調整時はこのファイルの定数を変更する
///   - GameManager や UIManager では定数を直接参照（ハードコーディング禁止）
///   - 段階的な調整に対応（データ駆動設計）
/// 
/// ========================================
/// </summary>
public static class GameConstants
{
    // ===== Phase Configuration =====
    /// <summary>
    /// フェーズ設定構造体
    /// </summary>
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
    ///   - GameManager.InitializePhaseSequence() で展開して _phaseSegments を構築
    ///   - PHASE_SEQUENCE のすべての duration 合計が実質的な GAME_DURATION
    ///   - LastSprintPhase を明示的に配列に含める（タイミング調整が簡単）
    /// 
    /// 特性：
    ///   - 要素を順番にループしてフェーズを埋める
    ///   - ゲーム調整時はここで各フェーズの継続時間を変更
    ///   - LastSprintPhase を配列内に含めることで、PHASE_SEQUENCE だけで完全な構成
    /// 
    /// 例：PHASE_SEQUENCE = [10s Note, 5s Rest, ..., 15s LastSprint] で合計=65s
    ///   GameManager が展開：
    ///     _phaseSegments = [
    ///       (Note, 0, 10), (Rest, 10, 5),
    ///       (Note, 15, 10), (Rest, 25, 5),
    ///       ...
    ///       (LastSprint, 50, 15)
    ///     ]
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
    /// これにより、PHASE_SEQUENCE を編集すると自動的に GAME_DURATION が更新される
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
    
    // ===== Scoring =====
    public const int NOTE_SCORE = 1;                  // 音符をはじけた時のスコア
    public const int REST_PENALTY = 0;                // 休符をはじけた時のペナルティ
    public const int PERFECT_BONUS = 50;               // 完璧プレイボーナス（休符ノーミス）
    
    // ===== Freeze（フリーズ - フェーズとは別管理） =====
    /// <summary>
    /// 入力ロック時間（秒）
    /// 用途：Rest フェーズで音符を叩いた時のペナルティ
    /// </summary>
    public const float INPUT_LOCK_DURATION = 3f;
    
    // ===== Visuals =====
    public const int MAX_NOTE_COUNT = 100;              // 同時生成上限の音符数
    public const float NOTE_ROTATION_MAX = 30f;         // 生成時の回転範囲（±度）
    
    // ===== Debug =====
    public const bool DEBUG_MODE = true;                // デバッグモード（キーボード入力有効化）
    public const bool USE_KEYBOARD_INPUT = true;        // キーボード入力の使用
}