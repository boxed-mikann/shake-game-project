using UnityEngine;

/// <summary>
/// IdleRegister 状態のシェイク処理
/// - DeviceRegisterManagerを使用したデバイス登録処理
/// - 登録済みアイコンの透明度制御とエフェクト再生
/// - SE再生
/// </summary>
public class IdleRegisterHandler : ShakeHandlerBase
{
    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[IdleRegisterHandler] Processing registration shake for DeviceID={deviceId}");
        
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
        
        // DeviceRegisterManagerに登録処理を委譲
        if (DeviceRegisterManager.Instance != null)
        {
            DeviceRegisterManager.Instance.ProcessShake(deviceId, timestamp);
        }
        else
        {
            Debug.LogWarning("[IdleRegisterHandler] DeviceRegisterManager instance not found!");
        }
    }
}
