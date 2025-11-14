using UnityEngine;
using System.IO.Ports;
using System.Collections.Generic;

/// <summary>
/// 入力管理 - SerialPort通信とキーボード入力を処理
/// ESP32デバイスからの無線入力を受け取る
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
                if (_instance == null)
                {
                    GameObject go = new GameObject("InputManager");
                    _instance = go.AddComponent<InputManager>();
                }
            }
            return _instance;
        }
    }

    // ===== SerialPort通信 =====
    private SerialPort _serialPort;
    private bool _isSerialConnected = false;
    private Dictionary<int, DeviceInputState> _deviceStates = new Dictionary<int, DeviceInputState>();
    
    // ===== キーボード入力（デバッグ用） =====
    private Dictionary<KeyCode, int> _keyToChildId = new Dictionary<KeyCode, int>
    {
        { KeyCode.Alpha1, 1 },
        { KeyCode.Alpha2, 2 },
        { KeyCode.Alpha3, 3 },
        { KeyCode.Alpha4, 4 },
        { KeyCode.Alpha5, 5 },
        { KeyCode.Alpha6, 6 },
        { KeyCode.Alpha7, 7 },
        { KeyCode.Alpha8, 8 },
        { KeyCode.Alpha9, 9 },
        { KeyCode.Alpha0, 10 }
    };
    
    // ===== イベント =====
    public delegate void OnDeviceInputEvent(int childId, int shakeCount, float acceleration);
    public event OnDeviceInputEvent OnDeviceInput;
    
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
            
            if (GameConstants.USE_KEYBOARD_INPUT)
            {
                Debug.Log("[InputManager] Keyboard input mode enabled for debugging");
            }
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
        foreach (var kvp in _keyToChildId)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                int childId = kvp.Value;
                int shakeCount = 1;
                float acceleration = 1500f;
                
                OnInputDetected(childId, shakeCount, acceleration);
                
                if (GameConstants.DEBUG_MODE)
                {
                    Debug.Log($"[InputManager] Keyboard input: ChildID={childId}, ShakeCount={shakeCount}, Acceleration={acceleration}");
                }
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
            
            int childId = int.Parse(parts[0].Trim());
            int shakeCount = int.Parse(parts[1].Trim());
            float acceleration = float.Parse(parts[2].Trim());
            
            OnInputDetected(childId, shakeCount, acceleration);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InputManager] Failed to parse data: {data} - Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 入力イベントを発火
    /// </summary>
    private void OnInputDetected(int childId, int shakeCount, float acceleration)
    {
        // デバイス状態を更新
        if (!_deviceStates.ContainsKey(childId))
        {
            _deviceStates[childId] = new DeviceInputState();
        }
        
        _deviceStates[childId].lastShakeCount = shakeCount;
        _deviceStates[childId].lastAcceleration = acceleration;
        _deviceStates[childId].lastInputTime = Time.time;
        
        // GameManager に通知
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
        {
            GameManager.Instance.OnPlayerShakeInput(childId, shakeCount, acceleration);
        }
        
        // イベント発火
        OnDeviceInput?.Invoke(childId, shakeCount, acceleration);
    }
    
    /// <summary>
    /// SerialPort を設定
    /// </summary>
    public void SetSerialPort(string portName, int baudRate = 115200)
    {
        try
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.Open();
            _isSerialConnected = true;
            
            Debug.Log($"[InputManager] SerialPort changed to {portName}");
        }
        catch (System.Exception ex)
        {
            _isSerialConnected = false;
            Debug.LogError($"[InputManager] Failed to set SerialPort: {ex.Message}");
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
    public Dictionary<int, DeviceInputState> DeviceStates => _deviceStates;
}

/// <summary>
/// デバイスの入力状態
/// </summary>
public class DeviceInputState
{
    public int lastShakeCount;
    public float lastAcceleration;
    public float lastInputTime;
}
