using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フェーズインジケータースライダー
/// 現在のフェーズと残り時間をビジュアルで表示
/// フェーズによってスライダーの色を切り替え
/// スライダーは減っていく方向（残り時間をビジュアル表示）
/// ラストスパート時は表示も「ラストスパート！」に
/// </summary>
public class PhaseIndicatorSlider : MonoBehaviour
{
    [SerializeField] private Slider phaseSlider;                    // フェーズ進度スライダー
    [SerializeField] private Text phaseLabel;                       // フェーズ表示ラベル
    [SerializeField] private Color notePhaseColor = new Color(1f, 0.7f, 0f);      // 音符フェーズの色（オレンジ）
    [SerializeField] private Color restPhaseColor = new Color(0.3f, 0.8f, 1f);    // 休符フェーズの色（シアン）
    [SerializeField] private Color lastSprintColor = new Color(1f, 0.2f, 0.2f);   // ラストスパートの色（赤）
    
    private Image _fillImage;
    private Phase _currentPhase = Phase.NotePhase;
    private bool _isLastSprint = false;
    
    private void Start()
    {
        if (phaseSlider == null)
        {
            phaseSlider = GetComponent<Slider>();
        }
        
        if (phaseSlider == null)
        {
            Debug.LogError("[PhaseIndicatorSlider] Slider component not found!");
            return;
        }
        
        // スライダーを0～1で逆方向に設定（満タン=残り時間100%）
        phaseSlider.minValue = 0f;
        phaseSlider.maxValue = 1f;
        
        _fillImage = phaseSlider.fillRect.GetComponent<Image>();
        if (_fillImage == null)
        {
            Debug.LogWarning("[PhaseIndicatorSlider] Fill Image not found, trying to get from fillRect");
            _fillImage = phaseSlider.GetComponentInChildren<Image>();
        }
        
        // フェーズ変更イベント購読
        if (PhaseController.Instance != null)
        {
            PhaseController.Instance.OnPhaseChanged += OnPhaseChanged;
            // 初期フェーズを設定
            _currentPhase = PhaseController.Instance.GetCurrentPhase();
            UpdateSliderColor(_currentPhase);
            UpdatePhaseLabel(_currentPhase);
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PhaseIndicatorSlider] ✅ Initialized");
        }
    }
    
    private void Update()
    {
        if (phaseSlider == null || !PhaseController.Instance)
            return;
        
        // フェーズの残り時間を表示（逆方向）
        float progress = PhaseController.Instance.GetPhaseProgress();
        phaseSlider.value = 1f - progress;  // 逆方向にする（減っていく）
        
        // ラストスパート判定（GameManagerから）
        if (GameManager.Instance)
        {
            bool isLastSprintNow = GameManager.Instance.GameTimer <= GameConstants.LAST_SPRINT_DURATION;
            
            if (isLastSprintNow && !_isLastSprint)
            {
                _isLastSprint = true;
                UpdateSliderColor(Phase.LastSprintPhase);
                UpdatePhaseLabel(Phase.LastSprintPhase);
                if (GameConstants.DEBUG_MODE)
                    Debug.Log("[PhaseIndicatorSlider] ⚡ LastSprint activated!");
            }
            else if (!isLastSprintNow && _isLastSprint)
            {
                _isLastSprint = false;
                Phase normalPhase = PhaseController.Instance.GetCurrentPhase();
                if (normalPhase != Phase.LastSprintPhase)
                {
                    UpdateSliderColor(normalPhase);
                    UpdatePhaseLabel(normalPhase);
                }
                if (GameConstants.DEBUG_MODE)
                    Debug.Log("[PhaseIndicatorSlider] ⚡ LastSprint ended");
            }
        }
    }
    
    /// <summary>
    /// フェーズ変更時のコールバック
    /// </summary>
    private void OnPhaseChanged(Phase newPhase)
    {
        // ラストスパート中はラベル以外の更新をスキップ
        if (_isLastSprint)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[PhaseIndicatorSlider] Phase changed to {newPhase}, but LastSprint is active, ignoring");
            return;
        }
        
        _currentPhase = newPhase;
        UpdateSliderColor(newPhase);
        UpdatePhaseLabel(newPhase);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[PhaseIndicatorSlider] 🔄 Phase changed to {newPhase}");
        }
    }
    
    /// <summary>
    /// フェーズに応じてスライダーのFill色を更新
    /// </summary>
    private void UpdateSliderColor(Phase phase)
    {
        Color newColor = phase switch
        {
            Phase.NotePhase => notePhaseColor,
            Phase.RestPhase => restPhaseColor,
            Phase.LastSprintPhase => lastSprintColor,
            _ => notePhaseColor
        };
        
        SetFillColor(newColor);
    }
    
    /// <summary>
    /// フェーズラベルを更新
    /// </summary>
    private void UpdatePhaseLabel(Phase phase)
    {
        if (phaseLabel == null)
            return;
        
        phaseLabel.text = phase switch
        {
            Phase.NotePhase => "音符フェーズ",
            Phase.RestPhase => "休符フェーズ",
            Phase.LastSprintPhase => "🔥 ラストスパート！",
            _ => "不明"
        };
    }
    
    /// <summary>
    /// Fill画像の色を直接設定
    /// </summary>
    private void SetFillColor(Color color)
    {
        if (_fillImage != null)
        {
            _fillImage.color = color;
        }
    }
    
    /// <summary>
    /// ゲーム再開時に状態をリセット
    /// </summary>
    public void Reset()
    {
        _isLastSprint = false;
        _currentPhase = Phase.NotePhase;
        phaseSlider.value = 1f;
        UpdateSliderColor(Phase.NotePhase);
        UpdatePhaseLabel(Phase.NotePhase);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PhaseIndicatorSlider] 🔄 Reset called");
        }
    }
    
    private void OnDestroy()
    {
        if (PhaseController.Instance != null)
        {
            PhaseController.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }
}
