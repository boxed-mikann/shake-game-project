using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// ========================================
/// ScoreDisplay（新アーキテクチャ版）
/// ========================================
/// 
/// 責補：スコア数値表示
/// 主機能：
/// - ScoreManager.OnScoreChanged を購読
/// - TextMeshPro で数値表示
/// - StringBuilder で GC 削減
/// 
/// ========================================
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    
    [Header("Display Settings")]
    [SerializeField] private string _prefix = "Score: ";
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        // ScoreManager のイベントを購読
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
            
            // 初期表示
            OnScoreChanged(ScoreManager.Instance.GetScore());
        }
        else
        {
            Debug.LogError("[ScoreDisplay] ScoreManager instance not found!");
        }
    }
    
    /// <summary>
    /// スコア変更時のハンドラ
    /// </summary>
    private void OnScoreChanged(int score)
    {
        if (_scoreText == null)
        {
            Debug.LogWarning("[ScoreDisplay] Score text is not assigned!");
            return;
        }
        
        // StringBuilder で文字列構築（GC 削減）
        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);
        _stringBuilder.Append(score);
        
        _scoreText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ScoreDisplay] Updated score display: {score}");
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (ScoreManager.Instance != null && ScoreManager.Instance.OnScoreChanged != null)
        {
            ScoreManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
        }
    }
}