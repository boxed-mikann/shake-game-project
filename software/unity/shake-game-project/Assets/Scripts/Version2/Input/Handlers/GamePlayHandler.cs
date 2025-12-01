using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Game（ゲーム進行中）状態のシェイク処理
/// - シェイク記録処理
/// - 同期判定処理
/// - ゲーム中アイコンのエフェクト再生
/// </summary>
public class GamePlayHandler : ShakeHandlerBase
{
    [SerializeField] private List<GameObject> deviceIcons = new List<GameObject>();

    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[GamePlayHandler] Processing game shake for DeviceID={deviceId}");
        
        // シェイク記録処理
        // if (DeviceManager.Instance != null)
        // {
        //     DeviceManager.Instance.RecordShake(deviceId, timestamp);
        // }

        // ゲーム中アイコンのエフェクト再生
        if (int.TryParse(deviceId, out int index) && index >= 0 && index < deviceIcons.Count)
        {
            var icon = deviceIcons[index];
            if (icon != null)
            {
                var deviceIcon = icon.GetComponent<DeviceIcon>();
                if (deviceIcon != null)
                {
                    Debug.Log($"[GamePlayHandler] Calling OnShakeProcessed for index={index}");
                    deviceIcon.OnShakeProcessed();
                }
                else
                {
                    Debug.LogWarning($"[GamePlayHandler] DeviceIcon component not found on icon");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[GamePlayHandler] No icon found for deviceId={deviceId}");
        }

        // // 同期判定処理
        // if (SyncDetector.Instance != null)
        // {
        //     SyncDetector.Instance.OnShakeInput(deviceId, timestamp);
        // }
    }
}
