using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// ========================================
/// ResultScoreDisplay（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：リザルトパネルに最終スコア表示
/// 主機能：
/// - GameManager.OnGameOver を購読
/// - ScoreManager から最終スコアを取得
/// - TextMeshPro で表示
/// - StringBuilder で GC 削減
/// 
/// ========================================
/// </summary>
public class ResultScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    
    [Header("Display Settings")]
    [SerializeField] private string _prefix = "Final Score: ";
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        // GameManager のイベントを購読
        GameManager.OnGameOver.AddListener(OnGameOver);
    }
    
    /// <summary>
    /// ゲーム終了時のハンドラ
    /// </summary>
    private void OnGameOver()
    {
        if (_finalScoreText == null)
        {
            Debug.LogWarning("[ResultScoreDisplay] Final score text is not assigned!");
            return;
        }
        
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("[ResultScoreDisplay] ScoreManager instance not found!");
            return;
        }
        
        int finalScore = ScoreManager.Instance.GetScore();
        
        // StringBuilder で文字列構築（GC 削減）
        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);
        _stringBuilder.Append(finalScore);
        _finalScoreText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ResultScoreDisplay] Final score displayed: {finalScore}");
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (GameManager.OnGameOver != null)
        {
            GameManager.OnGameOver.RemoveListener(OnGameOver);
        }
    }
}
