using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serial通信プロトコルを統一管理
/// </summary>
public static class SerialProtocol
{
    // 受信フォーマット: "childID,shakeCount,acceleration"
    public const string SHAKE_DATA_FORMAT = "0,1,30000";
    
    // 送信コマンド
    public const string CMD_LIGHT_ON = "LIGHT_ON";
    public const string CMD_LIGHT_OFF = "LIGHT_OFF";
    public const string CMD_LIGHT_BLINK = "LIGHT_BLINK";
    public const string CMD_VIBRATION_ON = "VIB_ON";
    public const string CMD_VIBRATION_OFF = "VIB_OFF";
    
    /// <summary>
    /// コマンドにチェックサムを付加
    /// </summary>
    public static string AddChecksum(string command)
    {
        int checksum = 0;
        foreach (char c in command)
        {
            checksum += (int)c;
        }
        return $"{command}|{checksum}";
    }
    
    /// <summary>
    /// チェックサム検証
    /// </summary>
    public static bool VerifyChecksum(string data)
    {
        string[] parts = data.Split('|');
        if (parts.Length != 2)
            return false;
        
        int checksum = 0;
        foreach (char c in parts[0])
        {
            checksum += (int)c;
        }
        
        return checksum.ToString() == parts[1];
    }
}