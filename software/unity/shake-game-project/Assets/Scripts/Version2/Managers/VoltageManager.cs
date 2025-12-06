using System;
using UnityEngine;

public class VoltageManager : MonoBehaviour
{
    public static VoltageManager Instance { get; private set; }

    public event Action<float> OnVoltageChanged;

    [Header("Voltage Settings")]
    [SerializeField] private float maxVoltage = GameConstantsV2.VOLTAGE_MAX;
    [SerializeField] private float baseVoltagePerTick = GameConstantsV2.BASE_VOLTAGE;
    // スケーリング係数
    [SerializeField] private float scalingFactor = 1.0f;
    private float currentVoltage = 0f;

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

    private void Start()
    {
        // ゲームフェーズイベントを購読
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        // ゲームフェーズイベントを購読
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        // unsubscribe to avoid memory leaks
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart -= ResetVoltage;
            GameManagerV2.Instance.OnGameStart -= ResetVoltage;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnResisterStart -= ResetVoltage;
            GameManagerV2.Instance.OnResisterStart += ResetVoltage;
            GameManagerV2.Instance.OnGameStart -= ResetVoltage;
            GameManagerV2.Instance.OnGameStart += ResetVoltage;
            Debug.Log("[VoltageManager] Subscribed to game phase events.");
        }
    }
    public float CurrentVoltage => currentVoltage;
    public float MaxVoltage => maxVoltage;
    
    public float GetVoltage() => currentVoltage;

    public void ResetVoltage()
    {
        currentVoltage = 0f;
        OnVoltageChanged?.Invoke(currentVoltage);
    }

    public void SimpleAddVoltage(float amount)
    {
        currentVoltage = Mathf.Clamp(currentVoltage + amount, 0f, maxVoltage);
        OnVoltageChanged?.Invoke(currentVoltage);
        // Debug log for voltage addition
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[VoltageManager] SimpleAddVoltage: +{amount:F1}V (Total: {currentVoltage:F1}V)");
        }
    }

    /// <summary>
    /// 判定結果とシンクロ人数に基づいてVoltageを加算
    /// </summary>
    /// <param name="judgement">判定結果 (Perfect/Good/Bad/Miss)</param>
    /// <param name="syncCount">シンクロした人数 (1以上)</param>
    public void SimpleAddVoltage(JudgeManagerV2.JudgementType judgement, int syncCount)
    {
        float amount = CalculateVoltageScore(judgement, syncCount);
        amount = ScaleScore(amount);// 難易度スケーリング適用
        currentVoltage = Mathf.Clamp(currentVoltage + amount, 0f, maxVoltage);
        OnVoltageChanged?.Invoke(currentVoltage);

        // Debug log for voltage addition
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[VoltageManager] Judgement: {judgement}, SyncCount: {syncCount}, Added: {amount:F1}V (Total: {currentVoltage:F1}V)");
        }
    }

    //得点加算をスケーリングする関数　ゲーム難易度調整用
    private float ScaleScore(float baseScore)
    {
        //登録デバイス数でスケーリングする
        int registeredDeviceCount = DeviceRegisterManager.Instance != null ? DeviceRegisterManager.Instance.RegisteredDeviceCount : 1;
        // 現在はスケーリングなし。将来的に難易度に応じたスケーリングを実装可能。
        return baseScore*scalingFactor/(registeredDeviceCount );
    }

    /// <summary>
    /// 判定結果とシンクロ人数から最終スコアを計算
    /// Miss以外: ベーススコア × シンクロ人数
    /// Miss: ベーススコア + (シンクロ人数 × 0.8) でマイナス軽減
    /// </summary>
    private float CalculateVoltageScore(JudgeManagerV2.JudgementType judgement, int syncCount)
    {
        float baseScore = GetBaseScoreFromJudgement(judgement);
        int actualSyncCount = Mathf.Max(1, syncCount);

        // Miss判定の場合は特別な計算
        if (judgement == JudgeManagerV2.JudgementType.Miss)
        {
            // ベース-2.0 + シンクロボーナス
            // 1人: -2.0 + 0.8 = -1.2
            // 2人: -2.0 + 1.6 = -0.4
            // 3人: -2.0 + 2.4 = +0.4
            // 4人以上: プラスに転じる
            float syncBonus = actualSyncCount * 0.8f;
            return baseScore + syncBonus;
        }
        else
        {
            // Perfect/Good/Badは通常通りシンクロ倍率適用
            return baseScore * actualSyncCount;
        }
    }

    /// <summary>
    /// 判定結果からベーススコアを取得
    /// Perfect: +3.0, Good: +2.0, Bad: +1.0, Miss: -2.0
    /// </summary>
    private float GetBaseScoreFromJudgement(JudgeManagerV2.JudgementType judgement)
    {
        switch (judgement)
        {
            case JudgeManagerV2.JudgementType.Perfect:
                return 3.0f;
            case JudgeManagerV2.JudgementType.Good:
                return 2.0f;
            case JudgeManagerV2.JudgementType.Bad:
                return 1.0f;
            case JudgeManagerV2.JudgementType.Miss:
                return -2.0f;
            default:
                return 0f;
        }
    }
}
