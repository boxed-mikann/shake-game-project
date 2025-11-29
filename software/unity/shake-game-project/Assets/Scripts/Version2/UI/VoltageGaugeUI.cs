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
        if (VoltageManager.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged += OnVoltageChanged;
            // 現在の値で初期化
            targetValue = VoltageManager.Instance.GetVoltage() / GameConstantsV2.VOLTAGE_MAX;
            if (slider != null)
            {
                slider.value = targetValue;
            }
        }
    }
    
    void OnDisable()
    {
        if (VoltageManager.Instance != null)
        {
            VoltageManager.Instance.OnVoltageChanged -= OnVoltageChanged;
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
    }
}
