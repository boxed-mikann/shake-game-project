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
/// - Slider で進行度を毎フレーム更新（1→0に減っていく）
/// - フェーズごとに色を変更（視覚的な区別）
/// 
/// ========================================
/// </summary>
public class PhaseProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider _progressSlider;
    
    [Header("Phase Colors")]
    [SerializeField] private Color _notePhaseColor = new Color(0.3f, 0.5f, 1f);      // 青系
    [SerializeField] private Color _restPhaseColor = new Color(1f, 0.6f, 0.2f);      // オレンジ系
    [SerializeField] private Color _lastSprintColor = new Color(1f, 0.2f, 0.2f);     // 赤系
    
    private float _totalDuration = 0f;
    private float _remainingTime = 0f;
    private bool _isTracking = false;
    private Image _fillImage;
    
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
            _progressSlider.value = 1f;  // 初期値を1に（満タン状態から開始）
            
            // Fill部分のImageコンポーネント取得（色変更用）
            if (_progressSlider.fillRect != null)
            {
                _fillImage = _progressSlider.fillRect.GetComponent<Image>();
                if (_fillImage == null)
                {
                    Debug.LogWarning("[PhaseProgressBar] Slider fillRect does not have an Image component!");
                }
            }
            else
            {
                Debug.LogWarning("[PhaseProgressBar] Slider does not have a fillRect assigned!");
            }
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
        
        // フェーズに応じた色変更
        if (_fillImage != null)
        {
            switch (data.phaseType)
            {
                case Phase.NotePhase:
                    _fillImage.color = _notePhaseColor;
                    if (GameConstants.DEBUG_MODE)
                        Debug.Log("[PhaseProgressBar] Color changed to Note Phase (Blue)");
                    break;
                case Phase.RestPhase:
                    _fillImage.color = _restPhaseColor;
                    if (GameConstants.DEBUG_MODE)
                        Debug.Log("[PhaseProgressBar] Color changed to Rest Phase (Orange)");
                    break;
                case Phase.LastSprintPhase:
                    _fillImage.color = _lastSprintColor;
                    if (GameConstants.DEBUG_MODE)
                        Debug.Log("[PhaseProgressBar] Color changed to Last Sprint Phase (Red)");
                    break;
            }
        }
        
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
        
        // ★ 修正: 進行度計算を反転（1→0に減っていく）
        float progress = 0f;
        if (_totalDuration > 0f)
        {
            progress = _remainingTime / _totalDuration;  // 残り時間の割合（1→0に減る）
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