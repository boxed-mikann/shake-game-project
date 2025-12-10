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
public class SerialPortManagerV2 : MonoBehaviour
{
    public static SerialPortManagerV2 Instance { get; private set; }
    
    private SerialPort _serialPort;
    private float _reconnectTimer = 0f;
    private bool _isReconnecting = false;
    
    // 現在設定されているポート名
    public string CurrentPortName { get; private set; }
    private const string PREF_KEY_PORT_NAME = "SerialPortName";

    public bool IsConnected => _serialPort != null && _serialPort.IsOpen;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 設定読み込み
            CurrentPortName = PlayerPrefs.GetString(PREF_KEY_PORT_NAME, GameConstantsV2.SERIAL_PORT_NAME);
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
            if (_reconnectTimer >= GameConstantsV2.SERIAL_RECONNECT_INTERVAL)
            {
                _reconnectTimer = 0f;
                Connect();
            }
        }
    }

    /// <summary>
    /// 利用可能なポート一覧を取得
    /// </summary>
    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }
    
    /// <summary>
    /// シリアルポート接続試行
    /// portNameを指定すると、そのポート設定を更新して接続を試みる
    /// </summary>
    public void Connect(string portName = null)
    {
        // ポート名が指定された場合、設定を更新して切断（再接続のため）
        if (!string.IsNullOrEmpty(portName) && portName != CurrentPortName)
        {
            Disconnect();
            CurrentPortName = portName;
            // 設定保存
            PlayerPrefs.SetString(PREF_KEY_PORT_NAME, CurrentPortName);
            PlayerPrefs.Save();
        }

        if (IsConnected)
        {
            if (GameConstantsV2.DEBUG_MODE)
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
                if (port == CurrentPortName)
                {
                    portExists = true;
                    break;
                }
            }
            
            if (!portExists)
            {
                Debug.LogWarning($"[SerialPortManager] Port {CurrentPortName} not found. Available ports: {string.Join(", ", availablePorts)}");
                _isReconnecting = false;
                return;
            }
            
            // SerialPort 初期化
            _serialPort = new SerialPort(CurrentPortName, GameConstantsV2.SERIAL_BAUD_RATE);
            _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;  // ★ 変更：ブロッキング待機
            _serialPort.WriteTimeout = 100;
            _serialPort.Open();
            
            if (GameConstantsV2.DEBUG_MODE)
                Debug.Log($"[SerialPortManager] Connected to {CurrentPortName} at {GameConstantsV2.SERIAL_BAUD_RATE} baud");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SerialPortManager] Connection failed to {CurrentPortName}: {ex.Message}");
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
                
                if (GameConstantsV2.DEBUG_MODE)
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
    /// ブロッキング動作：データが到着するまで待機
    /// </summary>
    public string ReadLine()
    {
        if (!IsConnected)
            return null;
        
        try
        {
            // ★ BytesToReadチェックを削除：ブロッキング待機
            return _serialPort.ReadLine();
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