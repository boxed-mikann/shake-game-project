using UnityEngine;

/// <summary>
/// ========================================
/// PanelController（新アーキテクチャ版）
/// ========================================
/// 
/// 責補：画面パネル表示・非表示（Title/Play/Result）
/// 主機能：
/// - GameManager.OnGameStart を購読 → Play パネルアクティベート
/// - GameManager.OnGameOver を購読 → Result パネルアクティベート
/// - CanvasGroup で見た目制御
/// 
/// ========================================
/// </summary>
public class PanelController : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private CanvasGroup _titlePanel;
    [SerializeField] private CanvasGroup _playPanel;
    [SerializeField] private CanvasGroup _resultPanel;
    
    void Start()
    {
        // GameManager のイベントを購読
        GameManager.OnGameStart.AddListener(OnGameStart);
        GameManager.OnGameOver.AddListener(OnGameOver);
        
        // 初期状態：タイトルパネルのみ表示
        ShowPanel(_titlePanel);
        HidePanel(_playPanel);
        HidePanel(_resultPanel);
    }
    
    /// <summary>
    /// ゲーム開始時のハンドラ
    /// </summary>
    private void OnGameStart()
    {
        HidePanel(_titlePanel);
        ShowPanel(_playPanel);
        HidePanel(_resultPanel);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PanelController] Game started - showing Play panel");
    }
    
    /// <summary>
    /// ゲーム終了時のハンドラ
    /// </summary>
    private void OnGameOver()
    {
        HidePanel(_titlePanel);
        HidePanel(_playPanel);
        ShowPanel(_resultPanel);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PanelController] Game over - showing Result panel");
    }
    
    /// <summary>
    /// パネルを表示
    /// </summary>
    private void ShowPanel(CanvasGroup panel)
    {
        if (panel != null)
        {
            panel.alpha = 1f;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
    }
    
    /// <summary>
    /// パネルを非表示
    /// </summary>
    private void HidePanel(CanvasGroup panel)
    {
        if (panel != null)
        {
            panel.alpha = 0f;
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (GameManager.OnGameStart != null)
            GameManager.OnGameStart.RemoveListener(OnGameStart);
        if (GameManager.OnGameOver != null)
            GameManager.OnGameOver.RemoveListener(OnGameOver);
    }
}