/// <summary>
/// シェイク入力時にエフェクト再生を行うオブジェクトが実装するインターフェース
/// </summary>
public interface IShakeable
{
    /// <summary>
    /// シェイク入力が処理されたことを通知
    /// エフェクト再生などの視覚的フィードバックを実行
    /// </summary>
    void OnShakeProcessed();
}
