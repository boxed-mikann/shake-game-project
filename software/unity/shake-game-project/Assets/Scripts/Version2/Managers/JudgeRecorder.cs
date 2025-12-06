using UnityEngine;

/// <summary>
/// ========================================
/// JudgeRecorder
/// ========================================
/// 
/// 責務：ゲーム中の判定結果を記録
/// - Perfect、Good、Bad、Missの判定回数をカウント
/// - ゲームリセット時にカウントをリセット
/// - 結果表示用にデータを提供
/// 
/// 特徴：
/// - シングルトンパターンで全体からアクセス可能
/// - 2周目以降も正しくリセットされる
/// 
/// ========================================
/// </summary>
public class JudgeRecorder : MonoBehaviour
{
    private static JudgeRecorder instance;
    
    public static JudgeRecorder Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JudgeRecorder>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("JudgeRecorder");
                    instance = obj.AddComponent<JudgeRecorder>();
                }
            }
            return instance;
        }
    }

    // 判定回数
    private int perfectCount = 0;
    private int goodCount = 0;
    private int badCount = 0;
    private int missCount = 0;

    // 公開プロパティ
    public int PerfectCount => perfectCount;
    public int GoodCount => goodCount;
    public int BadCount => badCount;
    public int MissCount => missCount;
    public int TotalCount => perfectCount + goodCount + badCount + missCount;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        SubscribeToGameEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }

    /// <summary>
    /// ゲームイベントに登録
    /// </summary>
    private void SubscribeToGameEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart += OnGameStart;
        }
    }

    /// <summary>
    /// ゲームイベントから解除
    /// </summary>
    private void UnsubscribeFromGameEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
        }
    }

    /// <summary>
    /// ゲーム開始時にカウントをリセット
    /// </summary>
    private void OnGameStart()
    {
        ResetCounts();
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log("[JudgeRecorder] Counts reset on game start");
        }
    }

    /// <summary>
    /// 判定結果を記録
    /// </summary>
    public void RecordJudgement(JudgeManagerV2.JudgementType judgement)
    {
        switch (judgement)
        {
            case JudgeManagerV2.JudgementType.Perfect:
                perfectCount++;
                break;
            case JudgeManagerV2.JudgementType.Good:
                goodCount++;
                break;
            case JudgeManagerV2.JudgementType.Bad:
                badCount++;
                break;
            case JudgeManagerV2.JudgementType.Miss:
                missCount++;
                break;
        }

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[JudgeRecorder] Recorded {judgement}. Total: Perfect={perfectCount}, Good={goodCount}, Bad={badCount}, Miss={missCount}");
        }
    }

    /// <summary>
    /// カウントをリセット（ゲーム開始時や手動リセット用）
    /// </summary>
    public void ResetCounts()
    {
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        missCount = 0;

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log("[JudgeRecorder] All counts reset");
        }
    }

    /// <summary>
    /// デバッグ用：現在のカウントを文字列で取得
    /// </summary>
    public string GetCountsDebugString()
    {
        return $"Perfect: {perfectCount}, Good: {goodCount}, Bad: {badCount}, Miss: {missCount}, Total: {TotalCount}";
    }
}
