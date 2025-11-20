using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// ========================================
/// HighScoreDisplay（ハイスコア表示）
/// ========================================
/// 
/// 責務：タイトル画面・ゲーム中のハイスコア表示
/// 
/// 機能：
///   - ハイスコアをTextMeshProに表示
///   - 新記録時の自動更新
///   - StringBuilderによるGC削減
/// 
/// イベント購読：
///   - HighScoreManager.OnHighScoreUpdated → 表示更新
/// 
/// Inspector設定：
///   - _highScoreText: TextMeshProUGUIコンポーネント
///   - _prefix: 表示プレフィックス（デフォルト: "High Score: "）
/// 
/// ========================================
/// </summary>
public class HighScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private string _prefix = "High Score: ";
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        if (_highScoreText == null)
        {
            Debug.LogError("[HighScoreDisplay] _highScoreText is not assigned!");
            return;
        }
        
        UpdateDisplay(HighScoreManager.Instance.GetHighScore());
        HighScoreManager.OnHighScoreUpdated.AddListener(UpdateDisplay);
    }
    
    void OnDestroy()
    {
        HighScoreManager.OnHighScoreUpdated.RemoveListener(UpdateDisplay);
    }
    
    void UpdateDisplay(int highScore)
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(_prefix);
        _stringBuilder.Append(highScore);
        _highScoreText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[HighScoreDisplay] Updated display: {_stringBuilder}");
    }
}
