using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// ========================================
/// TimerDisplay（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：ゲーム全体の残り時間表示
/// 主機能：
/// - GameManager のイベントを購読してタイマー制御
/// - 毎フレーム残り時間を更新
/// - TextMeshPro で表示
/// - StringBuilder で GC 削減
/// 
/// 注意：ゲーム終了判定は行わない（PhaseManager が担当）
/// 
/// ========================================
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _timerText;
    
    private float _remainingTime = 0f;
    private bool _isRunning = false;
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        // GameManager のイベントを購読
        GameManager.OnGameStart.AddListener(OnGameStart);
        GameManager.OnShowTitle.AddListener(OnShowTitle);
    }
    
    /// <summary>
    /// ゲーム開始時のハンドラ
    /// </summary>
    private void OnGameStart()
    {
        _remainingTime = GameConstants.GAME_DURATION;
        _isRunning = true;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[TimerDisplay] Timer started: {_remainingTime}s");
    }
    
    /// <summary>
    /// タイトル表示時のハンドラ
    /// </summary>
    private void OnShowTitle()
    {
        _isRunning = false;
        _remainingTime = 0f;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[TimerDisplay] Timer stopped");
    }
    
    void Update()
    {
        if (!_isRunning || _timerText == null) return;
        
        // 残り時間を減算
        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(0f, _remainingTime);
        
        // 表示更新
        _stringBuilder.Clear();
        _stringBuilder.Append(Mathf.CeilToInt(_remainingTime));
        _stringBuilder.Append("s");
        _timerText.text = _stringBuilder.ToString();
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (GameManager.OnGameStart != null)
            GameManager.OnGameStart.RemoveListener(OnGameStart);
        if (GameManager.OnShowTitle != null)
            GameManager.OnShowTitle.RemoveListener(OnShowTitle);
    }
}
