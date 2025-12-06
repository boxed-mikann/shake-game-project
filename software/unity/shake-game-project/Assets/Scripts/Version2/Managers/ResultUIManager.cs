using UnityEngine;
using TMPro;

/// <summary>
/// ========================================
/// ResultUIManager
/// ========================================
/// 
/// 責務：ゲーム終了時の結果表示UI管理
/// - GameManagerV2のゲーム終了イベントを感知
/// - VoltageManagerからVoltage値を取得
/// - Voltage値に基づくランク・メッセージ・色を決定
/// - 結果をUIに表示
/// 
/// 特徴：
/// - ランク評価システムが拡張可能
/// - インスペクタから全パラメータを設定可能
/// - UIテンプレートの一元管理
/// 
/// ========================================
/// </summary>

/// <summary>
/// ランク評価設定用クラス
/// インスペクタから各ランクのパラメータを編集可能
/// </summary>
[System.Serializable]
public class RankConfiguration
{
    [Header("ランク基準")]
    public float minVoltage;
    public string rankName;
    public string message;

    [Header("表示スタイル")]
    public Color rankColor = Color.white;
    public float rankFontSize = 80f;
}

/// <summary>
/// UI表示要素用クラス
/// インスペクタからTextMeshProUGUI参照を設定可能
/// </summary>
[System.Serializable]
public class UITemplate
{
    [Header("UI表示要素")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI voltageText;

    [Header("判定回数表示")]
    public TextMeshProUGUI perfectCountText;
    public TextMeshProUGUI goodCountText;
    public TextMeshProUGUI badCountText;
    public TextMeshProUGUI missCountText;
}

public class ResultUIManager : MonoBehaviour
{

    [SerializeField] private UITemplate uiTemplate = new UITemplate();

    [Header("ランク評価設定")]
    [SerializeField] private RankConfiguration[] rankConfigurations = new RankConfiguration[]
    {
        new RankConfiguration { minVoltage = 0f, rankName = "D", message = "まあまあだね！", rankColor = new Color(0.5f, 0.5f, 0.5f), rankFontSize = 800f },
        new RankConfiguration { minVoltage = 30f, rankName = "C", message = "悪くないね！", rankColor = new Color(0.2f, 0.8f, 0.2f), rankFontSize = 850f },
        new RankConfiguration { minVoltage = 60f, rankName = "B", message = "素晴らしい！", rankColor = new Color(0.2f, 0.5f, 1f), rankFontSize = 900f },
        new RankConfiguration { minVoltage = 85f, rankName = "A", message = "完璧だ！", rankColor = new Color(1f, 0.8f, 0f), rankFontSize = 950f },
        new RankConfiguration { minVoltage = 95f, rankName = "S", message = "超完璧！！", rankColor = new Color(1f, 0.2f, 0.2f), rankFontSize = 100f },
    };

    [Header("表示フォーマット")]
    [SerializeField] private string voltageFormat = "Voltage: {0:F1}V / {1:F1}V";
    [SerializeField] private string judgeCountFormat = "{0}";
    [SerializeField] private string judgeCountLabelFormat = "Perfect: {0}\nGood: {1}\nBad: {2}";

    private void Start()
    {
        SubscribeToGameEvents();
    }

    private void OnEnable()
    {
        SubscribeToGameEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }

    private void SubscribeToGameEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
        }
    }

    private void UnsubscribeFromGameEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }

    private void OnGameEnd()
    {
        if (VoltageManager.Instance == null)
        {
            Debug.LogError("[ResultUIManager] VoltageManager.Instance is null!");
            return;
        }

        float currentVoltage = VoltageManager.Instance.CurrentVoltage;
        float maxVoltage = VoltageManager.Instance.MaxVoltage;

        DisplayResult(currentVoltage, maxVoltage);

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[ResultUIManager] Game ended. Voltage: {currentVoltage:F1}V / {maxVoltage:F1}V");
        }
    }

    private void DisplayResult(float currentVoltage, float maxVoltage)
    {
        RankConfiguration rankConfig = GetRankConfiguration(currentVoltage);

        // ランク表示
        if (uiTemplate.rankText != null)
        {
            uiTemplate.rankText.text = rankConfig.rankName;
            uiTemplate.rankText.color = rankConfig.rankColor;
            uiTemplate.rankText.fontSize = rankConfig.rankFontSize;
        }

        // メッセージ表示
        if (uiTemplate.messageText != null)
        {
            uiTemplate.messageText.text = rankConfig.message;
            uiTemplate.messageText.color = rankConfig.rankColor;
        }

        // Voltage値表示
        if (uiTemplate.voltageText != null)
        {
            uiTemplate.voltageText.text = string.Format(voltageFormat, currentVoltage, maxVoltage);
        }

        // 判定回数表示
        DisplayJudgeCounts();
    }

    /// <summary>
    /// Voltage値に基づいてランク設定を取得
    /// minVoltageが高い順に評価（降順ソート済みと仮定）
    /// </summary>
    private RankConfiguration GetRankConfiguration(float voltage)
    {
        // 高い値から順に確認
        for (int i = rankConfigurations.Length - 1; i >= 0; i--)
        {
            if (voltage >= rankConfigurations[i].minVoltage)
            {
                return rankConfigurations[i];
            }
        }

        // フォールバック：最初の設定を返す
        return rankConfigurations[0];
    }

    /// <summary>
    /// 外部からランク設定を動的に追加・変更する場合はこのメソッドを使用
    /// </summary>
    public void SetRankConfigurations(RankConfiguration[] newConfigurations)
    {
        rankConfigurations = newConfigurations;
    }

    /// <summary>
    /// Voltage値フォーマットを変更
    /// 例: "{0:F2}V" で小数点以下2桁表示
    /// </summary>
    public void SetVoltageFormat(string format)
    {
        voltageFormat = format;
    }

    /// <summary>
    /// 判定回数を表示
    /// JudgeRecorderからデータを取得して各TMPに表示
    /// </summary>
    private void DisplayJudgeCounts()
    {
        if (JudgeRecorder.Instance == null)
        {
            Debug.LogWarning("[ResultUIManager] JudgeRecorder.Instance is null!");
            return;
        }

        int perfectCount = JudgeRecorder.Instance.PerfectCount;
        int goodCount = JudgeRecorder.Instance.GoodCount;
        int badCount = JudgeRecorder.Instance.BadCount;
        int missCount = JudgeRecorder.Instance.MissCount;

        // Perfect回数表示
        if (uiTemplate.perfectCountText != null)
        {
            uiTemplate.perfectCountText.text = string.Format(judgeCountFormat, perfectCount);
        }

        // Good回数表示
        if (uiTemplate.goodCountText != null)
        {
            uiTemplate.goodCountText.text = string.Format(judgeCountFormat, goodCount);
        }

        // Bad回数表示
        if (uiTemplate.badCountText != null)
        {
            uiTemplate.badCountText.text = string.Format(judgeCountFormat, badCount);
        }

        // Miss回数表示
        if (uiTemplate.missCountText != null)
        {
            uiTemplate.missCountText.text = string.Format(judgeCountFormat, missCount);
        }

        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[ResultUIManager] Judge counts displayed - Perfect: {perfectCount}, Good: {goodCount}, Bad: {badCount}, Miss: {missCount}");
        }
    }
}
