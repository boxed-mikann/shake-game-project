using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ========================================
/// GameManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：ゲーム全体のライフサイクル管理
/// - ゲーム開始・終了の状態管理
/// - 静的イベントによる全システムへの通知
/// - PhaseManager の実行制御
/// 
/// 新設計の特徴：
/// - フェーズ管理は PhaseManager に移譲
/// - イベント駆動で疎結合
/// - シングルトンパターン採用
/// 
/// ========================================
/// </summary>
public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }
    
    // 静的イベント（全システムがアクセス可能）
    public static UnityEvent OnGameStart = new UnityEvent();
    public static UnityEvent OnGameOver = new UnityEvent();
    
    // ゲーム状態
    private bool _isGameRunning = false;
    
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
            Debug.Log("[GameManager] Initialized");
    }
    
    /// <summary>
    /// ゲーム開始
    /// OnGameStart イベントを発行し、PhaseManager が開始する
    /// </summary>
    public static void StartGame()
    {
        if (Instance == null)
        {
            Debug.LogError("[GameManager] Instance not found!");
            return;
        }
        
        if (Instance._isGameRunning)
        {
            Debug.LogWarning("[GameManager] Game is already running!");
            return;
        }
        
        Instance._isGameRunning = true;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[GameManager] ▶️ Game started!");
        
        // ゲーム開始イベント発行
        OnGameStart.Invoke();
    }
    
    /// <summary>
    /// ゲーム終了
    /// OnGameOver イベントを発行
    /// </summary>
    public static void EndGame()
    {
        if (Instance == null)
        {
            Debug.LogError("[GameManager] Instance not found!");
            return;
        }
        
        if (!Instance._isGameRunning)
        {
            Debug.LogWarning("[GameManager] Game is not running!");
            return;
        }
        
        Instance._isGameRunning = false;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[GameManager] ⏹️ Game ended!");
        
        // ゲーム終了イベント発行
        OnGameOver.Invoke();
    }
    
    /// <summary>
    /// ゲーム実行中かどうか
    /// </summary>
    public static bool IsGameRunning()
    {
        return Instance != null && Instance._isGameRunning;
    }
}