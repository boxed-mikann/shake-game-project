using System;
using UnityEngine;

public class VoltageManager : MonoBehaviour
{
    public static VoltageManager Instance { get; private set; }

    public event Action<float> OnVoltageChanged;

    [Header("Voltage Settings")]
    [SerializeField] private float maxVoltage = GameConstantsV2.VOLTAGE_MAX;
    [SerializeField] private float baseVoltagePerTick = GameConstantsV2.BASE_VOLTAGE;

    private float currentVoltage = 0f;

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

    private void Start()
    {
        // ゲームフェーズイベントを購読
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart += ResetVoltage;
            GameManagerV2.Instance.OnGameStart += ResetVoltage;
            //デバッグログ
            Debug.Log("[VoltageManager] Subscribed to game phase events.");
        }
    }
    
    private void OnEnable()
    {
        // ゲームフェーズイベントを購読
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart += ResetVoltage;
            GameManagerV2.Instance.OnGameStart += ResetVoltage;
        }
    }
    private void OnDisable()
    {
        // unsubscribe to avoid memory leaks
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart -= ResetVoltage;
            GameManagerV2.Instance.OnGameStart -= ResetVoltage;
        }
    }
    public float CurrentVoltage => currentVoltage;
    public float MaxVoltage => maxVoltage;
    
    public float GetVoltage() => currentVoltage;

    public void ResetVoltage()
    {
        currentVoltage = 0f;
        OnVoltageChanged?.Invoke(currentVoltage);
    }

    // syncRate: 0.0 ~ 1.0, phaseMultiplier: 1.0 ~ 2.0 など
    public void AddVoltage(float syncRate, float phaseMultiplier)
    {
        float bonus = Mathf.Pow(syncRate, 2f);
        float increase = baseVoltagePerTick * phaseMultiplier * bonus;
        currentVoltage = Mathf.Clamp(currentVoltage + increase, 0f, maxVoltage);
        OnVoltageChanged?.Invoke(currentVoltage);
    }
    public void SimpleAddVoltage(float amount)
    {
        currentVoltage = Mathf.Clamp(currentVoltage + amount, 0f, maxVoltage);
        OnVoltageChanged?.Invoke(currentVoltage);
        // Debug log for voltage addition
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[VoltageManager] SimpleAddVoltage: +{amount:F1}V (Total: {currentVoltage:F1}V)");
        }
    }
}
