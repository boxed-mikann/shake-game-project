using UnityEngine;

/// <summary>
/// IdleRegister 状態のシェイク処理
/// - デバイスの登録処理を実行
/// - 登録済みアイコンのエフェクト再生
/// </summary>
public class IdleRegisterHandler : ShakeHandlerBase
{
    [SerializeField] private DeviceIconManager idleIconManager;

    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[IdleRegisterHandler] ProcessRegistration for DeviceID={deviceId}");
        
        // デバイス登録処理
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.ProcessRegistration(deviceId, timestamp);
        }

        // 登録済みアイコンのエフェクト再生
        var icon = idleIconManager?.GetDeviceIcon(deviceId);
        icon?.OnShakeProcessed();
    }
}
