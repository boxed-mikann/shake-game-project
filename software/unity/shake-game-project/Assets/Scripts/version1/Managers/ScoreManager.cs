using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ========================================
/// ScoreManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：スコアの加算・減算とイベント発行
/// - Phase*ShakeHandler から AddScore(points) で呼ばれる
/// - スコア変更時に常に OnScoreChanged.Invoke(currentScore) を発火
/// 
/// イベント発行：
/// - OnScoreChanged(int) → UI層に通知（現在スコア）
/// 
/// 参照元：Assets/Scripts/FormerCodes/Game/ の Note 処理時の加減点ロジック
/// - NotePhase: +1
/// - RestPhase: -1（またはペナルティ値）
/// 
/// ========================================
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // シングルトンインスタンス
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
    
    // スコア変更イベント（現在スコアを引数）
    public static UnityEvent<int> OnScoreChanged = new UnityEvent<int>();
    
    // 現在のスコア
    private int _currentScore = 0;
    
    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[ScoreManager] Initialized");
    }
    
    private void OnEnable()
    {
        // GameManager.OnGameStart を購読してスコアをリセット
        GameManager.OnGameStart.AddListener(Initialize);
        GameManager.OnShowTitle.AddListener(Initialize);
    }
    
    private void OnDisable()
    {
        // イベント購読解除
        GameManager.OnGameStart.RemoveListener(Initialize);
        GameManager.OnShowTitle.RemoveListener(Initialize);
    }
    
    /// <summary>
    /// 初期化（スコアリセット）
    /// </summary>
    public void Initialize()
    {
        _currentScore = 0;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[ScoreManager] Score reset to 0");
        
        // OnScoreChanged イベント発行
        OnScoreChanged.Invoke(_currentScore);
    }
    
    /// <summary>
    /// スコア加算（負数で減点可）
    /// </summary>
    public void AddScore(int points)
    {
        _currentScore += points;
        
        // スコアが負にならないようにする（オプション）
        if (_currentScore < 0)
        {
            _currentScore = 0;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ScoreManager] Score changed: {points:+#;-#;0} → Total: {_currentScore}");
        
        // OnScoreChanged イベント発行
        OnScoreChanged.Invoke(_currentScore);
    }
    
    /// <summary>
    /// 現在のスコアを取得
    /// </summary>
    public int GetScore()
    {
        return _currentScore;
    }
}