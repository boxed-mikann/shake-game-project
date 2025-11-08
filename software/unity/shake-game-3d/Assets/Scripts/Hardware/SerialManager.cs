using System;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ESP32 親機との Serial 通信を管理
/// 
/// フォーマット: childID,shakeCount,acceleration
/// 例: 0,1,12345
/// </summary>
public class SerialManager : MonoBehaviour
{
    [SerializeField] private string portName = "COM3";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private int receiveTimeout = 1000;

    private SerialPort serialPort;
    private bool isConnected = false;

    // イベント
    public delegate void OnShakeDataReceived(int childID, int shakeCount, int acceleration);
    public event OnShakeDataReceived OnShake;

    // シングルトン
    private static SerialManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        ConnectToSerial();
    }

    private void Update()
    {
        if (isConnected && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    string data = serialPort.ReadLine();
                    ParseData(data);
                }
            }
            catch (TimeoutException)
            {
                // タイムアウト（通常）
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Serial read error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Serial ポートに接続
    /// </summary>
    public void ConnectToSerial()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = receiveTimeout;
            serialPort.WriteTimeout = receiveTimeout;
            serialPort.Open();
            isConnected = true;

            Debug.Log($"Serial connected: {portName} @ {baudRate} baud");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to connect to serial port: {ex.Message}");
            isConnected = false;
        }
    }

    /// <summary>
    /// Serial ポートから切断
    /// </summary>
    public void DisconnectSerial()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            serialPort.Dispose();
            isConnected = false;
            Debug.Log("Serial disconnected");
        }
    }

    /// <summary>
    /// データをパース（childID,shakeCount,acceleration）
    /// </summary>
    private void ParseData(string data)
    {
        try
        {
            string[] parts = data.Split(',');

            if (parts.Length == 3)
            {
                int childID = int.Parse(parts[0]);
                int shakeCount = int.Parse(parts[1]);
                int acceleration = int.Parse(parts[2]);

                Debug.Log($"Shake detected - Child #{childID} | Count: {shakeCount} | Accel: {acceleration}");

                // イベント発火
                OnShake?.Invoke(childID, shakeCount, acceleration);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Parse error: {data} - {ex.Message}");
        }
    }

    /// <summary>
    /// 親機にコマンドを送信（テスト用）
    /// </summary>
    public void SendCommand(string command)
    {
        try
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.WriteLine(command);
                Debug.Log($"Sent command: {command}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to send command: {ex.Message}");
        }
    }

    /// <summary>
    /// 接続状態を取得
    /// </summary>
    public bool IsConnected
    {
        get { return isConnected && serialPort != null && serialPort.IsOpen; }
    }

    private void OnDestroy()
    {
        DisconnectSerial();
    }
}
