using System.Collections;
using UnityEngine;

/// <summary>
/// ========================================
/// IdleStartingDeviceIconManager
/// ========================================
/// 
/// 責務：IdleStarting中のデバイスアイコン表示管理
/// - IdleStarting開始時に登録済みデバイスのアイコンのみ表示
/// - 未登録デバイスのアイコンを非表示
/// 
/// イベント駆動：
/// - GameManagerV2.OnIdleStart を購読
/// ========================================
/// </summary>
public class IdleStartingDeviceIconManager : MonoBehaviour
{
    public static IdleStartingDeviceIconManager Instance { get; private set; }

    [Header("IdleStarting Device Icons")]
    [SerializeField] private GameObject[] idleStartingDeviceIcons = new GameObject[10]; // IdleStarting中のデバイスアイコン（インスペクターでアタッチ）

    private bool subscribedToGameManager = false;

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

    private void OnEnable()
    {
        // 遅延購読でAwake順序に依存しないようにする
        StartCoroutine(EnsureSubscriptions());
    }

    private void OnDisable()
    {
        if (subscribedToGameManager && GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
        }
        subscribedToGameManager = false;
    }

    private IEnumerator EnsureSubscriptions()
    {
        float timeout = 5f;
        float elapsed = 0f;

        // GameManagerV2の購読
        while (!subscribedToGameManager && elapsed < timeout)
        {
            if (GameManagerV2.Instance != null)
            {
                GameManagerV2.Instance.OnIdleStart += OnIdleStart;
                subscribedToGameManager = true;
                Debug.Log("[IdleStartingDeviceIconManager] Subscribed to GameManagerV2.OnIdleStart");
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!subscribedToGameManager)
        {
            Debug.LogWarning("[IdleStartingDeviceIconManager] Failed to subscribe to GameManagerV2.OnIdleStart within timeout");
        }
    }

    /// <summary>
    /// IdleStarting開始時に呼ばれる
    /// 登録済みデバイスのアイコンのみ表示し、未登録デバイスのアイコンを非表示にする
    /// </summary>
    private void OnIdleStart()
    {
        Debug.Log("[IdleStartingDeviceIconManager] OnIdleStart called - showing only registered device icons");

        if (DeviceRegisterManager.Instance == null)
        {
            Debug.LogWarning("[IdleStartingDeviceIconManager] DeviceRegisterManager instance not found!");
            return;
        }

        // 各デバイスアイコンをチェック
        for (int i = 0; i < idleStartingDeviceIcons.Length; i++)
        {
            if (idleStartingDeviceIcons[i] != null)
            {
                string deviceId = i.ToString();
                bool isRegistered = DeviceRegisterManager.Instance.IsDeviceRegistered(deviceId);

                // 登録済みデバイスのみ表示
                idleStartingDeviceIcons[i].SetActive(isRegistered);

                if (isRegistered)
                {
                    Debug.Log($"[IdleStartingDeviceIconManager] Device {deviceId} is registered - showing icon");
                }
                else
                {
                    Debug.Log($"[IdleStartingDeviceIconManager] Device {deviceId} is NOT registered - hiding icon");
                }
            }
        }

        Debug.Log($"[IdleStartingDeviceIconManager] Total registered devices: {DeviceRegisterManager.Instance.RegisteredDeviceCount}");
    }

    /// <summary>
    /// 指定したデバイスIDのアイコンを取得
    /// </summary>
    public GameObject GetDeviceIcon(string deviceId)
    {
        if (int.TryParse(deviceId, out int index) && index >= 0 && index < idleStartingDeviceIcons.Length)
        {
            return idleStartingDeviceIcons[index];
        }
        return null;
    }

    /// <summary>
    /// すべてのアイコンを表示（デバッグ用）
    /// </summary>
    public void ShowAllIcons()
    {
        foreach (var icon in idleStartingDeviceIcons)
        {
            if (icon != null)
            {
                icon.SetActive(true);
            }
        }
        Debug.Log("[IdleStartingDeviceIconManager] All icons shown");
    }

    /// <summary>
    /// すべてのアイコンを非表示（デバッグ用）
    /// </summary>
    public void HideAllIcons()
    {
        foreach (var icon in idleStartingDeviceIcons)
        {
            if (icon != null)
            {
                icon.SetActive(false);
            }
        }
        Debug.Log("[IdleStartingDeviceIconManager] All icons hidden");
    }

    /// <summary>
    /// 指定したデバイスIDのシェイクエフェクトを再生
    /// </summary>
    public void PlayShakeEffect(string deviceId)
    {
        if (int.TryParse(deviceId, out int index) && index >= 0 && index < idleStartingDeviceIcons.Length)
        {
            if (idleStartingDeviceIcons[index] != null)
            {
                // ParticleSystemを取得して再生
                var particleSystem = idleStartingDeviceIcons[index].GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Play();
                    Debug.Log($"[IdleStartingDeviceIconManager] Playing shake effect for device {deviceId}");
                }
                else
                {
                    Debug.LogWarning($"[IdleStartingDeviceIconManager] No ParticleSystem found for device {deviceId}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[IdleStartingDeviceIconManager] Invalid deviceId: {deviceId}");
        }
    }
}
