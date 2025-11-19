using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ========================================
/// PhaseProgressBar（新アーキテクチャ版）
/// ========================================
/// 
/// 責補：フェーズ進行度バー表示
/// 主機能：
/// - PhaseManager.OnPhaseChanged を購読
/// - フェーズの duration を取得してローカルタイマー開始
/// - Slider で進行度を毎フレーム更新
/// 
/// ========================================
/// </summary>
public class PhaseProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _progressSlider;
    
    private float _totalDuration = 0f;
    private float _remainingTime = 0f;
    private bool _isTracking = false;
    
    void Start()
    {
        // PhaseManager のイベントを購読
        if (PhaseManager.Instance != null)
        {
            PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
        else
        {
            Debug.LogError("[PhaseProgressBar] PhaseManager instance not found!");
        }
        
        // スライダー初期化
        if (_progressSlider != null)
        {
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = 1f;
            _progressSlider.value = 0f;
        }
    }
    
    /// <summary>
    /// フェーズ変更時のハンドラ
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData data)
    {
        // duration を保存してタイマー開始
        _totalDuration = data.duration;
        _remainingTime = data.duration;
        _isTracking = true;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[PhaseProgressBar] Phase {data.phaseIndex + 1} started - duration: {data.duration}s");
    }
    
    /// <summary>
    /// 毎フレーム進行度を更新
    /// </summary>
    void Update()
    {
        if (!_isTracking || _progressSlider == null)
            return;
        
        // 残り時間を減算
        _remainingTime -= Time.deltaTime;
        _remainingTime = Mathf.Max(0f, _remainingTime);
        
        // 進行度計算（0.0 ～ 1.0）
        float progress = 0f;
        if (_totalDuration > 0f)
        {
            progress = (_totalDuration - _remainingTime) / _totalDuration;
        }
        
        // スライダー更新
        _progressSlider.value = progress;
        
        // タイマー終了チェック
        if (_remainingTime <= 0f)
        {
            _isTracking = false;
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[PhaseProgressBar] Phase timer completed");
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (PhaseManager.OnPhaseChanged != null)
        {
            PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
    }
}