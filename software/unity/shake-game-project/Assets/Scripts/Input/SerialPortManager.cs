using UnityEngine;
using System.IO.Ports;
using System;

/// <summary>
/// ========================================
/// SerialPortManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：シリアルポート接続・管理
/// 主機能：
/// - SerialPort 接続・切断
/// - 再接続ロジック（SERIAL_RECONNECT_INTERVAL で定期試行）
/// - 接続状態の監視
/// 
/// ========================================
/// </summary>
public class SerialPortManager : MonoBehaviour
{
    public static SerialPortManager Instance { get; private set; }
    
    private SerialPort _serialPort;
    private float _reconnectTimer = 0f;
    private bool _isReconnecting = false;
    
    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Connect();
    }
    
    void Update()
    {
        // 再接続ロジック
        if (!IsConnected && !_isReconnecting)
        {
            _reconnectTimer += Time.deltaTime;
            if (_reconnectTimer >= GameConstants.SERIAL_RECONNECT_INTERVAL)
            {
                _reconnectTimer = 0f;
                Connect();
            }
        }
    }
    
    /// <summary>
    /// シリアルポート接続試行
    /// </summary>
    public void Connect()
    {
        if (IsConnected)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[SerialPortManager] Already connected");
            return;
        }
        
        _isReconnecting = true;
        
        try
        {
            // ポート存在確認
            string[] availablePorts = SerialPort.GetPortNames();
            bool portExists = false;
            foreach (string port in availablePorts)
            {
                if (port == GameConstants.SERIAL_PORT_NAME)
                {
                    portExists = true;
                    break;
                }
            }
            
            if (!portExists)
            {
                Debug.LogWarning($"[SerialPortManager] Port {GameConstants.SERIAL_PORT_NAME} not found. Available ports: {string.Join(", ", availablePorts)}");
                _isReconnecting = false;
                return;
            }
            
            // SerialPort 初期化
            _serialPort = new SerialPort(GameConstants.SERIAL_PORT_NAME, GameConstants.SERIAL_BAUD_RATE);
            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 100;
            _serialPort.Open();
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[SerialPortManager] Connected to {GameConstants.SERIAL_PORT_NAME} at {GameConstants.SERIAL_BAUD_RATE} baud");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerialPortManager] Connection failed: {ex.Message}");
            _serialPort = null;
        }
        finally
        {
            _isReconnecting = false;
        }
    }
    
    /// <summary>
    /// シリアルポート切断
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                _serialPort.Dispose();
                _serialPort = null;
                
                if (GameConstants.DEBUG_MODE)
                    Debug.Log("[SerialPortManager] Disconnected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerialPortManager] Disconnect error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// ポートからデータ読み込み（1行）
    /// </summary>
    public string ReadLine()
    {
        if (!IsConnected)
            return null;
        
        try
        {
            if (_serialPort.BytesToRead > 0)
            {
                return _serialPort.ReadLine();
            }
        }
        catch (TimeoutException)
        {
            // タイムアウトは正常（データがない場合）
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerialPortManager] ReadLine error: {ex.Message}");
            Disconnect();
        }
        
        return null;
    }
    
    void OnDestroy()
    {
        Disconnect();
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