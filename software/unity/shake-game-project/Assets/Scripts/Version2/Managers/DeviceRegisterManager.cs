using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ========================================
/// DeviceRegisterManager
/// ========================================
/// 
/// 責務：デバイス登録管琁E��Register時�EUI管琁E
/// - チE��イスの登録状態管琁E��状態機械�E�E
/// - 登録時�Eアイコン透�E度制御
/// - 登録済みチE��イス惁E��の提侁E
/// 
/// 状態�E移�E�E
/// 未登録(Unregistered) ↁEシェイク中(ShakingUnregistered) ↁE登録済み(Registered) ↁE登録解除中(Unregistering)
/// ========================================
/// </summary>
public class DeviceRegisterManager : MonoBehaviour
{
    public static DeviceRegisterManager Instance { get; private set; }

    // チE��イスアイコンの状慁E
    public enum RegistrationState
    {
        Unregistered,           // 未登録�E�透�E度80%�E�E
        ShakingUnregistered,    // シェイク中�E�透�E度50%�E�E
        Registered,             // 登録済み�E�透�E度0% - 完�E表示�E�E
        Unregistering           // 登録解除中�E�徐、E��80%へ�E�E
    }

    [Serializable]
    public class DeviceIconState
    {
        public string deviceId;
        public RegistrationState state = RegistrationState.Unregistered;
        public int consecutiveShakeCount = 0;
        public double lastShakeTime = 0;
        public double stateChangeTime = 0;
        public float currentOpacity = 0.8f; // 初期は80%透�E
        public GameObject iconObject;
        public SpriteRenderer spriteRenderer;  // ImageではなくSpriteRenderer
        public ParticleSystem shakeEffect;
    }

    [Header("Register UI Settings")]
    [SerializeField] private GameObject[] deviceIconObjects = new GameObject[10]; // インスペクターでアタチE��

    [Header("Registration Settings")]
    [SerializeField] private int shakesNeededToRegister = 10;     // 連綁E0回で登録
    [SerializeField] private float shakeWindowSeconds = 0.3f;     // 0.3秒以冁E��連続判宁E
    [SerializeField] private float timeoutSeconds = 30f;          // 30秒無操作でタイムアウチE
    
    [Header("Opacity Settings")]
    [SerializeField] private float opacityUnregistered = 0.8f;    // 未登録時�E透�E度
    [SerializeField] private float opacityShaking = 0.5f;         // シェイク中の透�E度
    [SerializeField] private float opacityRegistered = 0.0f;      // 登録済みの透�E度(完�E表示)
    [SerializeField] private float unregisteringDuration = 30f;   // 登録解除にかかる時閁E

    private Dictionary<string, DeviceIconState> deviceStates = new Dictionary<string, DeviceIconState>();
    private HashSet<string> registeredDevices = new HashSet<string>();
    private bool subscribedToGameManager = false;

    // 公開�Eロパティ�E�登録済みチE��イスのリスチE
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
        // 遁E��購読でAwake頁E��に依存しなぁE��ぁE��する
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
                // // Debug.Log("[DeviceRegisterManager] Subscribed to GameManagerV2.OnResisterStart");
                break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!subscribedToGameManager)
        {
            // // Debug.LogWarning("[DeviceRegisterManager] Failed to subscribe to GameManagerV2.OnResisterStart within timeout");
        }
    }

    /// <summary>
    /// Resisterモード開始時に呼ばれる
    /// すべてのチE��イス登録をリセチE��する
    /// </summary>
    private void OnResisterStart()
    {
        // // Debug.Log("[DeviceRegisterManager] OnResisterStart called - resetting all registrations");
        ResetAllRegistrations();
    }

    private void InitializeDeviceIcons()
    {
        // チE��イスアイコンの初期匁E
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
                
                // 初期透�E度を設宁E
                UpdateIconOpacity(state);
                
                // // Debug.Log($"[DeviceRegisterManager] Initialized icon for device {deviceId}, SpriteRenderer={state.spriteRenderer != null}");
            }
        }
    }

    private void Update()
    {
        // 吁E��バイスの状態を更新
        double currentTime = AudioSettings.dspTime;
        
        foreach (var state in deviceStates.Values)
        {
            UpdateDeviceState(state, currentTime);
        }
    }

    /// <summary>
    /// シェイク惁E��を受け取り、登録処琁E��行う
    /// IdleRegisterHandlerから呼び出されめE
    /// </summary>
    public void ProcessShake(string deviceId, double timestamp)
    {
        if (!deviceStates.TryGetValue(deviceId, out var state))
        {
            // // Debug.LogWarning($"[DeviceRegisterManager] Unknown device ID: {deviceId}");
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
                // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Unregistered ↁEShakingUnregistered (1/{shakesNeededToRegister})");
                break;

            case RegistrationState.ShakingUnregistered:
                // シェイク中の処琁E
                if (timeSinceLastShake <= shakeWindowSeconds)
                {
                    // 連続シェイクとしてカウンチE
                    state.consecutiveShakeCount++;
                    // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Shake count {state.consecutiveShakeCount}/{shakesNeededToRegister}");

                    // 10回連続で登録完亁E
                    if (state.consecutiveShakeCount >= shakesNeededToRegister)
                    {
                        state.state = RegistrationState.Registered;
                        state.stateChangeTime = timestamp;
                        registeredDevices.Add(deviceId);
                        // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: REGISTERED!");
                        
                        // 登録完亁E��を�E甁E
                        if (SEManager.Instance != null)
                        {
                            SEManager.Instance.PlayRegisterSound();
                        }
                    }
                }
                else
                {
                    // 時間が空ぁE��のでカウントリセチE��
                    state.consecutiveShakeCount = 1;
                    // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Count reset (too slow)");
                }
                break;

            case RegistrationState.Registered:
                // 登録済み�E�シェイクでタイムアウトタイマ�EをリセチE��
                state.stateChangeTime = timestamp;
                // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Timeout timer reset");
                break;

            case RegistrationState.Unregistering:
                // 登録解除中にシェイク�E��E登録
                state.state = RegistrationState.ShakingUnregistered;
                state.consecutiveShakeCount = 1;
                state.stateChangeTime = timestamp;
                // Debug.Log($"[DeviceRegisterManager] Device {deviceId}: Unregistering ↁEShakingUnregistered");
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
                // シェイク中�E�E.3秒無操作で未登録に戻めE
                if (timeSinceStateChange > shakeWindowSeconds)
                {
                    state.state = RegistrationState.Unregistered;
                    state.consecutiveShakeCount = 0;
                    state.stateChangeTime = currentTime;
                    // Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: ShakingUnregistered ↁEUnregistered (timeout)");
                    UpdateIconOpacity(state);
                }
                break;

            case RegistrationState.Registered:
                // 登録済み�E�E0秒無操作で登録解除開姁E
                if (timeSinceStateChange > timeoutSeconds)
                {
                    state.state = RegistrationState.Unregistering;
                    state.stateChangeTime = currentTime;
                    // // Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: Registered ↁEUnregistering");
                }
                break;

            case RegistrationState.Unregistering:
                // 登録解除中�E�E0秒かけて透�E度めE0%に戻ぁE
                float progress = Mathf.Clamp01((float)(timeSinceStateChange / unregisteringDuration));
                state.currentOpacity = Mathf.Lerp(opacityRegistered, opacityUnregistered, progress);
                
                if (progress >= 1.0f)
                {
                    // 登録解除完亁E
                    state.state = RegistrationState.Unregistered;
                    state.stateChangeTime = currentTime;
                    registeredDevices.Remove(state.deviceId);
                    // Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: Unregistering ↁEUnregistered (complete)");
                }
                
                UpdateIconOpacity(state);
                break;
        }
    }

    private void UpdateIconOpacity(DeviceIconState state)
    {
        if (state.spriteRenderer == null)
        {
            // // Debug.LogWarning($"[DeviceRegisterManager] SpriteRenderer is null for device {state.deviceId}");
            return;
        }

        // 状態に応じた透�E度を設定！Enregistering以外！E
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
        // currentOpacityは透�E度(0=完�E表示, 1=完�E透�E)なので、E
        // alpha = 1 - opacity として不透�E度に変換
        Color color = state.spriteRenderer.color;
        color.a = 1.0f - state.currentOpacity;
        state.spriteRenderer.color = color;
        
        // Debug.Log($"[DeviceRegisterManager] Device {state.deviceId}: State={state.state}, Opacity={state.currentOpacity}, Alpha={color.a}");
    }

    /// <summary>
    /// 持E��したデバイスが登録済みかどぁE��を判宁E
    /// </summary>
    public bool IsDeviceRegistered(string deviceId)
    {
        return registeredDevices.Contains(deviceId);
    }

    /// <summary>
    /// チE��イスの現在の状態を取征E
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
    /// すべてのチE��イスの登録をリセチE��
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
        
        // Debug.Log("[DeviceRegisterManager] All registrations reset");
    }

}
