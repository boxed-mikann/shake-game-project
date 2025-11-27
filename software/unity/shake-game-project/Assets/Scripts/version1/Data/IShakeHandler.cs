/// <summary>
/// フェーズごとのシェイク処理を定義するインターフェース
/// NoteShakeHandler, RestShakeHandler が実装する
/// 直接呼び出し方式で高速化
/// </summary>
public interface IShakeHandler
{
    /// <summary>
    /// シェイク処理メソッド
    /// </summary>
    /// <param name="data">シェイクデータ（文字列）</param>
    /// <param name="timestamp">AudioSettings.dspTime のタイムスタンプ</param>
    void HandleShake(string data, double timestamp);
}