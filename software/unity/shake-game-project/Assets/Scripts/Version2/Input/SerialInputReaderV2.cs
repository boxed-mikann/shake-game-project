using UnityEngine;
using System.Threading;

/// <summary>
/// ========================================
/// SerialInputReaderV2（Version2専用シリアル入力読み込み）
/// ========================================
/// 
/// 責務：シリアルポートから入力読み込み（スレッド化）
/// 主機能：
/// - バックグラウンドスレッドでシリアル読み込み
/// - InputQueueV2.Enqueue() で統一キューに追加
/// - ブロッキングReadLine() でCPU使用率削減
/// 
/// Version1との共通点：
/// - タイムスタンプ記録: AudioSettings.dspTime（スレッドセーフ、高精度）
/// - 入力先: InputQueueV2（Version2専用の統一キュー）
/// 
/// 設計上の利点：
/// - AudioSettings.dspTime はスレッドセーフ（バックグラウンドで使用可能）
/// - VideoPlayerの音声クロックと同期
/// - サンプル単位の高精度（44.1kHz = 約0.02ms）
/// 
/// ========================================
/// </summary>
public class SerialInputReaderV2 : MonoBehaviour
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
        
        if (GameConstantsV2.DEBUG_MODE)
            Debug.Log("[SerialInputReaderV2] Started reading thread");
    }
    
    /// <summary>
    /// 入力読み込みスレッド停止
    /// </summary>
    private void StopReadThread()
    {
        _isRunning = false;
        
        // ReadLine() のブロックを解除するためポート切断
        if (SerialPortManagerV2.Instance != null)
        {
            SerialPortManagerV2.Instance.Disconnect();
        }
        
        if (_readThread != null && _readThread.IsAlive)
        {
            _readThread.Join(2000);  // 最大2秒待機
        }
        
        if (GameConstantsV2.DEBUG_MODE)
            Debug.Log("[SerialInputReaderV2] Stopped reading thread");
    }
    
    /// <summary>
    /// バックグラウンドスレッドでシリアル読み込み
    /// ブロッキング動作：ReadLine() がデータ到着まで待機
    /// </summary>
    private void ReadThreadLoop()
    {
        while (_isRunning)
        {
            try
            {
                // 接続チェック
                if (SerialPortManagerV2.Instance == null || !SerialPortManagerV2.Instance.IsConnected)
                {
                    Thread.Sleep(500);  // 未接続時のみ待機
                    continue;
                }
                
                // ブロッキング待機：データが来るまでここで待つ
                string data = SerialPortManagerV2.Instance.ReadLine();
                if (!string.IsNullOrEmpty(data))
                {
                    // ★ AudioSettings.dspTime はスレッドセーフで高精度（Version1と同じ方式）
                    double timestamp = AudioSettings.dspTime;
                    
                    // InputQueueV2 に追加
                    InputQueueV2.Enqueue(data.Trim(), timestamp);
                }
            }
            catch (System.Exception ex)
            {
                // エラーはログ出力のみ（接続切れ等）
                if (_isRunning)  // 停止処理中のエラーは無視
                {
                    Debug.LogError($"[SerialInputReaderV2] Thread error: {ex.Message}");
                    Thread.Sleep(500);  // エラー時のみ待機
                }
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
