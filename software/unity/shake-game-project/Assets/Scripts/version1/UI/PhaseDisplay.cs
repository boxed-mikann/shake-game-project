using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// ========================================
/// PhaseDisplay（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：現在のフェーズ名表示
/// 主機能：
/// - PhaseManager.OnPhaseChanged を購読
/// - フェーズ名を TextMeshPro で表示
/// - StringBuilder で GC 削減
/// 
/// ========================================
/// </summary>
public class PhaseDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _phaseText;
    
    private StringBuilder _stringBuilder = new StringBuilder();
    
    void Start()
    {
        // PhaseManager のイベントを購読
        if (PhaseManager.Instance != null)
        {
            PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
        else
        {
            Debug.LogError("[PhaseDisplay] PhaseManager instance not found!");
        }
    }
    
    /// <summary>
    /// フェーズ変更時のハンドラ
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData data)
    {
        if (_phaseText == null)
        {
            Debug.LogWarning("[PhaseDisplay] Phase text is not assigned!");
            return;
        }
        
        // StringBuilder で文字列構築（GC 削減）
        _stringBuilder.Clear();
        _stringBuilder.Append(GetPhaseName(data.phaseType));
        _phaseText.text = _stringBuilder.ToString();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[PhaseDisplay] Phase changed to: {data.phaseType}");
    }
    
    /// <summary>
    /// フェーズタイプから表示名を取得
    /// </summary>
    private string GetPhaseName(Phase phase)
    {
        switch (phase)
        {
            case Phase.NotePhase: return "♪ 音符フェーズ";
            case Phase.RestPhase: return "💤 休符フェーズ";
            case Phase.LastSprintPhase: return "🔥 ラストスパート";
            default: return "不明";
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
