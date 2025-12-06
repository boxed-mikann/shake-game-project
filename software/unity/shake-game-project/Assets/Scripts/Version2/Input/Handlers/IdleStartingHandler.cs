using UnityEngine;

/// <summary>
/// IdleStarting 状態のシェイク処理
/// - 登録済みデバイスのみを受け付ける
/// - SyncDetectorでシンクロ検出
/// - 登録デバイス数に達したらゲーム開始
/// - 登録済みアイコンのエフェクト再生
/// </summary>
public class IdleStartingHandler : ShakeHandlerBase
{

    public override void HandleShake(string deviceId, double timestamp)
    {
        // 登録済みデバイスのみ処理
        if (DeviceRegisterManager.Instance == null || 
            !DeviceRegisterManager.Instance.IsDeviceRegistered(deviceId))
        {
            Debug.Log($"[IdleStartingHandler] Device {deviceId} is not registered, ignoring shake");
            return;
        }

        Debug.Log($"[IdleStartingHandler] Processing shake for registered DeviceID={deviceId}");
        
        // 効果音を再生
        if (SEManager.Instance != null)
        {
            if (int.TryParse(deviceId, out int id))
            {
                SEManager.Instance.PlayShakeHit(id);
            }
            else
            {
                SEManager.Instance.PlayShakeHit();
            }
        }
        
        // 登録済みアイコンのエフェクト再生
        if (IdleStartingDeviceIconManager.Instance != null)
        {
            IdleStartingDeviceIconManager.Instance.PlayShakeEffect(deviceId);
        }

        // SyncDetectorにシェイクタイミングを通知
        int syncCount = 0;
        if (SyncDetector.Instance != null)
        {
            syncCount = SyncDetector.Instance.UpdateDeviceShakeTime(deviceId, timestamp);
        }

        // 登録デバイス数に達したらゲーム開始
        int registeredDeviceCount = DeviceRegisterManager.Instance.RegisteredDeviceCount;
        if (syncCount >= registeredDeviceCount && registeredDeviceCount > 0)
        {
            Debug.Log($"[IdleStartingHandler] All registered devices synced ({syncCount}/{registeredDeviceCount}). Starting game!");
            if (GameManagerV2.Instance != null)
            {
                GameManagerV2.Instance.StartGame();
            }
        }
        else
        {
            Debug.Log($"[IdleStartingHandler] Sync count: {syncCount}/{registeredDeviceCount}");
        }
    }
}
