using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

public class SerialManager : MonoBehaviour
{
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float reconnectInterval = 5f;
    
    private SerialPort serialPort;
    private Queue<string> receivedMessages = new Queue<string>();
    private Thread serialReadThread;
    private bool isRunning = false;
    private bool portOpenFailed = false;
    
    private float timeSinceLastReconnectAttempt = 0f;
    private bool isConnected = false;
    private bool wasConnectedLastFrame = false;
    
    private static SerialManager instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        OpenSerialPort();
    }
    
    void Update()
    {
        CheckConnectionHealth();
        AttemptReconnect();
        
        if (isConnected != wasConnectedLastFrame)
        {
            if (isConnected)
                Debug.Log($"✅ Serial Connected: {portName}");
            else
                Debug.LogWarning($"❌ Serial Disconnected: {portName}");
            
            wasConnectedLastFrame = isConnected;
        }
    }
    
    void OpenSerialPort()
    {
        try
        {
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
    
    private void CheckConnectionHealth()
    {
        bool currentHealth = serialPort != null && serialPort.IsOpen && isRunning;
        
        if (isConnected && !currentHealth)
        {
            isConnected = false;
            Debug.LogWarning("⚠️ Connection lost!");
        }
    }
    
    private void AttemptReconnect()
    {
        if (isConnected)
            return;
        
        timeSinceLastReconnectAttempt += Time.deltaTime;
        
        if (timeSinceLastReconnectAttempt >= reconnectInterval)
        {
            timeSinceLastReconnectAttempt = 0f;
            
            if (debugMode)
                Debug.Log($"🔄 Reconnect attempt...");
            
            OpenSerialPort();
        }
    }
    
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
    /// ★ ESP32 にコマンドを送信（新機能）
    /// </summary>
    public void SendCommand(string command)
    {
        if (!isConnected || serialPort == null || !serialPort.IsOpen)
        {
            Debug.LogWarning("❌ Serial port not connected. Cannot send command.");
            return;
        }
        
        try
        {
            // チェックサム付きでコマンド送信
            string commandWithChecksum = SerialProtocol.AddChecksum(command);
            serialPort.WriteLine(commandWithChecksum);
            
            Debug.Log($"📤 Sent: {command}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to send command: {e.Message}");
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
    
    public void ManualReconnect()
    {
        Debug.Log("🔌 Manual reconnect triggered...");
        OpenSerialPort();
    }
    
    public bool IsConnected() => isConnected;
    
    public void ChangePort(string newPortName)
    {
        portName = newPortName;
        Debug.Log($"Port changed to: {newPortName}");
        OpenSerialPort();
    }
    
    public static SerialManager Instance => instance;
    
    void OnDestroy()
    {
        isRunning = false;
        
        if (serialReadThread != null && serialReadThread.IsAlive)
            serialReadThread.Join(1000);
        
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
        }
    }
}