using UnityEngine;

/// <summary>
/// 譜面ローダー - Resources/Charts/からJSON譜面を読み込む
/// </summary>
public class ChartLoader : MonoBehaviour
{
    public static ChartLoader Instance { get; private set; }
    
    private ChartData currentChart;
    
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
    
    /// <summary>
    /// 譜面を読み込む
    /// </summary>
    /// <param name="chartName">Resources/Charts/配下のファイル名（拡張子なし）</param>
    public ChartData LoadChart(string chartName)
    {
        string path = $"Charts/{chartName}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(path);
        
        if (jsonAsset == null)
        {
            Debug.LogError($"[ChartLoader] 譜面が見つかりません: {path}");
            return null;
        }
        
        try
        {
            currentChart = JsonUtility.FromJson<ChartData>(jsonAsset.text);
            
            if (currentChart != null)
            {
                currentChart.CalculateNoteTimes();
                Debug.Log($"[ChartLoader] 譜面読み込み成功: {chartName}, ノーツ数: {currentChart.notes.Count}");
            }
            
            return currentChart;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ChartLoader] JSON解析エラー: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 現在の譜面を取得
    /// </summary>
    public ChartData GetCurrentChart()
    {
        return currentChart;
    }
    
    /// <summary>
    /// 譜面をクリア
    /// </summary>
    public void ClearChart()
    {
        currentChart = null;
    }
}
