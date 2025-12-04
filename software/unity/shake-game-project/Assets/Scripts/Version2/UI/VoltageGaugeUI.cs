using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ボルテージゲージUI - Sliderでボルテージを視覚化
/// </summary>
public class VoltageGaugeUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float lerpSpeed = 5f;
    
    private float targetValue = 0f;
    
    void OnEnable()
    {
        if (VoltageManager.Instance != null && GameManagerV2.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged += OnVoltageChanged;
            GameManagerV2.Instance.OnGameStart += () => ResetGauge();
            GameManagerV2.Instance.OnGameEnd += () => ResetGauge();
            // 現在の値で初期化
            ResetGauge();
        }
    }
    void Start()
    {
        if (VoltageManager.Instance != null && GameManagerV2.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged += OnVoltageChanged;
            GameManagerV2.Instance.OnGameStart += () => ResetGauge();
            GameManagerV2.Instance.OnGameEnd += () => ResetGauge();
            // 現在の値で初期化
            ResetGauge();            
        }
    }
    void OnDisable()
    {
        if (VoltageManager.Instance != null && GameManagerV2.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged -= OnVoltageChanged;
            GameManagerV2.Instance.OnGameStart -= () => ResetGauge();
            GameManagerV2.Instance.OnGameEnd -= () => ResetGauge();
        }
    }
    
    void Update()
    {
        if (slider != null)
        {
            slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * lerpSpeed);
        }
    }
    
    private void OnVoltageChanged(float newVoltage)
    {
        targetValue = newVoltage / GameConstantsV2.VOLTAGE_MAX;
        // Debug log for voltage change
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[VoltageGaugeUI] Voltage changed: {newVoltage:F1}V, TargetValue: {targetValue:F3}");
        }   
    }
    private void ResetGauge()//現在の値で初期化
    {
        if (VoltageManager.Instance != null)
        {
            float currentVoltage = VoltageManager.Instance.GetVoltage();
            targetValue = currentVoltage / GameConstantsV2.VOLTAGE_MAX;
            if (slider != null)
            {
                slider.value = targetValue;
            }
            // Debug log for gauge reset
            if (GameConstantsV2.DEBUG_MODE)
            {
                Debug.Log($"[VoltageGaugeUI] Gauge reset: {currentVoltage:F1}V, TargetValue: {targetValue:F3}");
            }   
        }
    }
}
