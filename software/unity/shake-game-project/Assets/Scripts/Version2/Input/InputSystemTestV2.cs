using UnityEngine;

/// <summary>
/// ========================================
/// InputSystemTestV2（Phase 1-1 動作確認用）
/// ========================================
/// 
/// 責務：入力システムのデバッグ・動作確認
/// 主機能：
/// - InputQueueV2 からの入力取得
/// - タイムスタンプ検証
/// - デバッグログ出力
/// 
/// 使い方：
/// 1. 空のGameObjectにアタッチ
/// 2. SerialInputReaderV2 と KeyboardInputReaderV2 を別のGameObjectにアタッチ
/// 3. SerialPortManagerV2 をシーンに配置
/// 4. スペースキーまたは数字キーで入力をシミュレート
/// 5. コンソールログで動作確認
/// 
/// ========================================
/// </summary>
public class InputSystemTestV2 : MonoBehaviour
{
    [Header("統計情報")]
    [SerializeField] private int totalInputs = 0;
    [SerializeField] private double lastInputTime = 0.0;
    [SerializeField] private string lastDeviceId = "";
    
    [Header("デバッグ表示")]
    [SerializeField] private bool showDetailedLog = true;
    
    void Update()
    {
        // InputQueueV2 から入力を取得
        while (InputQueueV2.TryDequeue(out var input))
        {
            totalInputs++;
            lastInputTime = input.timestamp;
            lastDeviceId = input.deviceId;
            
            // 現在時刻との差分を計算（ラグ確認）
            double currentTime = AudioSettings.dspTime;
            double lag = currentTime - input.timestamp;
            
            if (showDetailedLog)
            {
                Debug.Log($"[InputSystemTest] Input #{totalInputs} | Device: {input.deviceId} | " +
                          $"dspTime: {input.timestamp:F3}s | Current: {currentTime:F3}s | Lag: {lag * 1000:F1}ms");
            }
            
            // 異常なラグを警告
            if (lag > 0.1f)
            {
                Debug.LogWarning($"[InputSystemTest] High lag detected: {lag * 1000:F0}ms");
            }
        }
    }
    
    void OnGUI()
    {
        // 画面上部に統計情報を表示
        GUILayout.BeginArea(new Rect(10, 10, 400, 150));
        GUILayout.Label($"=== Input System Test V2 (dspTime) ===");
        GUILayout.Label($"Total Inputs: {totalInputs}");
        GUILayout.Label($"Last Device: {lastDeviceId}");
        GUILayout.Label($"Last dspTime: {lastInputTime:F3}s");
        GUILayout.Label($"Current dspTime: {AudioSettings.dspTime:F3}s");
        GUILayout.Label($"Queue Size: {InputQueueV2.Count}");
        GUILayout.Label("");
        GUILayout.Label("Controls:");
        GUILayout.Label("- Space/0: Simulate ID=0");
        GUILayout.Label("- 1~9: Simulate ID=1~9");
        GUILayout.EndArea();
    }
}
