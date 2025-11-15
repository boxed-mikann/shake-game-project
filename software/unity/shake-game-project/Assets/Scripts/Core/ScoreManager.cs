using UnityEngine;

/// <summary>
/// スコア管理
/// 責務：スコア計算、加点、ペナルティ管理
/// </summary>
public class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScoreManager>();
            }
            return _instance;
        }
    }

    private int _currentScore = 0;
    private int _restHitCount = 0;  // 休符をはじけた回数（ペナルティ計算用）
    
    // イベント
    public delegate void OnScoreChangedEvent(int newScore);
    public event OnScoreChangedEvent OnScoreChanged;
    
    public void Initialize()
    {
        _currentScore = 0;
        _restHitCount = 0;
        OnScoreChanged?.Invoke(_currentScore);
    }
    
    /// <summary>
    /// スコア加算（音符をはじけた場合）
    /// </summary>
    public void AddNoteScore(int noteCount)
    {
        int points = noteCount * GameConstants.NOTE_SCORE;
        _currentScore += points;
        OnScoreChanged?.Invoke(_currentScore);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ScoreManager] +{points} points (Notes: {noteCount}). Total: {_currentScore}");
    }
    
    /// <summary>
    /// スコア減算（休符をはじけた場合）
    /// </summary>
    public void SubtractRestPenalty(int restCount)
    {
        int penalty = restCount * GameConstants.REST_PENALTY;
        _currentScore += penalty;  // NOTE_PENALTYは負の値
        _restHitCount += restCount;
        OnScoreChanged?.Invoke(_currentScore);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ScoreManager] {penalty} penalty (Rests: {restCount}). Total: {_currentScore}");
    }
    
    /// <summary>
    /// ラストスパート時のスコア計算
    /// </summary>
    public void AddLastSprintBonus(int noteCount)
    {
        int basePoints = noteCount * GameConstants.NOTE_SCORE;
        int bonusPoints = (int)(basePoints * (GameConstants.LAST_SPRINT_MULTIPLIER - 1f));  // 追加ボーナス分
        _currentScore += basePoints + bonusPoints;
        OnScoreChanged?.Invoke(_currentScore);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ScoreManager] +{basePoints + bonusPoints} points (LastSprint x{GameConstants.LAST_SPRINT_MULTIPLIER}). Total: {_currentScore}");
    }
    
    /// <summary>
    /// ゲーム終了時のボーナス計算
    /// </summary>
    public int GetFinalScore()
    {
        int finalScore = _currentScore;
        
        // 完璧プレイボーナス（休符ノーミス）
        if (_restHitCount == 0)
        {
            finalScore += GameConstants.PERFECT_BONUS;
            Debug.Log($"[ScoreManager] Perfect Play! +{GameConstants.PERFECT_BONUS} bonus");
        }
        
        return finalScore;
    }
    
    public int CurrentScore => _currentScore;
    public int RestHitCount => _restHitCount;
}