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
    
    private System.Action onGameStartAction;
    private System.Action onGameEndAction;
    
    void Start()
    {
        SubscribeToEvents();
    }
    
    void OnEnable()
    {
        SubscribeToEvents();
    }
    
    void OnDisable()
    {
        if (VoltageManager.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged -= OnVoltageChanged;
        }
        if (GameManagerV2.Instance != null && onGameStartAction != null && onGameEndAction != null)
        {
            GameManagerV2.Instance.OnGameStart -= onGameStartAction;
            GameManagerV2.Instance.OnGameEnd -= onGameEndAction;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (VoltageManager.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            VoltageManager.Instance.OnVoltageChanged -= OnVoltageChanged;
            VoltageManager.Instance.OnVoltageChanged += OnVoltageChanged;
        }
        
        if (GameManagerV2.Instance != null)
        {
            // ラムダ式は毎回新しいインスタンスを作るため、フィールドに保存
            if (onGameStartAction == null) onGameStartAction = () => ResetGauge();
            if (onGameEndAction == null) onGameEndAction = () => ResetGauge();
            
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnGameStart -= onGameStartAction;
            GameManagerV2.Instance.OnGameStart += onGameStartAction;
            GameManagerV2.Instance.OnGameEnd -= onGameEndAction;
            GameManagerV2.Instance.OnGameEnd += onGameEndAction;
        }
        
        // 現在の値で初期化
        ResetGauge();
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
