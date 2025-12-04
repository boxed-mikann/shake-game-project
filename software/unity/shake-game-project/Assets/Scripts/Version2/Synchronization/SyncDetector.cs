using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シンクロ判定システム - スライディングウィンドウ方式でシンクロ人数を計算
/// デバイスごとの最新シェイクタイミングを管理し、更新時に同期判定を実施
/// </summary>
public class SyncDetector : MonoBehaviour
{
    public static SyncDetector Instance { get; private set; }
    
    public event Action<int> OnSyncDetected; // syncCount (同期人数)
    
    [Header("Settings")]
    [SerializeField] private float syncWindowSize = GameConstantsV2.SYNC_WINDOW_SIZE;
    [SerializeField] private int minSyncCountToDetect = 2; // 2人以上でシンクロ判定
    
    // デバイスIDごとの最新シェイクタイミングを保持
    private readonly Dictionary<string, double> deviceLatestShakeTimes = new Dictionary<string, double>();
    
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
    /// デバイスのシェイクタイミングを更新し、シンクロ判定を実施
    /// ShakeHandler等から呼び出される
    /// </summary>
    public int UpdateDeviceShakeTime(string deviceId, double timestamp)
    {
        if (string.IsNullOrEmpty(deviceId)) return 0;
        
        // デバイスの最新シェイクタイミングを更新
        deviceLatestShakeTimes[deviceId] = timestamp;
        
        // 同期判定を実施
        return CheckSync(timestamp);
    }
    
    /// <summary>
    /// 指定タイムスタンプから±syncWindowSize以内のシェイクを持つデバイス数を数える
    /// </summary>
    private int CheckSync(double currentTimestamp)
    {
        int syncCount = 0;
        
        foreach (var latestTime in deviceLatestShakeTimes.Values)
        {
            double timeDiff = Mathf.Abs((float)(currentTimestamp - latestTime));
            
            if (timeDiff <= syncWindowSize)
            {
                syncCount++;
            }
        }
        
        // 最小シンクロ人数以上の場合のみイベント発行
        if (syncCount >= minSyncCountToDetect)
        {
            OnSyncDetected?.Invoke(syncCount);
            
            if (GameConstantsV2.DEBUG_MODE)
            {
                Debug.Log($"[SyncDetector] シンクロ検出: Count={syncCount}, Time={currentTimestamp:F3}");
            }
        }
        return syncCount;
    }
    
    /// <summary>
    /// デバイスが登録解除されたときに呼び出す
    /// </summary>
    public void RemoveDevice(string deviceId)
    {
        if (deviceLatestShakeTimes.ContainsKey(deviceId))
        {
            deviceLatestShakeTimes.Remove(deviceId);
        }
    }
    
    /// <summary>
    /// 全デバイスのシェイク履歴をクリア
    /// </summary>
    public void ClearAllDevices()
    {
        deviceLatestShakeTimes.Clear();
    }
}
