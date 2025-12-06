using UnityEngine;

/// <summary>
/// 判定管理 - 譜面データに基づいて判定を行う
/// シングルトンパターン採用
/// </summary>
public class JudgeManagerV2 : MonoBehaviour
{
    public static JudgeManagerV2 Instance { get; private set; }

    [Header("Judge Settings")]
    [SerializeField] private double perfectRange = 0.05;  // ±50ms
    [SerializeField] private double goodRange = 0.1;      // ±100ms
    [SerializeField] private double badRange = 0.15;      // ±150ms

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
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// デバイスIDとタイムスタンプから判定を返す
    /// </summary>
    /// <param name="deviceId">デバイスID（文字列）</param>
    /// <param name="relativeTime">ゲーム開始からの相対時間（秒）</param>
    /// <returns>判定種別</returns>
    public JudgementType Judge(string deviceId, double relativeTime)
    {
        // 文字列のdeviceIdを整数に変換
        if (!int.TryParse(deviceId, out int deviceIdInt))
        {
            Debug.LogWarning($"[JudgeManagerV2] 無効なデバイスID: {deviceId}");
            return JudgementType.Miss;
        }
        
        return Judge(deviceIdInt, relativeTime);
    }

    /// <summary>
    /// デバイスIDとタイムスタンプから判定を返す（整数版）
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="shakeTime">シェイク時刻（秒）</param>
    /// <returns>判定種別</returns>
    public JudgementType Judge(int deviceId, double shakeTime)
    {
        if (ChartManagerV2.Instance == null)
        {
            Debug.LogWarning("[JudgeManagerV2] ChartManagerV2が見つかりません");
            return JudgementType.Miss;
        }

        // 長押し期間中かチェック
        if (ChartManagerV2.Instance.IsInLongNotePeriod(deviceId, shakeTime))
        {
            Debug.Log($"[JudgeManagerV2] 長押し期間中 - DeviceId={deviceId}, Time={shakeTime}");
            return JudgementType.Perfect; // 長押し期間中は常にPerfect
        }

        // 最も近いノートを検索
        ChartDataV2.GameNote nearestNote = ChartManagerV2.Instance.FindNearestNote(deviceId, shakeTime);
        
        if (nearestNote == null)
        {
            Debug.Log($"[JudgeManagerV2] Miss - 近くにノートがない DeviceId={deviceId}, Time={shakeTime}");
            return JudgementType.Miss;
        }

        // ずれを計算
        double diff = System.Math.Abs(nearestNote.time - shakeTime);
        
        JudgementType judgement;
        if (diff < perfectRange)
        {
            judgement = JudgementType.Perfect;
        }
        else if (diff < goodRange)
        {
            judgement = JudgementType.Good;
        }
        else if (diff < badRange)
        {
            judgement = JudgementType.Bad;
        }
        else
        {
            judgement = JudgementType.Miss;
        }
        
        Debug.Log($"[JudgeManagerV2] DeviceId={deviceId}, ShakeTime={shakeTime:F6}, NoteTime={nearestNote.time:F6}, Diff={diff:F6}, Judgement={judgement}");
        
        // ヒットしたノートを処理（画面から消す）
        if (judgement != JudgementType.Miss)
        {
            NotifyNoteHit(deviceId, nearestNote.time);
        }
        
        return judgement;
    }

    /// <summary>
    /// ノートがヒットしたことを通知（ノートオブジェクトを消す）
    /// </summary>
    private void NotifyNoteHit(int deviceId, double noteTime)
    {
        // アクティブなノートを検索して消す
        if (NotePoolV2.Instance != null)
        {
            NoteObjectV2[] activeNotes = FindObjectsOfType<NoteObjectV2>();
            foreach (var note in activeNotes)
            {
                if (note.IsInitialized() && 
                    note.GetDeviceId() == deviceId && 
                    System.Math.Abs(note.GetArrivalTime() - noteTime) < 0.01)
                {
                    note.OnHit();
                    break;
                }
            }
        }
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
