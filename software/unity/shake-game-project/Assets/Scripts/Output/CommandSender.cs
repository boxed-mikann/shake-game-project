using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI からの指示を ESP32 に送信する責務のみ
/// （Single Responsibility Principle）
/// </summary>
public class CommandSender : MonoBehaviour
{
    private static CommandSender instance;
    
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
    
    /// <summary>
    /// ★ UI が呼び出す：ライトを点灯
    /// </summary>
    public void SendLightOn(int deviceId)
    {
        string command = $"DEVICE_{deviceId}_LIGHT_ON";
        SerialManager.Instance.SendCommand(command);
        Debug.Log($"💡 Light ON signal sent to device {deviceId}");
    }
    
    /// <summary>
    /// ★ UI が呼び出す：ライトを消灯
    /// </summary>
    public void SendLightOff(int deviceId)
    {
        string command = $"DEVICE_{deviceId}_LIGHT_OFF";
        SerialManager.Instance.SendCommand(command);
        Debug.Log($"💡 Light OFF signal sent to device {deviceId}");
    }
    
    /// <summary>
    /// ★ UI が呼び出す：ライトを点滅
    /// </summary>
    public void SendLightBlink(int deviceId, int blinkCount = 3)
    {
        string command = $"DEVICE_{deviceId}_LIGHT_BLINK_{blinkCount}";
        SerialManager.Instance.SendCommand(command);
        Debug.Log($"💡 Light BLINK signal sent to device {deviceId}");
    }
    
    /// <summary>
    /// ★ UI が呼び出す：バイブレーション開始
    /// </summary>
    public void SendVibrationOn(int deviceId)
    {
        string command = $"DEVICE_{deviceId}_VIB_ON";
        SerialManager.Instance.SendCommand(command);
        Debug.Log($"📳 Vibration ON signal sent to device {deviceId}");
    }
    
    /// <summary>
    /// ★ UI が呼び出す：バイブレーション停止
    /// </summary>
    public void SendVibrationOff(int deviceId)
    {
        string command = $"DEVICE_{deviceId}_VIB_OFF";
        SerialManager.Instance.SendCommand(command);
        Debug.Log($"📳 Vibration OFF signal sent to device {deviceId}");
    }
    
    public static CommandSender Instance => instance;
}