using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// ========================================
/// SerialInputReader（直接呼び出し方式）
/// ========================================
/// 
/// 責務：シリアルポートから入力読み込み（スレッド化）
/// 実装：IInputSource インターフェース
/// 主機能：
/// - バックグラウンドスレッドでシリアル読み込み
/// - ConcurrentQueue<(string data, double timestamp)> でタイムスタンプ付きデータをキューイング
/// - メインスレッドでの直接アクセス（TryDequeue）
/// - UnityEvent廃止で約3倍高速化
/// 
/// ========================================
/// </summary>
public class SerialInputReader : MonoBehaviour, IInputSource
{
    private ConcurrentQueue<(string data, double timestamp)> _inputQueue = new ConcurrentQueue<(string data, double timestamp)>();
    private Thread _readThread;
    private bool _isRunning = false;
    
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
                        // タイムスタンプ付きでキューに格納
                        double timestamp = AudioSettings.dspTime;
                        _inputQueue.Enqueue((data.Trim(), timestamp));
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