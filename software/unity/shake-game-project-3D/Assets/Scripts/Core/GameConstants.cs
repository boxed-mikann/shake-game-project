using UnityEngine;

/// <summary>
/// ゲーム全体で使用する定数管理
/// </summary>
public static class GameConstants
{
    // ===== ゲーム時間設定 =====
    public const float TOTAL_GAME_TIME = 30f;           // ゲーム総時間（秒）
    public const float PRACTICE_PHASE_DURATION = 5f;    // 練習フェーズ時間（秒）
    public const float HIGH_DAMAGE_PHASE_DURATION = 15f; // 高ダメージ期の時間（秒）
    public const float SYNC_PHASE_DURATION = 10f;       // 同期期の時間（秒）
    
    // ===== ボス設定 =====
    public const float BOSS_MAX_HP = 1000f;             // ボスの最大HP
    public const float NORMAL_DAMAGE_PER_SHAKE = 5f;    // 1回のシェイク当たりの通常ダメージ
    public const float SYNC_BONUS_MULTIPLIER = 3f;      // 同期時のダメージ倍率
    public const float BOSS_ATTACK_DAMAGE = 50f;        // ボス攻撃のダメージ
    
    // ===== タイミングシステム =====
    public const float MUSIC_BPM = 120f;                // BGM のビート数
    public const float BEAT_DURATION = 60f / MUSIC_BPM; // 1ビートの長さ（秒）
    public const float TIMING_WINDOW = 0.3f;            // タイミング判定窓（秒）
    public const float SYNC_WINDOW = 0.5f;              // 同期判定窓（秒）
    
    // ===== シェイク検知 =====
    public const int SHAKE_ACCELERATION_THRESHOLD = 1000; // 加速度閾値
    public const int MAX_PLAYERS = 10;                   // 最大プレイヤー数
    
    // ===== SerialPort通信 =====
    public const string SERIAL_PORT_NAME = "COM3";      // デフォルトCOMポート（設定画面で変更可）
    public const int SERIAL_BAUD_RATE = 115200;         // ボーレート
    public const string SERIAL_LINE_ENDING = "\n";      // 行末文字
    
    // ===== UI配置 =====
    public const float UI_FADE_DURATION = 0.5f;         // UIフェード時間（秒）
    public const float DAMAGE_POPUP_DURATION = 2f;      // ダメージポップアップの表示時間（秒）
    public const float DAMAGE_POPUP_RISE_SPEED = 50f;   // ダメージポップアップの上昇速度
    
    // ===== スコア =====
    public const int SCORE_PER_SHAKE = 10;              // シェイク1回当たりのスコア
    public const int SCORE_PER_SYNC = 100;              // 同期成功1回当たりのスコア
    public const int SCORE_VICTORY_BONUS = 1000;        // 勝利ボーナス
    
    // ===== デバッグ =====
    public const bool DEBUG_MODE = true;                // デバッグモード有効化
    public const bool USE_KEYBOARD_INPUT = true;        // キーボード入力でシェイク信号を模擬
}
