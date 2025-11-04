using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

public class SerialManager : MonoBehaviour
{
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private bool debugMode = true;
    
    // 再接続設定
    [SerializeField] private float reconnectInterval = 5f;  // 5秒ごとに再接続試行
    [SerializeField] private int maxReconnectAttempts = 3;   // 1回のサイクルで最大3回試行
    
    private SerialPort serialPort;
    private Queue<string> receivedMessages = new Queue<string>();
    private Thread serialReadThread;
    private bool isRunning = false;
    private bool portOpenFailed = false;
    
    // 再接続用
    private float timeSinceLastReconnectAttempt = 0f;
    private int reconnectAttemptCount = 0;
    
    // ★ 接続状態フラグ
    private bool isConnected = false;
    private bool wasConnectedLastFrame = false;
    
    void Start()
    {
        OpenSerialPort();
    }
    
    void Update()
    {
        // 接続状態の監視
        CheckConnectionHealth();
        
        // 定期的に再接続を試行
        AttemptReconnect();
        
        // 接続状態が変わった時にログ出力
        if (isConnected != wasConnectedLastFrame)
        {
            if (isConnected)
                Debug.Log($"✅ Serial Port Connected: {portName}");
            else
                Debug.LogWarning($"❌ Serial Port Disconnected: {portName}");
            
            wasConnectedLastFrame = isConnected;
        }
        
        // メインスレッドでメッセージを処理
        ProcessReceivedMessages();
    }
    
    /// <summary>
    /// ポートをオープンしてバックグラウンドスレッドを起動
    /// </summary>
    void OpenSerialPort()
    {
        try
        {
            // 既に開いている場合はクローズ
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                Thread.Sleep(100);
            }
            
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
            serialPort.Open();
            
            isRunning = true;
            isConnected = true;
            portOpenFailed = false;
            reconnectAttemptCount = 0;
            
            // バックグラウンドスレッドで読み込み開始
            if (serialReadThread == null || !serialReadThread.IsAlive)
            {
                serialReadThread = new Thread(ReadSerialData);
                serialReadThread.IsBackground = true;
                serialReadThread.Start();
            }
        }
        catch (System.Exception e)
        {
            if (!portOpenFailed)
            {
                Debug.LogError($"❌ Failed to open serial port '{portName}': {e.Message}");
                portOpenFailed = true;
            }
            
            isConnected = false;
        }
    }
    
    /// <summary>
    /// ポート接続状態をチェック
    /// </summary>
    private void CheckConnectionHealth()
    {
        bool currentHealth = serialPort != null && serialPort.IsOpen && isRunning;
        
        if (isConnected && !currentHealth)
        {
            isConnected = false;
            Debug.LogWarning("⚠️ Connection lost!");
        }
    }
    
    /// <summary>
    /// 定期的に再接続を試行
    /// </summary>
    private void AttemptReconnect()
    {
        if (isConnected)
            return; // 既に接続されている
        
        timeSinceLastReconnectAttempt += Time.deltaTime;
        
        if (timeSinceLastReconnectAttempt >= reconnectInterval)
        {
            timeSinceLastReconnectAttempt = 0f;
            
            for (int i = 0; i < maxReconnectAttempts; i++)
            {
                reconnectAttemptCount++;
                
                if (debugMode)
                    Debug.Log($"🔄 Reconnect attempt #{reconnectAttemptCount}...");
                
                OpenSerialPort();
                
                if (isConnected)
                {
                    Debug.Log($"✅ Reconnection successful!");
                    return;
                }
                
                Thread.Sleep(100);
            }
            
            if (debugMode)
                Debug.LogWarning($"⚠️ Reconnection failed after {maxReconnectAttempts} attempts");
        }
    }
    
    /// <summary>
    /// バックグラウンドスレッドでシリアルデータを読み込む
    /// </summary>
    private void ReadSerialData()
    {
        while (isRunning)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        string line = serialPort.ReadLine();
                        lock (receivedMessages)
                        {
                            receivedMessages.Enqueue(line);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            catch (System.TimeoutException)
            {
                Thread.Sleep(10);
            }
            catch (System.Exception e)
            {
                if (debugMode && isRunning)
                    Debug.LogWarning($"⚠️ Serial read error: {e.Message}");
                break;
            }
        }
    }
    
    /// <summary>
    /// メインスレッドでメッセージを処理
    /// </summary>
    private void ProcessReceivedMessages()
    {
        lock (receivedMessages)
        {
            while (receivedMessages.Count > 0)
            {
                string data = receivedMessages.Dequeue();
                if (debugMode) Debug.Log($"📨 Received: {data}");
            }
        }
    }
    
    /// <summary>
    /// 外部からメッセージキューを取得
    /// </summary>
    public Queue<string> GetReceivedMessages()
    {
        lock (receivedMessages)
        {
            var copy = new Queue<string>(receivedMessages);
            receivedMessages.Clear();
            return copy;
        }
    }
    
    // ★ 新機能：再接続ボタン用の手動接続メソッド
    public void ManualReconnect()
    {
        Debug.Log("🔌 Manual reconnect triggered...");
        OpenSerialPort();
    }
    
    // ★ 新機能：接続状態を外部から取得
    public bool IsConnected()
    {
        return isConnected;
    }
    
    // ★ 新機能：ポート名を動的に変更
    public void ChangePort(string newPortName)
    {
        portName = newPortName;
        Debug.Log($"Port changed to: {newPortName}");
        OpenSerialPort();
    }
    
    void OnDestroy()
    {
        isRunning = false;
        
        if (serialReadThread != null && serialReadThread.IsAlive)
        {
            serialReadThread.Join(1000);
        }
        
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
            if (debugMode) Debug.Log("🔌 Serial Port Closed");
        }
    }
}