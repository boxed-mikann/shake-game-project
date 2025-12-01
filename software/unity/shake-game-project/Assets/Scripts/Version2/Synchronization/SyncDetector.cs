using System;
using UnityEngine;

/// <summary>
/// シンクロ判定システム - スライディングウィンドウ方式でシンクロ率を計算
/// Version1のShakeResolverと連携し、ラグの少ない判定を実現
/// </summary>
public class SyncDetector : MonoBehaviour
{
    public static SyncDetector Instance { get; private set; }
    
    public event Action<float, double> OnSyncDetected; // syncRate, timestamp
    
    [Header("Settings")]
    [SerializeField] private float syncWindowSize = GameConstantsV2.SYNC_WINDOW_SIZE;
    [SerializeField] private float minSyncRateToDetect = 0.5f; // 50%以上でシンクロ判定
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    
    /// <summary>
    /// シェイク入力時にシンクロ率を計算
    /// ShakeResolverV2から呼び出される
    /// </summary>
    public void OnShakeInput(string deviceId, double timestamp)
    {
        //if (!GameManagerV2.Instance.IsGameStarted) return;
        if (DeviceManager.Instance == null) return;
        
        float syncRate = CalculateSyncRate(timestamp);
        
        // 最小シンクロ率以上の場合のみイベント発行
        if (syncRate >= minSyncRateToDetect)
        {
            OnSyncDetected?.Invoke(syncRate, timestamp);
            
            if (GameConstantsV2.DEBUG_MODE)
            {
                Debug.Log($"[SyncDetector] シンクロ検出: Rate={syncRate:P0}, Time={timestamp:F3}");
            }
        }
    }
    
    private float CalculateSyncRate(double currentTimestamp)
    {
        int totalDevices = DeviceManager.Instance.GetDeviceCount();
        if (totalDevices == 0) return 0f;
        
        int syncCount = 0;
        
        foreach (var device in DeviceManager.Instance.GetRegisteredDevices())
        {
            double lastShakeTime = device.lastShakeTime;
            double timeDiff = Mathf.Abs((float)(currentTimestamp - lastShakeTime));
            
            if (timeDiff < syncWindowSize)
            {
                syncCount++;
            }
        }
        
        return (float)syncCount / totalDevices;
    }
}
