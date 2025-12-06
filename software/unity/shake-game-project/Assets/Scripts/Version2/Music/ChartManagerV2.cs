using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 譜面管理 - JSONから譜面を読み込み、判定・生成システムにデータを提供
/// インスペクタで譜面JSONをアタッチ可能
/// </summary>
public class ChartManagerV2 : MonoBehaviour
{
    public static ChartManagerV2 Instance { get; private set; }

    [Header("Chart Settings")]
    [SerializeField] private TextAsset chartJson; // インスペクタでJSONをアタッチ
    
    private ChartDataV2 currentChart;
    private Dictionary<int, List<ChartDataV2.GameNote>> notesByDevice; // デバイスごとのノート管理

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (chartJson != null)
        {
            LoadChart();
        }
        else
        {
            Debug.LogWarning("[ChartManagerV2] 譜面JSONが設定されていません");
        }
    }

    /// <summary>
    /// 譜面を読み込む
    /// </summary>
    public void LoadChart()
    {
        if (chartJson == null)
        {
            Debug.LogError("[ChartManagerV2] 譜面JSONが設定されていません");
            return;
        }

        try
        {
            currentChart = JsonUtility.FromJson<ChartDataV2>(chartJson.text);
            currentChart.ConvertToGameNotes();
            BuildDeviceNoteIndex();
            Debug.Log($"[ChartManagerV2] 譜面読み込み完了: {currentChart.name}, {currentChart.gameNotes.Count}ノーツ");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChartManagerV2] JSON解析エラー: {e.Message}");
        }
    }

    /// <summary>
    /// デバイスごとのノートインデックスを構築
    /// </summary>
    private void BuildDeviceNoteIndex()
    {
        notesByDevice = new Dictionary<int, List<ChartDataV2.GameNote>>();
        
        if (currentChart == null || currentChart.gameNotes == null) return;

        int longStartCount = 0;
        int longEndCount = 0;
        foreach (var note in currentChart.gameNotes)
        {
            if (!notesByDevice.ContainsKey(note.deviceId))
            {
                notesByDevice[note.deviceId] = new List<ChartDataV2.GameNote>();
            }
            notesByDevice[note.deviceId].Add(note);
            
            if (note.noteType == ChartDataV2.NoteType.LongStart)
            {
                longStartCount++;
                Debug.Log($"[ChartManagerV2] BuildDeviceNoteIndex: LongStart found - deviceId={note.deviceId}, time={note.time:F6}, duration={note.duration:F6}");
            }
            else if (note.noteType == ChartDataV2.NoteType.LongEnd)
            {
                longEndCount++;
                Debug.Log($"[ChartManagerV2] BuildDeviceNoteIndex: LongEnd found - deviceId={note.deviceId}, time={note.time:F6}, duration={note.duration:F6}");
            }
        }
        Debug.Log($"[ChartManagerV2] BuildDeviceNoteIndex: Total {longStartCount} LongStart, {longEndCount} LongEnd notes indexed");
    }

    /// <summary>
    /// 現在の譜面を取得
    /// </summary>
    public ChartDataV2 GetChart()
    {
        return currentChart;
    }

    /// <summary>
    /// 指定時刻から最も近いノートを検索（判定用）
    /// 長押し開始は除外し、単ノートと長押し終了のみを対象とする
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="time">判定時刻（秒）</param>
    /// <param name="searchRange">検索範囲（秒）</param>
    /// <returns>最も近いノート、見つからなければnull</returns>
    public ChartDataV2.GameNote FindNearestNote(int deviceId, double time, double searchRange = 0.2)
    {
        if (currentChart == null || notesByDevice == null)
        {
            Debug.LogWarning("[ChartManagerV2] 譜面データが未ロードまたはインデックスが未構築です");
            return null;
        }
        if (!notesByDevice.ContainsKey(deviceId))
        {
            Debug.LogWarning($"[ChartManagerV2] デバイスID {deviceId} のノートが存在しません");
            return null;
        }

        ChartDataV2.GameNote nearest = null;
        double minDiff = double.MaxValue;// 十分大きな値で初期化

        foreach (var note in notesByDevice[deviceId])
        {
            // 長押し開始は判定しない
            if (note.noteType == ChartDataV2.NoteType.LongStart) continue;

            double diff = System.Math.Abs(note.time - time);
            if (diff < minDiff)
            {
                minDiff = diff;
                nearest = note;
            }
        }
        Debug.Log($"[ChartManagerV2] FindNearestNote: deviceId={deviceId}, time={time:F6}, nearestTime={(nearest != null ? nearest.time.ToString("F6") : "null")}, diff={minDiff:F6}");

        return nearest;
    }

    /// <summary>
    /// 指定時刻が長押し期間内かどうかを判定
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="time">判定時刻（秒）</param>
    /// <returns>長押し期間内ならtrue</returns>
    public bool IsInLongNotePeriod(int deviceId, double time)
    {
        if (currentChart == null || notesByDevice == null) return false;
        if (!notesByDevice.ContainsKey(deviceId)) return false;

        foreach (var note in notesByDevice[deviceId])
        {
            // LongStartノートを探し、そのdurationから長押し期間を判定
            if (note.noteType == ChartDataV2.NoteType.LongStart && note.duration > 0)
            {
                // note.timeが開始時刻、note.durationが全体の長さ
                double startTime = note.time;
                double endTime = note.time + note.duration;
                
                Debug.Log($"[ChartManagerV2] IsInLongNotePeriod check: deviceId={deviceId}, checkTime={time:F6}, longStart={startTime:F6}, longEnd={endTime:F6}, duration={note.duration:F6}");
                
                if (time >= startTime && time <= endTime)
                {
                    Debug.Log($"[ChartManagerV2] IsInLongNotePeriod: TRUE - time is in long note period");
                    return true;
                }
            }
        }

        Debug.Log($"[ChartManagerV2] IsInLongNotePeriod: FALSE - no matching long note period found");
        return false;
    }

    /// <summary>
    /// 譜面のBPMを取得
    /// </summary>
    public int GetBPM()
    {
        return currentChart != null ? currentChart.BPM : 0;
    }

}
