using UnityEngine;

/// <summary>
/// IdleStarting 状態のシェイク処理
/// - 登録済みデバイスのみを受け付ける
/// - 登録済みアイコンのエフェクト再生
/// （将来: 全員同時シェイク検知などを追加）
/// </summary>
public class IdleStartingHandler : ShakeHandlerBase
{
    [SerializeField] private DeviceIconManager idleIconManager;

    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[IdleStartingHandler] Processing shake for registered DeviceID={deviceId}");
        
        // 登録済みアイコンのエフェクト再生のみ
        var icon = idleIconManager?.GetDeviceIcon(deviceId);
        icon?.OnShakeProcessed();

        // 将来: 全員同時シェイク検知などの機能を追加する場合はここに記述
    }
}
