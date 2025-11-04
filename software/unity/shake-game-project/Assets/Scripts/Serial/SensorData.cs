using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ESP32の子機から受け取るシェイクデータ
/// </summary>
public struct ShakeDataPacket
{
    public int childID;
    public int shakeCount;
    public float acceleration;
    
    public override string ToString()
    {
        return $"[Child{childID}] Count: {shakeCount}, Accel: {acceleration:F1}";
    }
}
