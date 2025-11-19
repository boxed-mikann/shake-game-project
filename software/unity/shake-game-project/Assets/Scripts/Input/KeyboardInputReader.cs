using UnityEngine;
using System.Collections.Concurrent;

/// <summary>
/// ========================================
/// KeyboardInputReader（直接呼び出し方式）
/// ========================================
/// 
/// 責務：キーボード入力（デバッグ用）
/// 実装：IInputSource インターフェース
/// 主機能：
/// - Input.GetKeyDown(KeyCode.Space) 等でシェイク検出
/// - ConcurrentQueue<(string data, double timestamp)> でタイムスタンプ付きデータをキューイング
/// - メインスレッドでの直接アクセス（TryDequeue）
/// - UnityEvent廃止で約3倍高速化
/// 
/// ========================================
/// </summary>
public class KeyboardInputReader : MonoBehaviour, IInputSource
{
    private ConcurrentQueue<(string data, double timestamp)> _inputQueue = new ConcurrentQueue<(string data, double timestamp)>();
    
    [SerializeField] private KeyCode _shakeKey = KeyCode.Space;
    
    private bool _isListening = false;
    
    /// <summary>
    /// キューから入力データを取り出す（直接呼び出し方式）
    /// </summary>
    public bool TryDequeue(out (string data, double timestamp) input)
    {
        return _inputQueue.TryDequeue(out input);
    }
    
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
            // タイムスタンプ付きでキューに格納
            double timestamp = AudioSettings.dspTime;
            _inputQueue.Enqueue(("shake", timestamp));
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[KeyboardInputReader] Shake detected (Key: {_shakeKey}, Timestamp: {timestamp})");
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