/// <summary>
/// フェーズごとのシェイク処理を定義するインターフェース
/// Phase1ShakeHandler, Phase2ShakeHandler などが実装する
/// </summary>
public interface IShakeHandler
{
    /// <summary>
    /// シェイク処理メソッド
    /// 入力検出時に ShakeResolver から呼び出される
    /// </summary>
    void HandleShake();
}