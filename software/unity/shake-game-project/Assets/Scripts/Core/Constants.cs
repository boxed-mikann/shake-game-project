using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体の定数を一元管理
/// </summary>
public static class Constants
{
    // ゲーム設定
    public const float MAX_SCORE = 100f;
    public const float ACCELERATION_TO_SCORE_DIVISOR = 10000f;
    
    // Serial通信
    public const int SERIAL_BAUD_RATE = 115200;
    public const int SERIAL_READ_TIMEOUT = 500;
    
    // フェーズタイマー
    public const float VICTORY_DISPLAY_TIME = 3f;
    public const float RESULT_DISPLAY_TIME = 5f;
}
