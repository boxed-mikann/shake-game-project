using System;
using System.Collections.Generic;
using UnityEngine;

public class DeviceManager : MonoBehaviour
{
    public static DeviceManager Instance { get; private set; }

    public event Action<string> OnDeviceRegistered;
    public event Action<int> OnRegisteredCountChanged;

    [Serializable]
    public class DeviceInfo
    {
        public string deviceId;
        public double lastShakeTime;   // dspTime
        public int consecutiveShakes;  // 登録用カウント
        public bool registered;        // 登録完了フラグ
    }

    [Header("Registration Settings")]
    [SerializeField] private int shakesNeededToRegister = 10;
    [SerializeField] private float simultaneousWindowSeconds = 0.2f; // 200ms
    [SerializeField] private bool debugAllowSingleToStart = false;

    private readonly Dictionary<string, DeviceInfo> devices = new Dictionary<string, DeviceInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public IEnumerable<DeviceInfo> GetRegisteredDevices()
    {
        foreach (var d in devices.Values)
        {
            if (d.registered) yield return d;
        }
    }

    public int GetDeviceCount()
    {
        int c = 0;
        foreach (var d in devices.Values) if (d.registered) c++;
        return c;
    }

    public void ProcessRegistration(string deviceId, double timestamp)
    {
        Debug.Log($"[DeviceManager] ProcessRegistration: DeviceID={deviceId}");
        if (string.IsNullOrEmpty(deviceId)) return;

        if (!devices.TryGetValue(deviceId, out var info))
        {
            info = new DeviceInfo { deviceId = deviceId, lastShakeTime = timestamp, consecutiveShakes = 1, registered = false };
            devices[deviceId] = info;
            Debug.Log($"[DeviceManager] New device: {deviceId}, Shake count: {info.consecutiveShakes}/{shakesNeededToRegister}");
        }
        else
        {
            info.lastShakeTime = timestamp;
            info.consecutiveShakes++;
            Debug.Log($"[DeviceManager] Existing device: {deviceId}, Shake count: {info.consecutiveShakes}/{shakesNeededToRegister}");
        }

        if (!info.registered && info.consecutiveShakes >= shakesNeededToRegister)
        {
            // 登録完了（以降他IDは無視される想定）
            info.registered = true;
            Debug.Log($"[DeviceManager] Device REGISTERED: {deviceId}");
            OnDeviceRegistered?.Invoke(deviceId);
            OnRegisteredCountChanged?.Invoke(GetDeviceCount());
        }
    }

    public void RecordShake(string deviceId, double timestamp)
    {
        if (!devices.TryGetValue(deviceId, out var info))
        {
            // 未知IDは登録処理に回す
            ProcessRegistration(deviceId, timestamp);
            return;
        }
        if (!info.registered)
        {
            // 未登録は登録用カウントのみ増やす
            ProcessRegistration(deviceId, timestamp);
            return;
        }
        info.lastShakeTime = timestamp;
    }

    public bool CheckSimultaneousShakeToStart()
    {
        int count = GetDeviceCount();
        if (count == 0) return false;
        if (debugAllowSingleToStart && count >= 1) return true;
        if (count < 2) return false;

        double now = AudioSettings.dspTime;
        int withinWindow = 0;
        foreach (var d in devices.Values)
        {
            if (!d.registered) continue;
            double relativeShake = d.lastShakeTime; // absolute dspTime
            if (Mathf.Abs((float)(now - relativeShake)) < simultaneousWindowSeconds)
            {
                withinWindow++;
            }
        }

        return withinWindow == count; // 全員がウィンドウ内
    }

    public void ResetRegistrations()
    {
        devices.Clear();
        OnRegisteredCountChanged?.Invoke(0);
    }
}
