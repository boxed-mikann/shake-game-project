using UnityEngine;

/// <summary>
/// 判定管理 - ランダム判定を返す（プロトタイプ版）
/// シングルトンパターン採用
/// 将来: BPM・ノーツ・タイミングに基づく実装に置き換え
/// </summary>
public class JudgeManagerV2 : MonoBehaviour
{
    public static JudgeManagerV2 Instance { get; private set; }

    public enum JudgementType
    {
        Perfect,
        Good,
        Bad,
        Miss
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// デバイスIDとタイムスタンプから判定を返す
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="relativeTime">ゲーム開始からの相対時間（秒）</param>
    /// <returns>判定種別</returns>
    public JudgementType Judge(string deviceId, double relativeTime)
    {
        Debug.Log($"[JudgeManager] Judging deviceId={deviceId}, relativeTime={relativeTime}s");
        
        // プロトタイプ: ランダム判定を返す
        int random = Random.Range(0, 4);
        JudgementType judgement = (JudgementType)random;
        
        Debug.Log($"[JudgeManager] Judgement result: {judgement}");
        return judgement;
    }

    /// <summary>
    /// 判定タイプから表示テキストを取得
    /// </summary>
    public static string GetJudgementText(JudgementType judgement)
    {
        return judgement switch
        {
            JudgementType.Perfect => "PERFECT",
            JudgementType.Good => "GOOD",
            JudgementType.Bad => "BAD",
            JudgementType.Miss => "MISS",
            _ => "?"
        };
    }
}
