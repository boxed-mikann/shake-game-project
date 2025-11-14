/// <summary>
/// ゲーム全体で使用する列挙型定義
/// </summary>

/// <summary>
/// ゲーム全体の状態
/// </summary>
public enum GameState
{
    Title,      // タイトル画面
    Loading,    // ロード中
    Running,    // ゲーム実行中
    Paused,     // ポーズ中
    Victory,    // 勝利
    Defeat,     // 敗北
    Result      // 結果画面
}

/// <summary>
/// ゲーム内フェーズ
/// </summary>
public enum GamePhase
{
    Practice,           // 練習フェーズ（0-5秒）
    BossHighDamage,     // ボス戦フェーズ（高ダメージ期）（5-20秒）
    BossSyncRequired    // ボス戦フェーズ（同期期）（20-30秒）
}

/// <summary>
/// タイミングイベントの種類
/// </summary>
public enum TimingType
{
    Normal,         // 通常のシェイク検知
    Sync,           // 同期が必要なタイミング
    BossAttack      // ボスが攻撃するタイミング
}

/// <summary>
/// ボスの状態
/// </summary>
public enum BossState
{
    Idle,           // 待機中
    Attacking,      // 攻撃中
    TakingDamage,   // ダメージ受け中
    Dead            // 倒された
}

/// <summary>
/// ダメージタイプ
/// </summary>
public enum DamageType
{
    Normal,         // 通常ダメージ
    Sync,           // 同期ボーナスダメージ
    Boss            // ボス攻撃
}

/// <summary>
/// シェイク検知の感度レベル
/// </summary>
public enum SensitivityLevel
{
    Low,
    Medium,
    High
}
