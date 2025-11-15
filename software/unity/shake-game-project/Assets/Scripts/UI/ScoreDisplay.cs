using UnityEngine;
using TMPro;

/// <summary>
/// スコア表示の更新（UIManager と統合している場合は不要）
/// 独立管理が必要な場合用
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    
    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
        }
    }
    
    private void UpdateScore(int newScore)
    {
        if (scoreText != null)
            scoreText.text = newScore.ToString();
    }
}