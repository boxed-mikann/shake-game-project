using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ========================================
/// KeyboardInputReader（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：キーボード入力（デバッグ用）
/// 実装：IInputSource インターフェース
/// 主機能：
/// - Input.GetKeyDown(KeyCode.Space) 等でシェイク検出
/// - OnShakeDetected イベント発火
/// 
/// ========================================
/// </summary>
public class KeyboardInputReader : MonoBehaviour, IInputSource
{
    private UnityEvent _onShakeDetected = new UnityEvent();
    public UnityEvent OnShakeDetected => _onShakeDetected;
    
    [SerializeField] private KeyCode _shakeKey = KeyCode.Space;
    
    private bool _isListening = false;
    
    public bool IsConnected => _isListening;
    
    void Start()
    {
        // GameManager.OnGameStart を購読して接続開始
        GameManager.OnGameStart.AddListener(Connect);
        GameManager.OnGameOver.AddListener(Disconnect);
    }
    
    /// <summary>
    /// 入力監視開始
    /// </summary>
    public void Connect()
    {
        _isListening = true;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[KeyboardInputReader] Started listening (Key: {_shakeKey})");
    }
    
    /// <summary>
    /// 入力監視停止
    /// </summary>
    public void Disconnect()
    {
        _isListening = false;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[KeyboardInputReader] Stopped listening");
    }
    
    /// <summary>
    /// 毎フレームキー入力チェック
    /// </summary>
    void Update()
    {
        if (!_isListening)
            return;
        
        // フリーズ中は入力無視
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFrozen)
            return;
        
        // スペースキー（またはカスタムキー）でシェイク検出
        if (Input.GetKeyDown(_shakeKey))
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[KeyboardInputReader] Shake detected (Key: {_shakeKey})");
            
            OnShakeDetected.Invoke();
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (GameManager.OnGameStart != null)
            GameManager.OnGameStart.RemoveListener(Connect);
        if (GameManager.OnGameOver != null)
            GameManager.OnGameOver.RemoveListener(Disconnect);
    }
}