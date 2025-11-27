using UnityEngine;

/// <summary>
/// フリーズ中用ハンドラー（何もしない）
/// </summary>
public class NullShakeHandler : MonoBehaviour, IShakeHandler
{
    public void HandleShake(string data, double timestamp)
    {
        // 何もしない（フリーズ中の入力を無視）
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NullShakeHandler] Input ignored during freeze");
            double lag_time = AudioSettings.dspTime - timestamp;
            Debug.Log($"[NoteShakeHandler] lag = {lag_time}");
    }
}