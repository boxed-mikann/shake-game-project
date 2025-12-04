using System.Collections;
using UnityEngine;

/// <summary>
/// ========================================
/// GameDeviceIconManager
/// ========================================
/// 
/// 責務：ゲーム中のデバイスアイコン表示管理
/// - ゲーム開始時に登録済みデバイスのアイコンのみ表示
/// - 未登録デバイスのアイコンを非表示
/// 
/// イベント駆動：
/// - GameManagerV2.OnGameStart を購読
/// ========================================
/// </summary>
public class GameDeviceIconManager : MonoBehaviour
{
    public static GameDeviceIconManager Instance { get; private set; }

    [Header("Game Device Icons")]
    [SerializeField] private GameObject[] gameDeviceIcons = new GameObject[10]; // ゲーム中のデバイスアイコン（インスペクターでアタッチ）

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
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
        }
        subscribedToGameManager = false;
    }

    private IEnumerator EnsureSubscriptions()
    {
        float timeout = 3f;
        float elapsed = 0f;

        // GameManagerV2の購読
        while (!subscribedToGameManager && elapsed < timeout)
        {
            if (GameManagerV2.Instance != null)
            {
                GameManagerV2.Instance.OnGameStart += OnGameStart;
                subscribedToGameManager = true;
                Debug.Log("[GameDeviceIconManager] Subscribed to GameManagerV2.OnGameStart");
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!subscribedToGameManager)
        {
            Debug.LogWarning("[GameDeviceIconManager] Failed to subscribe to GameManagerV2.OnGameStart within timeout");
        }
    }

    /// <summary>
    /// ゲーム開始時に呼ばれる
    /// 登録済みデバイスのアイコンのみ表示し、未登録デバイスのアイコンを非表示にする
    /// </summary>
    private void OnGameStart()
    {
        Debug.Log("[GameDeviceIconManager] OnGameStart called - hiding unregistered device icons");

        if (DeviceRegisterManager.Instance == null)
        {
            Debug.LogWarning("[GameDeviceIconManager] DeviceRegisterManager instance not found!");
            return;
        }

        // 各デバイスアイコンをチェック
        for (int i = 0; i < gameDeviceIcons.Length; i++)
        {
            if (gameDeviceIcons[i] != null)
            {
                string deviceId = i.ToString();
                bool isRegistered = DeviceRegisterManager.Instance.IsDeviceRegistered(deviceId);

                // 登録済みデバイスのみ表示
                gameDeviceIcons[i].SetActive(isRegistered);

                if (isRegistered)
                {
                    Debug.Log($"[GameDeviceIconManager] Device {deviceId} is registered - showing icon");
                }
                else
                {
                    Debug.Log($"[GameDeviceIconManager] Device {deviceId} is NOT registered - hiding icon");
                }
            }
        }

        Debug.Log($"[GameDeviceIconManager] Total registered devices: {DeviceRegisterManager.Instance.RegisteredDeviceCount}");
    }

    /// <summary>
    /// 指定したデバイスIDのアイコンを取得
    /// </summary>
    public GameObject GetDeviceIcon(string deviceId)
    {
        if (int.TryParse(deviceId, out int index) && index >= 0 && index < gameDeviceIcons.Length)
        {
            return gameDeviceIcons[index];
        }
        return null;
    }

    /// <summary>
    /// すべてのアイコンを表示（デバッグ用）
    /// </summary>
    public void ShowAllIcons()
    {
        foreach (var icon in gameDeviceIcons)
        {
            if (icon != null)
            {
                icon.SetActive(true);
            }
        }
        Debug.Log("[GameDeviceIconManager] All icons shown");
    }

    /// <summary>
    /// すべてのアイコンを非表示（デバッグ用）
    /// </summary>
    public void HideAllIcons()
    {
        foreach (var icon in gameDeviceIcons)
        {
            if (icon != null)
            {
                icon.SetActive(false);
            }
        }
        Debug.Log("[GameDeviceIconManager] All icons hidden");
    }
}
