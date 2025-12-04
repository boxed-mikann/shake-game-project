using UnityEngine;

public static class GameConstantsV2
{
    // タイミング・同期（dspTime 方針に準拠）
    public const float SYNC_WINDOW_SIZE = 0.20f; // 200ms

    // ボルテージ・スコア
    public const float BASE_VOLTAGE = 5f;
    public const float VOLTAGE_MAX = 100f;

    // 動画（StreamingAssets 相対パス）
    public const string VIDEO_RELATIVE_PATH = "Videos/UTA Live.mp4";

    // シリアル通信（SerialPortManagerV2 で使用）
    public const string SERIAL_PORT_NAME = "COM3";
    public const int SERIAL_BAUD_RATE = 115200;
    public const float SERIAL_RECONNECT_INTERVAL = 5f;

    // デバッグ
    public const bool DEBUG_MODE = true;
}
