using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Game（ゲーム進行中）状態のシェイク処理
/// - 判定処理
/// - ゲーム中アイコンのエフェクト再生と判定表示
/// </summary>
public class GamePlayHandler : ShakeHandlerBase
{
    [SerializeField] private List<GameObject> deviceIcons = new List<GameObject>();

    public override void HandleShake(string deviceId, double timestamp)
    {
        Debug.Log($"[GamePlayHandler] Processing game shake for DeviceID={deviceId}");
        
        int syncCount = 0;
        // シンクロ検出システムにシェイクタイミングを通知
        if (SyncDetector.Instance != null)
        {
            syncCount = SyncDetector.Instance.UpdateDeviceShakeTime(deviceId, timestamp);
        }
        //voltage加算
        VoltageManager.Instance.SimpleAddVoltage(1 * syncCount);
        // 判定処理 - ゲーム開始からの相対時間を計算
        double relativeTime = timestamp; // TODO: VideoManager.GetCurrentTime() を使用
        JudgeManagerV2.JudgementType judgement = JudgeManagerV2.Instance.Judge(deviceId, relativeTime);

        // ゲーム中アイコンのエフェクト再生と判定表示
        if (int.TryParse(deviceId, out int index) && index >= 0 && index < deviceIcons.Count)
        {
            var icon = deviceIcons[index];
            if (icon != null)
            {
                var deviceIcon = icon.GetComponent<DeviceIcon>();
                if (deviceIcon != null)
                {
                    Debug.Log($"[GamePlayHandler] Calling OnShakeProcessed for index={index}, judgement={judgement}");
                    deviceIcon.OnShakeProcessed(judgement);
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
    }
}
