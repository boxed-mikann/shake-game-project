/// <summary>
/// 入力ソースの抽象化インターフェース（直接呼び出し方式）
/// Serial通信、キーボード入力など、異なる入力元に対応
/// UnityEventを廃止し、キューへの直接アクセスで約3倍高速化
/// </summary>
public interface IInputSource
{
    /// <summary>
    /// キューから入力データを取り出す（直接呼び出し方式）
    /// </summary>
    /// <param name="input">取り出された入力データ（data: 文字列, timestamp: AudioSettings.dspTime）</param>
    /// <returns>キューにデータがあれば true、空なら false</returns>
    bool TryDequeue(out (string data, double timestamp) input);
    
    /// <summary>
    /// 入力ソースの接続
    /// </summary>
    void Connect();
    
    /// <summary>
    /// 入力ソースの切断
    /// </summary>
    void Disconnect();
}