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
    
    [Header("New Record Display")]
    [SerializeField] private Color _highlightColor = Color.yellow;
    [SerializeField] private TextMeshProUGUI _newRecordText;  // オプション
    
    private StringBuilder _stringBuilder = new StringBuilder();
    private Color _originalColor;
    
    void Start()
    {
        // GameManager のイベントを購読
        GameManager.OnGameOver.AddListener(OnGameOver);
        
        // 元の色を保存
        if (_finalScoreText != null)
            _originalColor = _finalScoreText.color;
        
        // 新記録テキストを初期非表示
        if (_newRecordText != null)
            _newRecordText.gameObject.SetActive(false);
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
        
        // 新記録チェック
        if (HighScoreManager.Instance != null && HighScoreManager.Instance.IsNewHighScore(finalScore))
        {
            ShowNewRecordEffect();
        }
        else
        {
            // 新記録でない場合は元の色に戻す
            if (_finalScoreText != null)
                _finalScoreText.color = _originalColor;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ResultScoreDisplay] Final score displayed: {finalScore}");
    }
    
    /// <summary>
    /// 新記録時の強調表示
    /// </summary>
    private void ShowNewRecordEffect()
    {
        // 色変更
        if (_highlightColor != Color.clear && _finalScoreText != null)
            _finalScoreText.color = _highlightColor;
        
        // 追加テキスト表示（オプション）
        if (_newRecordText != null)
            _newRecordText.gameObject.SetActive(true);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[ResultScoreDisplay] New record displayed!");
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
