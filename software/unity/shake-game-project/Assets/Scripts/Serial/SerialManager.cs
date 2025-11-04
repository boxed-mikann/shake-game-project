using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

public class SerialManager : MonoBehaviour
{
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private bool debugMode = true;
    
    private SerialPort serialPort;
    private Queue<string> receivedMessages = new Queue<string>();
    private Thread serialReadThread;
    private bool isRunning = false;
    private bool portOpenFailed = false; // フラグ: エラーログ抑止用
    
    void Start()
    {
        OpenSerialPort();
    }
    
    void OpenSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 500;  // タイムアウト時間短縮
            serialPort.WriteTimeout = 500;
            serialPort.Open();
            isRunning = true;
            portOpenFailed = false;
            
            if (debugMode) Debug.Log($"✅ Serial Port Opened: {portName}");
            
            // バックグラウンドスレッドでシリアル読み込み
            serialReadThread = new Thread(ReadSerialData);
            serialReadThread.Start();
        }
        catch (System.Exception e)
        {
            if (!portOpenFailed)
            {
                Debug.LogError($"❌ Failed to open serial port: {e.Message}");
                portOpenFailed = true; // 一度だけエラー表示
            }
        }
    }
    
    /// <summary>
    /// バックグラウンドスレッドでシリアルデータを読み込む
    /// </summary>
    private void ReadSerialData()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
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
                    Thread.Sleep(10); // CPU使用率低下
                }
            }
            catch (System.TimeoutException)
            {
                // タイムアウトは無視
                Thread.Sleep(10);
            }
            catch (System.Exception e)
            {
                if (debugMode)
                    Debug.LogWarning($"⚠️ Serial read error: {e.Message}");
                break;
            }
        }
    }
    
    void Update()
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
    
    public Queue<string> GetReceivedMessages()
    {
        lock (receivedMessages)
        {
            var copy = new Queue<string>(receivedMessages);
            receivedMessages.Clear();
            return copy;
        }
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