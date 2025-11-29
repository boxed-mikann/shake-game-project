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
    
    void OnEnable()
    {
        if (SyncDetector.Instance != null)
        {
            SyncDetector.Instance.OnSyncDetected += ProcessSync;
        }
    }
    
    void OnDisable()
    {
        if (SyncDetector.Instance != null)
        {
            SyncDetector.Instance.OnSyncDetected -= ProcessSync;
        }
    }
    
    private void ProcessSync(float syncRate, double timestamp)
    {
        float phaseMultiplier = 1.0f; // TODO: PhaseManagerから取得（Phase 4）
        AddVoltage(syncRate, phaseMultiplier);
        
        if (GameConstantsV2.DEBUG_MODE)
        {
            float bonus = Mathf.Pow(syncRate, 2f);
            float increase = baseVoltagePerTick * phaseMultiplier * bonus;
            Debug.Log($"[VoltageManager] Sync: {syncRate:P0} → +{increase:F1}V (Total: {currentVoltage:F1}V)");
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
}
