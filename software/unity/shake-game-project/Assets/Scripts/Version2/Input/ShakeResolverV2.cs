using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Version1のShakeResolverを参考にした統一シェイク処理システム
/// イベントではなく直接呼び出しでラグを削減
/// </summary>
public class ShakeResolverV2 : MonoBehaviour
{
    private static ConcurrentQueue<(string deviceId, double timestamp)> _sharedInputQueue 
        = new ConcurrentQueue<(string deviceId, double timestamp)>();
    
    [SerializeField] private DeviceIconManager idleIconManager;
    [SerializeField] private DeviceIconManager gamePlayIconManager;
    
    public static void EnqueueInput(string deviceId, double timestamp)
    {
        _sharedInputQueue.Enqueue((deviceId, timestamp));
    }
    
    void Update()
    {
        while (_sharedInputQueue.TryDequeue(out var input))
        {
            Debug.Log($"[ShakeResolverV2] Processing: DeviceID={input.deviceId} at {input.timestamp}s");
            ProcessShake(input.deviceId, input.timestamp);
        }
    }
    
    private void ProcessShake(string deviceId, double timestamp)
    {
        bool isGameStarted = GameManagerV2.Instance != null ;//&& GameManagerV2.Instance.IsGameStarted;
        Debug.Log($"[ShakeResolverV2] IsGameStarted={isGameStarted}");
        
        if (!isGameStarted)
        {
            // Idle時: デバイス登録処理
            Debug.Log($"[ShakeResolverV2] Calling DeviceManager.ProcessRegistration");
            if (DeviceManager.Instance != null)
            {
                DeviceManager.Instance.ProcessRegistration(deviceId, timestamp);
            }
            
            // 登録済みアイコンのエフェクト再生
            var icon = idleIconManager?.GetDeviceIcon(deviceId);
            icon?.OnShakeProcessed();
        }
        else
        {
            // ゲーム中: シェイク記録とシンクロ判定
            if (DeviceManager.Instance != null)
            {
                DeviceManager.Instance.RecordShake(deviceId, timestamp);
            }
            
            var icon = gamePlayIconManager?.GetDeviceIcon(deviceId);
            icon?.OnShakeProcessed();
            
            if (SyncDetector.Instance != null)
            {
                SyncDetector.Instance.OnShakeInput(deviceId, timestamp);
            }
        }
    }
}
