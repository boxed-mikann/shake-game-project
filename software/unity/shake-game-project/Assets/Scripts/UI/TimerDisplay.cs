using UnityEngine;
using TMPro;

/// <summary>
/// タイマー表示の更新（UIManager と統合している場合は不要）
/// 独立管理が必要な場合用
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
        {
            float remainingTime = Mathf.Max(0f, GameManager.Instance.GameTimer);
            if (timerText != null)
                timerText.text = remainingTime.ToString("F1");
        }
    }
}