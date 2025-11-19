using UnityEngine;

/// <summary>
/// シェイク入力を現在のハンドラーに振り分け（直接呼び出し方式）
/// Strategyパターン：フェーズ変更時にハンドラーを差し替え
/// </summary>
public class ShakeResolver : MonoBehaviour
{
    [Header("Input Sources")]
    [SerializeField] private SerialInputReader _serialInput;
    [SerializeField] private KeyboardInputReader _keyboardInput;
    
    [Header("Shake Handlers")]
    [SerializeField] private NoteShakeHandler _noteHandler;  // 音符用
    [SerializeField] private RestShakeHandler _restHandler;  // 休符用
    
    private IShakeHandler _currentHandler;
    private IInputSource _activeInputSource;
    
    void Start()
    {
        // DEBUG_MODEに応じて入力ソースを選択
        _activeInputSource = GameConstants.DEBUG_MODE 
            ? (IInputSource)_keyboardInput 
            : _serialInput;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Input source: {_activeInputSource.GetType().Name}");
        
        // 入力ソース接続
        _activeInputSource?.Connect();
        
        // PhaseManager購読
        if (PhaseManager.Instance != null)
        {
            PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
        else
        {
            Debug.LogError("[ShakeResolver] PhaseManager instance not found!");
        }
        
        // GameManager.OnShowTitle購読
        GameManager.OnShowTitle.AddListener(ResetResolver);
    }
    
    /// <summary>
    /// ShakeResolverの状態をリセット
    /// </summary>
    private void ResetResolver()
    {
        // 入力キューをクリア（TryDequeueで全て取り出す）
        if (_activeInputSource != null)
        {
            while (_activeInputSource.TryDequeue(out _)) { }
        }
        
        // ハンドラーをデフォルト状態に戻す
        _currentHandler = null;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[ShakeResolver] Reset to initial state");
    }
    
    void Update()
    {
        // ★ UnityEventを経由せず、直接キューから取り出して処理（最速）
        if (_activeInputSource != null)
        {
            while (_activeInputSource.TryDequeue(out var input))
            {
                // ★ 直接ハンドラー呼び出し（分岐なし・最速）
                _currentHandler?.HandleShake(input.data, input.timestamp);
            }
        }
    }
    
    /// <summary>
    /// フェーズ変更時のハンドラー切り替え
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData data)
    {
        // フェーズタイプに応じてハンドラーを差し替え
        // ★ここで1回だけ切り替え、以後の入力処理では分岐不要
        switch (data.phaseType)
        {
            case Phase.NotePhase:
                _currentHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
                break;
                
            case Phase.LastSprintPhase:
                _currentHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
                break;
                
            case Phase.RestPhase:
                _currentHandler = _restHandler;
                break;
                
            default:
                Debug.LogWarning($"[ShakeResolver] Unknown phase type: {data.phaseType}");
                break;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Handler switched to: {_currentHandler?.GetType().Name}");
    }
    
    void OnDestroy()
    {
        // 入力ソース切断
        _activeInputSource?.Disconnect();
        
        // イベント購読解除
        if (PhaseManager.Instance != null)
        {
            PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
        
        GameManager.OnShowTitle.RemoveListener(ResetResolver);
    }
}