using UnityEngine;
using System.IO.Ports;

/// <summary>
/// 入力管理 - SerialPort通信とキーボード入力を処理
/// ESP32デバイスからの無線入力を受け取る
/// イベント駆動型：高速・シンプル
/// </summary>
public class InputManager : MonoBehaviour
{
    // ===== Singleton =====
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InputManager>();
            }
            return _instance;
        }
    }

    // ===== SerialPort通信 =====
    private SerialPort _serialPort;
    private bool _isSerialConnected = false;
    
    // ===== イベント =====
    public delegate void OnShakeEvent(int deviceId, int shakeCount, float acceleration);
    public event OnShakeEvent OnShakeDetected;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[InputManager] Attempting to connect to SerialPort: " + GameConstants.SERIAL_PORT_NAME);
        }
        InitializeSerialPort();
    }
    
    private void InitializeSerialPort()
    {
        try
        {
            _serialPort = new SerialPort(GameConstants.SERIAL_PORT_NAME, GameConstants.SERIAL_BAUD_RATE);
            _serialPort.Open();
            _isSerialConnected = true;
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log("[InputManager] SerialPort connected successfully");
            }
        }
        catch (System.Exception ex)
        {
            _isSerialConnected = false;
            Debug.LogWarning($"[InputManager] Failed to connect SerialPort: {ex.Message}");
        }
    }
    
    private void Update()
    {
        // SerialPort通信からの入力
        if (_isSerialConnected)
        {
            ProcessSerialInput();
        }
        
        // キーボード入力（デバッグ用）
        if (GameConstants.USE_KEYBOARD_INPUT)
        {
            ProcessKeyboardInput();
        }
    }
    
    /// <summary>
    /// SerialPort からのデータを処理
    /// フォーマット: ChildID,ShakeCount,Acceleration\n
    /// </summary>
    private void ProcessSerialInput()
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                _isSerialConnected = false;
                return;
            }
            
            if (_serialPort.BytesToRead > 0)
            {
                string line = _serialPort.ReadLine();
                ParseDeviceData(line);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InputManager] SerialPort error: {ex.Message}");
            _isSerialConnected = false;
        }
    }
    
    /// <summary>
    /// キーボード入力を処理（デバッグ用）
    /// </summary>
    private void ProcessKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int deviceId = 0;
            int shakeCount = 1;
            float acceleration = 1500f;
            
            OnShakeDetected?.Invoke(deviceId, shakeCount, acceleration);
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log($"[InputManager] Keyboard input: DeviceID={deviceId}, ShakeCount={shakeCount}, Acceleration={acceleration}");
            }
        }
    }
    
    /// <summary>
    /// デバイスデータを解析
    /// </summary>
    private void ParseDeviceData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;
        
        try
        {
            string[] parts = data.Split(',');
            
            if (parts.Length < 3)
            {
                Debug.LogWarning("[InputManager] Invalid data format: " + data);
                return;
            }
            
            int deviceId = int.Parse(parts[0].Trim());
            int shakeCount = int.Parse(parts[1].Trim());
            float acceleration = float.Parse(parts[2].Trim());
            
            // ✨ 高速: イベント直接呼び出し
            OnShakeDetected?.Invoke(deviceId, shakeCount, acceleration);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InputManager] Failed to parse data: {data} - Error: {ex.Message}");
        }
    }
    
    private void OnDestroy()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            _serialPort.Close();
        }
    }
    
    // ===== Getter =====
    public bool IsSerialConnected => _isSerialConnected;
}