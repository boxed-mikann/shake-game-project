using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// ========================================
/// InputQueueV2（Version2専用統一入力キュー）
/// ========================================
/// 
/// 責務：シリアル入力・キーボード入力を統一的に管理
/// 主機能：
/// - スレッドセーフなキュー（ConcurrentQueue）
/// - デバイスID解析機能
/// - AudioSettings.dspTime ベースのタイムスタンプ記録
/// 
/// 設計方針：
/// - dspTime統一（VideoPlayerの音声と同期、スレッドセーフ、高精度）
/// - デバイスID解析機能を内包
/// 
/// ========================================
/// </summary>
public static class InputQueueV2
{
    /// <summary>
    /// シェイク入力データ構造
    /// </summary>
    public struct ShakeInput
    {
        public string rawData;      // 生データ（デバッグ用）
        public string deviceId;     // デバイスID（0~9の数値文字列）
        public double timestamp;    // AudioSettings.dspTime での記録時刻
        public int count;           // シリアルのカウント値
        public float accel;         // 加速度（例: 453223.213）

        public ShakeInput(string rawData, string deviceId, double timestamp, int count, float accel)
        {
            this.rawData = rawData;
            this.deviceId = deviceId;
            this.timestamp = timestamp;
            this.count = count;
            this.accel = accel;
        }
    }

    private static ConcurrentQueue<ShakeInput> _queue = new ConcurrentQueue<ShakeInput>();

    /// <summary>
    /// 入力をキューに追加（AudioSettings.dspTime で記録）
    /// </summary>
    /// <param name="data">生データ（例: "0,62,453223.213"）</param>
    /// <param name="timestamp">AudioSettings.dspTime での記録時刻</param>
    public static void Enqueue(string data, double timestamp)
    {
        ParseSerialPayload(data, out string deviceId, out int count, out float accel);
        _queue.Enqueue(new ShakeInput(data, deviceId, timestamp, count, accel));

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[InputQueueV2] Enqueued: {data} | DeviceID: {deviceId} | Time: {timestamp:F3}s");
        }
    }

    /// <summary>
    /// キューから入力を取り出し
    /// </summary>
    public static bool TryDequeue(out ShakeInput input)
    {
        return _queue.TryDequeue(out input);
    }

    /// <summary>
    /// キューのクリア（ゲーム開始時等）
    /// </summary>
    public static void Clear()
    {
        while (_queue.TryDequeue(out _)) { }

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log("[InputQueueV2] Queue cleared");
        }
    }

    /// <summary>
    /// 現在のキューサイズ取得（デバッグ用）
    /// </summary>
    public static int Count => _queue.Count;

    /// <summary>
    /// データからデバイスIDを抽出
    /// フォーマット例：
    /// - "SHAKE:ESP01" → "ESP01"
    /// - "ESP_8266_01" → "ESP_8266_01"
    /// - "KEY1" → "KEY1"
    /// </summary>
    private static void ParseSerialPayload(string data, out string deviceId, out int count, out float accel)
    {
        deviceId = "UNKNOWN";
        count = 0;
        accel = 0f;

        if (string.IsNullOrEmpty(data)) return;

        string[] tokens = data.Trim().Split(',');
        // 期待フォーマット: ID,COUNT,ACCEL
        if (tokens.Length >= 3)
        {
            // ID は 0~9 の数値
            if (int.TryParse(tokens[0].Trim(), out int idNum))
            {
                deviceId = idNum.ToString();
            }
            // COUNT は整数
            int.TryParse(tokens[1].Trim(), out count);
            // ACCEL は浮動小数
            float.TryParse(tokens[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out accel);
            return;
        }

        // フォールバック: 先頭トークンのみ ID 数値として扱う試み
        if (tokens.Length >= 1 && int.TryParse(tokens[0].Trim(), out int idOnly))
        {
            deviceId = idOnly.ToString();
        }
    }
}
