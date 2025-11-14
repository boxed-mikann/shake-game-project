using UnityEngine;
using TMPro;

/// <summary>
/// デバッグ用コンソール表示
/// </summary>
public class DebugConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _debugText;
    
    private string _debugInfo = "";
    
    private void Start()
    {
        // DebugText を自動検索
        if (_debugText == null)
        {
            _debugText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        GameManager.Instance.OnGameTick += UpdateDebugInfo;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[DebugConsole] Initialized");
        }
    }
    
    private void UpdateDebugInfo(float elapsedTime)
    {
        if (_debugText == null)
            return;
        
        GameManager gm = GameManager.Instance;
        InputManager im = InputManager.Instance;
        
        _debugInfo = $"=== DEBUG INFO ===\n";
        _debugInfo += $"GameState: {gm.CurrentState}\n";
        _debugInfo += $"GamePhase: {gm.CurrentPhase}\n";
        _debugInfo += $"ElapsedTime: {elapsedTime:F2}s\n";
        _debugInfo += $"BossHP: {gm.BossCurrentHP:F0}/{gm.BossMaxHP:F0}\n";
        _debugInfo += $"Players: {gm.Players.Count}\n";
        _debugInfo += $"TotalScore: {gm.TotalScore}\n";
        _debugInfo += $"TotalShakes: {gm.TotalShakeCount}\n";
        _debugInfo += $"SyncSuccess: {gm.TotalSyncSuccessCount}\n";
        _debugInfo += $"SerialConnected: {im.IsSerialConnected}\n";
        
        if (TimingSystem.Instance != null)
        {
            _debugInfo += $"BeatProgress: {TimingSystem.Instance.BeatProgress:F2}\n";
        }
        
        _debugInfo += "\n[1-9] Simulate shake input\n";
        _debugInfo += "[Enter] Start game\n";
        
        _debugText.text = _debugInfo;
    }
    
    private void Update()
    {
        // ゲーム開始トリガー
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (GameManager.Instance.CurrentState == GameState.Title)
            {
                GameManager.Instance.StartGame();
            }
        }
    }
}
