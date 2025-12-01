using UnityEngine;

/// <summary>
/// Game（ゲーム進行中）状態のシェイク処理
/// - シェイク記録処理
/// - 同期判定処理
/// - ゲーム中アイコンのエフェクト再生
/// </summary>
public class GamePlayHandler : ShakeHandlerBase
{
    [SerializeField] private DeviceIconManager gamePlayIconManager;

    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[GamePlayHandler] Processing game shake for DeviceID={deviceId}");
        
        // シェイク記録処理
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.RecordShake(deviceId, timestamp);
        }

        // ゲーム中アイコンのエフェクト再生
        var icon = gamePlayIconManager?.GetDeviceIcon(deviceId);
        icon?.OnShakeProcessed();

        // 同期判定処理
        if (SyncDetector.Instance != null)
        {
            SyncDetector.Instance.OnShakeInput(deviceId, timestamp);
        }
    }
}
