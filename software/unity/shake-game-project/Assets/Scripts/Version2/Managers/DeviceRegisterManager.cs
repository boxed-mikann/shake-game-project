using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ========================================
/// DeviceRegisterManager
/// ========================================
/// 
/// 責務：デバイス登録管理とRegister時のUI管理
/// - デバイスの登録状態管理（状態機械）
/// - 登録時のアイコン透明度制御
/// - 登録済みデバイス情報の提供
/// 
/// 状態遷移：
/// 未登録(Unregistered) → シェイク中(ShakingUnregistered) → 登録済み(Registered) → 登録解除中(Unregistering)
/// ========================================
/// </summary>
public class DeviceRegisterManager : MonoBehaviour
{
    public static DeviceRegisterManager Instance { get; private set; }

    // デバイスアイコンの状態
    public enum RegistrationState
    {
        Unregistered,           // 未登録（透明度80%）
        ShakingUnregistered,    // シェイク中（透明度50%）
        Registered,             // 登録済み（透明度0% - 完全表示）
        Unregistering           // 登録解除中（徐々に80%へ）
    }

    [Serializable]
    public class DeviceIconState
    {
        public string deviceId;
        public RegistrationState state = RegistrationState.Unregistered;
        public int consecutiveShakeCount = 0;
        public double lastShakeTime = 0;
        public double stateChangeTime = 0;
        public float currentOpacity = 0.8f; // 初期は80%透明
        public GameObject iconObject;
        public SpriteRenderer spriteRenderer;  // ImageではなくSpriteRenderer
        public ParticleSystem shakeEffect;
    }

    [Header("Register UI Settings")]
    [SerializeField] private GameObject[] deviceIconObjects = new GameObject[10]; // インスペクターでアタッチ

    [Header("Registration Settings")]
    [SerializeField] private int shakesNeededToRegister = 10;     // 連続10回で登録
    [SerializeField] private float shakeWindowSeconds = 0.3f;     // 0.3秒以内で連続判定
    [SerializeField] private float timeoutSeconds = 30f;          // 30秒無操作でタイムアウト
    
    [Header("Opacity Settings")]
    [SerializeField] private float opacityUnregistered = 0.8f;    // 未登録時の透明度
    [SerializeField] private float opacityShaking = 0.5f;         // シェイク中の透明度
    [SerializeField] private float opacityRegistered = 0.0f;      // 登録済みの透明度(完全表示)
    [SerializeField] private float unregisteringDuration = 30f;   // 登録解除にかかる時間

    private Dictionary<string, DeviceIconState> deviceStates = new Dictionary<string, DeviceIconState>();
    private HashSet<string> registeredDevices = new HashSet<string>();
    private bool subscribedToGameManager = false;

    // 公開プロパティ：登録済みデバイスのリスト
    public IReadOnlyCollection<string> RegisteredDevices => registeredDevices;
    public int RegisteredDeviceCount => registeredDevices.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        InitializeDeviceIcons();
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
            GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
        }
        subscribedToGameManager = false;
    }

    private System.Collections.IEnumerator EnsureSubscriptions()
    {
        float timeout = 3f;
        float elapsed = 0f;

        // GameManagerV2の購読
        while (!subscribedToGameManager && elapsed < timeout)
        {
            if (GameManagerV2.Instance != null)
            {
                GameManagerV2.Instance.OnResisterStart += OnResisterStart;
                subscribedToGameManager = true;
                Debug.Log("[DeviceRegisterManager] Subscribed to GameManagerV2.OnResisterStart");
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!subscribedToGameManager)
        {
            Debug.LogWarning("[DeviceRegisterManager] Failed to subscribe to GameManagerV2.OnResisterStart within timeout");
        }
    }

    /// <summary>
    /// Resisterモード開始時に呼ばれる
    /// すべてのデバイス登録をリセットする
    /// </summary>
    private void OnResisterStart()
    {
        Debug.Log("[DeviceRegisterManager] OnResisterStart called - resetting all registrations");
        ResetAllRegistrations();
    }

    private void InitializeDeviceIcons()
    {
        // デバイスアイコンの初期化
        for (int i = 0; i < deviceIconObjects.Length; i++)
        {
            if (deviceIconObjects[i] != null)
            {
                string deviceId = i.ToString();
                var state = new DeviceIconState
                {
                    deviceId = deviceId,
                    iconObject = deviceIconObjects[i],
                    spriteRenderer = deviceIconObjects[i].GetComponent<SpriteRenderer>(),
                    shakeEffect = deviceIconObjects[i].GetComponentInChildren<ParticleSystem>()
                };
                
                deviceStates[deviceId] = state;
                
                // 初期透明度を設定
                UpdateIconOpacity(state);
                
                Debug.Log($"[DeviceRegisterManager] Initialized icon for device {deviceId}, SpriteRenderer={state.spriteRenderer != null}");
            }
        }
    }

    private void Update()
    {
        // 各デバイスの状態を更新
        double currentTime = AudioSettings.dspTime;
        
        foreach (var state in deviceStates.Values)
        {
            UpdateDeviceState(state, currentTime);
        }
    }

    /// <summary>
    /// シェイク情報を受け取り、登録処理を行う
    /// IdleRegisterHandlerから呼び出される
    /// </summary>
    public void ProcessShake(string deviceId, double timestamp)
    {
        if (!deviceStates.TryGetValue(deviceId, out var state))
        {
            Debug.LogWarning($"[DeviceRegisterManager] Unknown device ID: {deviceId}");
            return;
        }

        double timeSinceLastShake = timestamp - state.lastShakeTime;
        state.lastShakeTime = timestamp;

        // シェイクエフェクトを再生
        if (state.shakeEffect != null)
        {
            state.shakeEffect.Play();
        }

        switch (state.state)
        {
            case RegistrationState.Unregistered:
                // 未登録状態からシェイク中へ遷移
                state.state = RegistrationState.ShakingUnregistered;
                state.consecutiveShakeCount = 1;
                state.stateChangeTime = timestamp;
                Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Unregistered → ShakingUnregistered (1/{shakesNeededToRegister})");
                break;

            case RegistrationState.ShakingUnregistered:
                // シェイク中の処理
                if (timeSinceLastShake <= shakeWindowSeconds)
                {
                    // 連続シェイクとしてカウント
                    state.consecutiveShakeCount++;
                    Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Shake count {state.consecutiveShakeCount}/{shakesNeededToRegister}");

                    // 10回連続で登録完了
                    if (state.consecutiveShakeCount >= shakesNeededToRegister)
                    {
                        state.state = RegistrationState.Registered;
                        state.stateChangeTime = timestamp;
                        registeredDevices.Add(deviceId);
                        Debug.Log($"[DeviceRegisterManager] Device {deviceId}: REGISTERED!");
                    }
                }
                else
                {
                    // 時間が空いたのでカウントリセット
                    state.consecutiveShakeCount = 1;
                    Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Count reset (too slow)");
                }
                break;

            case RegistrationState.Registered:
                // 登録済み：シェイクでタイムアウトタイマーをリセット
                state.stateChangeTime = timestamp;
                Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Timeout timer reset");
                break;

            case RegistrationState.Unregistering:
                // 登録解除中にシェイク：再登録
                state.state = RegistrationState.ShakingUnregistered;
                state.consecutiveShakeCount = 1;
                state.stateChangeTime = timestamp;
                Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Unregistering → ShakingUnregistered");
                break;
        }

        UpdateIconOpacity(state);
    }

    private void UpdateDeviceState(DeviceIconState state, double currentTime)
    {
        double timeSinceStateChange = currentTime - state.stateChangeTime;

        switch (state.state)
        {
            case RegistrationState.ShakingUnregistered:
                // シェイク中：0.3秒無操作で未登録に戻る
                if (timeSinceStateChange > shakeWindowSeconds)
                {
                    state.state = RegistrationState.Unregistered;
                    state.consecutiveShakeCount = 0;
                    state.stateChangeTime = currentTime;
                    Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: ShakingUnregistered → Unregistered (timeout)");
                    UpdateIconOpacity(state);
                }
                break;

            case RegistrationState.Registered:
                // 登録済み：30秒無操作で登録解除開始
                if (timeSinceStateChange > timeoutSeconds)
                {
                    state.state = RegistrationState.Unregistering;
                    state.stateChangeTime = currentTime;
                    Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: Registered → Unregistering");
                }
                break;

            case RegistrationState.Unregistering:
                // 登録解除中：30秒かけて透明度を80%に戻す
                float progress = Mathf.Clamp01((float)(timeSinceStateChange / unregisteringDuration));
                state.currentOpacity = Mathf.Lerp(opacityRegistered, opacityUnregistered, progress);
                
                if (progress >= 1.0f)
                {
                    // 登録解除完了
                    state.state = RegistrationState.Unregistered;
                    state.stateChangeTime = currentTime;
                    registeredDevices.Remove(state.deviceId);
                    Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: Unregistering → Unregistered (complete)");
                }
                
                UpdateIconOpacity(state);
                break;
        }
    }

    private void UpdateIconOpacity(DeviceIconState state)
    {
        if (state.spriteRenderer == null)
        {
            Debug.LogWarning($"[DeviceRegisterManager] SpriteRenderer is null for device {state.deviceId}");
            return;
        }

        // 状態に応じた透明度を設定（Unregistering以外）
        if (state.state != RegistrationState.Unregistering)
        {
            switch (state.state)
            {
                case RegistrationState.Unregistered:
                    state.currentOpacity = opacityUnregistered;
                    break;
                case RegistrationState.ShakingUnregistered:
                    state.currentOpacity = opacityShaking;
                    break;
                case RegistrationState.Registered:
                    state.currentOpacity = opacityRegistered;
                    break;
            }
        }

        // SpriteRendererのアルファ値を更新
        // currentOpacityは透明度(0=完全表示, 1=完全透明)なので、
        // alpha = 1 - opacity として不透明度に変換
        Color color = state.spriteRenderer.color;
        color.a = 1.0f - state.currentOpacity;
        state.spriteRenderer.color = color;
        
        Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: State={state.state}, Opacity={state.currentOpacity}, Alpha={color.a}");
    }

    /// <summary>
    /// 指定したデバイスが登録済みかどうかを判定
    /// </summary>
    public bool IsDeviceRegistered(string deviceId)
    {
        return registeredDevices.Contains(deviceId);
    }

    /// <summary>
    /// デバイスの現在の状態を取得
    /// </summary>
    public RegistrationState GetDeviceState(string deviceId)
    {
        if (deviceStates.TryGetValue(deviceId, out var state))
        {
            return state.state;
        }
        return RegistrationState.Unregistered;
    }

    /// <summary>
    /// すべてのデバイスの登録をリセット
    /// </summary>
    public void ResetAllRegistrations()
    {
        registeredDevices.Clear();
        
        foreach (var state in deviceStates.Values)
        {
            state.state = RegistrationState.Unregistered;
            state.consecutiveShakeCount = 0;
            state.stateChangeTime = AudioSettings.dspTime;
            UpdateIconOpacity(state);
        }
        
        Debug.Log("[DeviceRegisterManager] All registrations reset");
    }
}
