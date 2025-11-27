using UnityEngine;

/// <summary>
/// ========================================
/// KeyboardInputReaderV2（Version2専用キーボード入力）
/// ========================================
/// 
/// 責務：デバッグ用のキーボード入力をシミュレート
/// 主機能：
/// - スペースキーでシェイク入力
/// - 数字キー（1～0）で複数デバイスをシミュレート
/// - InputQueueV2 経由で統一キューに追加
/// - CSV形式（ID,COUNT,ACCEL）でシミュレート
/// 
/// 設計方針：
/// - タイムスタンプ記録: AudioSettings.dspTime（Version1と同じ）
/// - 入力先: InputQueueV2（Version2専用の統一キュー）
/// - 数字キー対応でマルチデバイステスト可能
/// 
/// デバイスID割り当て：
/// - スペースキー → ID=0
/// - 数字キー1～9 → ID=1～9
/// - 数字キー0 → ID=0
/// 
/// ========================================
/// </summary>
public class KeyboardInputReaderV2 : MonoBehaviour
{
    // キーボード入力用の送信カウント（IDごとにインクリメント）
    private static readonly System.Collections.Generic.Dictionary<int, int> _keyCounts = new System.Collections.Generic.Dictionary<int, int>();

    void Update()
    {
        // スペースキー：デフォルトデバイス
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SimulateCsv(0);
        }
        
        // 数字キー1～9：複数デバイスのシミュレート
        if (Input.GetKeyDown(KeyCode.Alpha1)) SimulateCsv(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SimulateCsv(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SimulateCsv(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SimulateCsv(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SimulateCsv(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SimulateCsv(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SimulateCsv(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SimulateCsv(8);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SimulateCsv(9);
        // 0キーは ID=0 として扱う（仕様に合わせ 0~9）
        if (Input.GetKeyDown(KeyCode.Alpha0)) SimulateCsv(0);
    }
    
    /// <summary>
    /// CSV形式 (ID,COUNT,ACCEL) の入力をシミュレート
    /// </summary>
    private void SimulateCsv(int id)
    {
        double timestamp = AudioSettings.dspTime;
        
        // カウントをIDごとにインクリメント
        if (!_keyCounts.TryGetValue(id, out int cnt)) cnt = 0;
        cnt++;
        _keyCounts[id] = cnt;

        // 簡易的な加速度値（ダミー）。必要に応じて変更可能。
        // 実機に近づけるため、少し変動を付ける。
        float accel = 100000f + (id * 1000f) + (cnt % 10) * 123.456f;

        // シリアルフォーマットに合わせて生成
        string data = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2}", id, cnt, accel);
        InputQueueV2.Enqueue(data, timestamp);

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[KeyboardInputReaderV2] Simulated CSV: {data} at {timestamp:F3}s");
        }
    }
}
