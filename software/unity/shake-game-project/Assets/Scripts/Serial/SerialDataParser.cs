using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class SerialDataParser : MonoBehaviour
{
    [SerializeField] private SerialManager serialManager;
    
    private Queue<ShakeDataPacket> parsedDataQueue = new Queue<ShakeDataPacket>();
    
    void Update()
    {
        var messages = serialManager.GetReceivedMessages();
        
        foreach (var message in messages)
        {
            ShakeDataPacket packet = ParseMessage(message);
            if (packet.childID >= 0)
            {
                parsedDataQueue.Enqueue(packet);
                Debug.Log($"✅ Parsed: {packet}");
            }
        }
    }
    
    /// <summary>
    /// CSV形式のシリアルデータをパース
    /// 例: "0,5,32450.5"
    /// </summary>
    private ShakeDataPacket ParseMessage(string message)
    {
        ShakeDataPacket packet = new ShakeDataPacket { childID = -1 };
        
        if (string.IsNullOrEmpty(message))
            return packet;
        
        try
        {
            string[] parts = message.Split(',');
            
            if (parts.Length >= 3)
            {
                packet.childID = int.Parse(parts[0]);
                packet.shakeCount = int.Parse(parts[1]);
                packet.acceleration = float.Parse(parts[2]);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Parse error: {message} - {e.Message}");
        }
        
        return packet;
    }
    
    public Queue<ShakeDataPacket> GetParsedDataQueue()
    {
        return parsedDataQueue;
    }
}