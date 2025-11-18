using UnityEngine;

/// <summary>
/// ========================================
/// ShakeResolver（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：入力ルーティング（Strategy パターン）
/// 主機能：
/// - 現在フェーズに応じて IShakeHandler 実装を切り替え
/// - IInputSource からの入力を現在の Handler に振り分け
/// 
/// ========================================
/// </summary>
public class ShakeResolver : MonoBehaviour
{
    [Header("Input Source")]
    [SerializeField] private MonoBehaviour _inputSourceComponent; // IInputSource を実装した MonoBehaviour
    private IInputSource _inputSource;
    
    [Header("Phase Handlers")]
    [SerializeField] private Phase1ShakeHandler _phase1Handler;
    [SerializeField] private Phase2ShakeHandler _phase2Handler;
    [SerializeField] private Phase3ShakeHandler _phase3Handler;
    [SerializeField] private Phase4ShakeHandler _phase4Handler;
    [SerializeField] private Phase5ShakeHandler _phase5Handler;
    [SerializeField] private Phase6ShakeHandler _phase6Handler;
    [SerializeField] private Phase7ShakeHandler _phase7Handler;
    
    private IShakeHandler _currentHandler;
    
    void Awake()
    {
        // IInputSource の取得
        if (_inputSourceComponent != null)
        {
            _inputSource = _inputSourceComponent as IInputSource;
            if (_inputSource == null)
            {
                Debug.LogError("[ShakeResolver] Input source component does not implement IInputSource!");
            }
        }
        else
        {
            Debug.LogError("[ShakeResolver] Input source component is not assigned!");
        }
    }
    
    void Start()
    {
        // PhaseManager のフェーズ変更イベントを購読
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.OnPhaseChanged.AddListener(OnPhaseChanged);
        }
        else
        {
            Debug.LogError("[ShakeResolver] PhaseManager instance not found!");
        }
        
        // 入力ソースのシェイク検出イベントを購読
        if (_inputSource != null)
        {
            _inputSource.OnShakeDetected.AddListener(OnInputDetected);
        }
    }
    
    /// <summary>
    /// フェーズ変更時のハンドラ切り替え
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData data)
    {
        // phaseIndex に基づいて Handler を切り替え
        _currentHandler = data.phaseIndex switch
        {
            0 => _phase1Handler,
            1 => _phase2Handler,
            2 => _phase3Handler,
            3 => _phase4Handler,
            4 => _phase5Handler,
            5 => _phase6Handler,
            6 => _phase7Handler,
            _ => throw new System.InvalidOperationException($"Invalid phase index: {data.phaseIndex}")
        };
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Switched to Phase {data.phaseIndex + 1} Handler ({data.phaseType})");
    }
    
    /// <summary>
    /// 入力検出時のコールバック
    /// </summary>
    private void OnInputDetected()
    {
        // フリーズ中は入力無視
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFrozen)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[ShakeResolver] Input ignored (Frozen)");
            return;
        }
        
        // 現在のハンドラで処理
        if (_currentHandler != null)
        {
            _currentHandler.HandleShake();
        }
        else
        {
            Debug.LogWarning("[ShakeResolver] No handler assigned for current phase!");
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (PhaseManager.Instance != null && PhaseManager.Instance.OnPhaseChanged != null)
        {
            PhaseManager.Instance.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        }
        
        if (_inputSource != null && _inputSource.OnShakeDetected != null)
        {
            _inputSource.OnShakeDetected.RemoveListener(OnInputDetected);
        }
    }
}