using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ========================================
/// HighScoreManager（ハイスコア管理）
/// ========================================
/// 
/// 責務：ハイスコアの保存・読み込み・更新
/// 
/// 機能：
///   - PlayerPrefsによる永続化
///   - ゲーム終了時のハイスコアチェック
///   - 新記録時のイベント発行
///   - エディタ用のリセット機能
/// 
/// イベント購読：
///   - GameManager.OnGameOver → ハイスコアチェック
/// 
/// イベント発行：
///   - OnHighScoreUpdated → 新記録時
/// 
/// ========================================
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }
    public static UnityEvent<int> OnHighScoreUpdated = new UnityEvent<int>();
    
    private int _currentHighScore = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _currentHighScore = PlayerPrefs.GetInt(GameConstants.HIGH_SCORE_KEY, 0);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[HighScoreManager] Initialized with high score: {_currentHighScore}");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEnable()
    {
        GameManager.OnGameOver.AddListener(CheckAndUpdateHighScore);
    }
    
    void OnDisable()
    {
        GameManager.OnGameOver.RemoveListener(CheckAndUpdateHighScore);
    }
    
    void CheckAndUpdateHighScore()
    {
        int currentScore = ScoreManager.Instance.GetScore();
        
        if (currentScore > _currentHighScore)
        {
            _currentHighScore = currentScore;
            PlayerPrefs.SetInt(GameConstants.HIGH_SCORE_KEY, _currentHighScore);
            PlayerPrefs.Save();
            
            OnHighScoreUpdated.Invoke(_currentHighScore);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[HighScoreManager] New high score: {_currentHighScore}");
        }
        else
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[HighScoreManager] Score {currentScore} did not beat high score {_currentHighScore}");
        }
    }
    
    public int GetHighScore()
    {
        return _currentHighScore;
    }
    
    public bool IsNewHighScore(int score)
    {
        return score > _currentHighScore;
    }
    
#if UNITY_EDITOR
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(GameConstants.HIGH_SCORE_KEY);
        _currentHighScore = 0;
        Debug.Log("[HighScoreManager] High score reset");
    }
#endif
}
