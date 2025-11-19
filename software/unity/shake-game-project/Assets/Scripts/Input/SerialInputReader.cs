using UnityEngine;
using System.Threading;

/// <summary>
/// ========================================
/// SerialInputReader（統一キュー方式）
/// ========================================
/// 
/// 責務：シリアルポートから入力読み込み（スレッド化）
/// 主機能：
/// - バックグラウンドスレッドでシリアル読み込み
/// - ShakeResolver.EnqueueInput()で統一キューに追加
/// - ブロッキングReadLine()でCPU使用率削減
/// 
/// ========================================
/// </summary>
public class SerialInputReader : MonoBehaviour
{
    private Thread _readThread;
    private bool _isRunning = false;
    
    void Start()
    {
        // ゲーム開始時にスレッド開始
        StartReadThread();
    }
    
    /// <summary>
    /// 入力読み込みスレッド開始
    /// </summary>
    private void StartReadThread()
    {
        if (_isRunning) return;
        
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
    private void StopReadThread()
    {
        _isRunning = false;
        
        // ReadLine()のブロックを解除するためポート切断
        if (SerialPortManager.Instance != null)
        {
            SerialPortManager.Instance.Disconnect();
        }
        
        if (_readThread != null && _readThread.IsAlive)
        {
            _readThread.Join(2000);  // 最大2秒待機
        }
    }
    
    /// <summary>
    /// バックグラウンドスレッドでシリアル読み込み
    /// ブロッキング動作：ReadLine()がデータ到着まで待機
    /// </summary>
    private void ReadThreadLoop()
    {
        while (_isRunning)
        {
            try
            {
                // ★ 接続チェックを残すが、未接続時は待機
                if (SerialPortManager.Instance == null || !SerialPortManager.Instance.IsConnected)
                {
                    Thread.Sleep(500);  // 未接続時のみ待機
                    continue;
                }
                
                // ★ ブロッキング待機：データが来るまでここで待つ
                string data = SerialPortManager.Instance.ReadLine();
                if (!string.IsNullOrEmpty(data))
                {
                    double timestamp = AudioSettings.dspTime;
                    ShakeResolver.EnqueueInput(data.Trim(), timestamp);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SerialInputReader] Thread error: {ex.Message}");
                Thread.Sleep(500);  // エラー時のみ待機
            }
        }
    }
    
    void OnDestroy()
    {
        StopReadThread();
    }
    
    void OnDisable()
    {
        StopReadThread();
    }
    
    void OnApplicationQuit()
    {
        StopReadThread();
    }
}