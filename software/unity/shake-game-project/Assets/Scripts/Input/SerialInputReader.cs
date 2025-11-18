using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// ========================================
/// SerialInputReader（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：シリアルポートから入力読み込み（スレッド化）
/// 実装：IInputSource インターフェース
/// 主機能：
/// - バックグラウンドスレッドでシリアル読み込み
/// - ConcurrentQueue<string> で入力データをキューイング
/// - メインスレッドでの安全な読み取り
/// 
/// ========================================
/// </summary>
public class SerialInputReader : MonoBehaviour, IInputSource
{
    public UnityEvent OnShakeDetected { get; private set; } = new UnityEvent();
    
    private ConcurrentQueue<string> _inputQueue = new ConcurrentQueue<string>();
    private Thread _readThread;
    private bool _isRunning = false;
    
    public bool IsConnected => SerialPortManager.Instance != null && SerialPortManager.Instance.IsConnected;
    
    void Start()
    {
        // GameManager.OnGameStart を購読して接続開始
        GameManager.OnGameStart.AddListener(Connect);
        GameManager.OnGameOver.AddListener(Disconnect);
    }
    
    /// <summary>
    /// 入力読み込みスレッド開始
    /// </summary>
    public void Connect()
    {
        if (_isRunning)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[SerialInputReader] Already running");
            return;
        }
        
        _isRunning = true;
        _readThread = new Thread(ReadThreadLoop);
        _readThread.IsBackground = true;
        _readThread.Start();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[SerialInputReader] Started reading thread");
    }
    
    /// <summary>
    /// 入力読み込みスレッド停止
    /// </summary>
    public void Disconnect()
    {
        _isRunning = false;
        
        if (_readThread != null && _readThread.IsAlive)
        {
            _readThread.Join(1000); // 最大1秒待機
            _readThread = null;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[SerialInputReader] Stopped reading thread");
    }
    
    /// <summary>
    /// バックグラウンドスレッドでシリアル読み込み
    /// </summary>
    private void ReadThreadLoop()
    {
        while (_isRunning)
        {
            try
            {
                if (SerialPortManager.Instance != null && SerialPortManager.Instance.IsConnected)
                {
                    string data = SerialPortManager.Instance.ReadLine();
                    if (!string.IsNullOrEmpty(data))
                    {
                        _inputQueue.Enqueue(data.Trim());
                    }
                }
                else
                {
                    // 接続待機
                    Thread.Sleep(100);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SerialInputReader] Thread error: {ex.Message}");
                Thread.Sleep(500);
            }
        }
    }
    
    /// <summary>
    /// メインスレッドで入力処理（キューからデキュー）
    /// </summary>
    void Update()
    {
        if (!_isRunning)
            return;
        
        // キューから入力を取り出して処理
        while (_inputQueue.TryDequeue(out string data))
        {
            ProcessInput(data);
        }
    }
    
    /// <summary>
    /// 入力データ処理
    /// </summary>
    private void ProcessInput(string data)
    {
        // シェイク検出条件（旧実装を参考に）
        // 例："shake" や特定の文字列を検出
        if (data.Contains("shake") || data.Contains("1"))
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[SerialInputReader] Shake detected: {data}");
            
            OnShakeDetected.Invoke();
        }
    }
    
    void OnDestroy()
    {
        Disconnect();
        
        // イベント購読解除
        if (GameManager.OnGameStart != null)
            GameManager.OnGameStart.RemoveListener(Connect);
        if (GameManager.OnGameOver != null)
            GameManager.OnGameOver.RemoveListener(Disconnect);
    }
    
    void OnDisable()
    {
        Disconnect();
    }
    
    void OnApplicationQuit()
    {
        Disconnect();
    }
}