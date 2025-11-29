using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 判定マネージャー - ノーツとシェイク入力のタイミングを判定
/// </summary>
public class JudgeManager : MonoBehaviour
{
    public static JudgeManager Instance { get; private set; }
    
    [Header("Timing Windows")]
    [SerializeField] private float perfectWindow = 0.05f;  // ±50ms
    [SerializeField] private float goodWindow = 0.10f;     // ±100ms
    [SerializeField] private float badWindow = 0.15f;      // ±150ms
    
    [Header("Statistics")]
    private int perfectCount = 0;
    private int goodCount = 0;
    private int badCount = 0;
    private int missCount = 0;
    
    // デバイスごとの最新シェイク時刻を記録
    private Dictionary<string, double> lastShakeTimes = new Dictionary<string, double>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    
    void OnEnable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
        }
    }
    
    void OnDisable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    
    private void OnGameStart()
    {
        ResetStatistics();
    }
    
    private void OnGameEnd()
    {
        // 統計をログ出力
        Debug.Log($"[JudgeManager] Perfect: {perfectCount}, Good: {goodCount}, Bad: {badCount}, Miss: {missCount}");
    }
    
    /// <summary>
    /// シェイク入力を記録
    /// ShakeResolverV2から呼び出される
    /// </summary>
    public void RecordShake(string deviceId, double timestamp)
    {
        lastShakeTimes[deviceId] = timestamp;
        
        // 最も近いノーツを検索して判定
        CheckNearestNote(deviceId, timestamp);
    }
    
    private void CheckNearestNote(string deviceId, double shakeTimestamp)
    {
        // TODO: 画面上のノーツから該当デバイスIDの最も近いノーツを検索
        // 現在は簡易実装（NoteV2から呼び出される想定）
    }
    
    /// <summary>
    /// ノーツヒット判定
    /// NoteV2から呼び出される
    /// </summary>
    public void OnNoteHit(string deviceId, float timingDiff)
    {
        string judgement;
        
        if (Mathf.Abs(timingDiff) <= perfectWindow)
        {
            judgement = "PERFECT";
            perfectCount++;
        }
        else if (Mathf.Abs(timingDiff) <= goodWindow)
        {
            judgement = "GOOD";
            goodCount++;
        }
        else if (Mathf.Abs(timingDiff) <= badWindow)
        {
            judgement = "BAD";
            badCount++;
        }
        else
        {
            judgement = "MISS";
            missCount++;
        }
        
        ShowJudgePopup(judgement);
        
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[JudgeManager] Device {deviceId}: {judgement} (Diff: {timingDiff:F3}s)");
        }
    }
    
    /// <summary>
    /// ノーツミス判定
    /// </summary>
    public void OnNoteMiss(string deviceId)
    {
        missCount++;
        ShowJudgePopup("MISS");
        
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[JudgeManager] Device {deviceId}: MISS");
        }
    }
    
    private void ShowJudgePopup(string judgement)
    {
        if (PopupPool.Instance == null) return;
        
        GameObject popupObj = PopupPool.Instance.GetJudgePopup();
        if (popupObj == null) return;
        
        JudgePopup popup = popupObj.GetComponent<JudgePopup>();
        if (popup != null)
        {
            popup.Show(judgement);
        }
    }
    
    private void ResetStatistics()
    {
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        missCount = 0;
        lastShakeTimes.Clear();
    }
    
    // 統計取得用
    public int GetPerfectCount() => perfectCount;
    public int GetGoodCount() => goodCount;
    public int GetBadCount() => badCount;
    public int GetMissCount() => missCount;
}
