using UnityEngine;

/// <summary>
/// シェイク入力処理の基底クラス
/// 戦略パターンで状態に応じた処理を実装する
/// </summary>
public abstract class ShakeHandlerBase : MonoBehaviour
{
    /// <summary>
    /// シェイク入力を処理する
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="timestamp">入力タイムスタンプ (AudioSettings.dspTime)</param>
    public abstract void HandleShake(string deviceId, double timestamp);
}
