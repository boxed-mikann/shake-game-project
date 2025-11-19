using UnityEngine.Events;

/// <summary>
/// 入力ソースの抽象化インターフェース
/// Serial通信、キーボード入力など、異なる入力元に対応
/// </summary>
public interface IInputSource
{
    /// <summary>シェイク検出イベント</summary>
    UnityEvent OnShakeDetected { get; }
    
    /// <summary>接続状態プロパティ</summary>
    bool IsConnected { get; }
    
    /// <summary>接続開始メソッド</summary>
    void Connect();
    
    /// <summary>接続切断メソッド</summary>
    void Disconnect();
}