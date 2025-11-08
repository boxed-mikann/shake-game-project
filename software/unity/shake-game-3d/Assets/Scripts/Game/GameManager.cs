using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体の進行管理
/// - 2チーム対戦システム
/// - ゲージ管理
/// - 最大20プレイヤー対応
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private int maxPlayers = 20;
    [SerializeField] private float gaugeThreshold = 100.0f;
    [SerializeField] private int phaseDurationSeconds = 10;

    private SerialManager serialManager;
    private Dictionary<int, PlayerData> players;

    private float gameTimer = 0f;
    private GamePhase currentPhase = GamePhase.Charge;
    private GameState gameState = GameState.Playing;

    // チーム別ゲージ（0: Team A, 1: Team B）
    private float[] teamGauges = new float[2];

    public enum GamePhase { Charge, Resist }
    public enum GameState { Playing, Finished }

    private static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        InitializeGame();
        serialManager = FindObjectOfType<SerialManager>();

        if (serialManager != null)
        {
            serialManager.OnShake += HandleShakeInput;
        }
    }

    private void Update()
    {
        if (gameState != GameState.Playing) return;

        gameTimer += Time.deltaTime;

        // フェーズ切り替え
        if (gameTimer >= phaseDurationSeconds)
        {
            gameTimer = 0f;
            SwitchPhase();
        }

        // ゲージ減衰（Resist フェーズ）
        if (currentPhase == GamePhase.Resist)
        {
            DecayGauges();
        }

        // 勝利判定
        CheckVictory();
    }

    /// <summary>
    /// ゲーム初期化
    /// </summary>
    private void InitializeGame()
    {
        players = new Dictionary<int, PlayerData>();

        for (int i = 0; i < maxPlayers; i++)
        {
            players[i] = new PlayerData
            {
                childID = i,
                team = i < maxPlayers / 2 ? 0 : 1,  // 前半が Team A、後半が Team B
                shakeCount = 0,
                lastAcceleration = 0
            };
        }

        teamGauges[0] = 0f;
        teamGauges[1] = 0f;

        Debug.Log($"Game initialized - {maxPlayers} players, 2 teams");
    }

    /// <summary>
    /// フリフリ入力を処理
    /// </summary>
    private void HandleShakeInput(int childID, int shakeCount, int acceleration)
    {
        if (!players.ContainsKey(childID))
        {
            Debug.LogWarning($"Unknown child ID: {childID}");
            return;
        }

        PlayerData player = players[childID];
        player.shakeCount = shakeCount;
        player.lastAcceleration = acceleration;

        // ゲージを加算（加速度値に基づく）
        float gaugeIncrease = acceleration * 0.005f;  // スケーリング係数
        int team = player.team;

        if (currentPhase == GamePhase.Charge)
        {
            teamGauges[team] += gaugeIncrease;
        }
        else if (currentPhase == GamePhase.Resist)
        {
            teamGauges[team] -= gaugeIncrease * 0.5f;  // Resist フェーズでは効果が半分
        }

        // ゲージ上限チェック
        teamGauges[team] = Mathf.Clamp(teamGauges[team], 0f, gaugeThreshold);

        Debug.Log($"Team {team} gauge: {teamGauges[team]:F1}/{gaugeThreshold}");
    }

    /// <summary>
    /// フェーズ切り替え
    /// </summary>
    private void SwitchPhase()
    {
        currentPhase = currentPhase == GamePhase.Charge ? GamePhase.Resist : GamePhase.Charge;
        Debug.Log($"Phase switched to: {currentPhase}");
    }

    /// <summary>
    /// ゲージ減衰（Resist フェーズ）
    /// </summary>
    private void DecayGauges()
    {
        float decayRate = 2.0f;  // 秒単位

        for (int i = 0; i < 2; i++)
        {
            teamGauges[i] -= decayRate * Time.deltaTime;
            teamGauges[i] = Mathf.Max(0f, teamGauges[i]);
        }
    }

    /// <summary>
    /// 勝利判定
    /// </summary>
    private void CheckVictory()
    {
        for (int i = 0; i < 2; i++)
        {
            if (teamGauges[i] >= gaugeThreshold)
            {
                FinishGame(i);
            }
        }
    }

    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void FinishGame(int winningTeam)
    {
        gameState = GameState.Finished;
        Debug.Log($"Game finished! Team {winningTeam} wins!");
        // UI 表示などの処理を追加
    }

    /// <summary>
    /// チームゲージを取得
    /// </summary>
    public float GetTeamGauge(int team)
    {
        return team >= 0 && team < 2 ? teamGauges[team] : 0f;
    }

    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }

    /// <summary>
    /// プレイヤー情報を取得
    /// </summary>
    public PlayerData GetPlayer(int childID)
    {
        return players.ContainsKey(childID) ? players[childID] : null;
    }

    public static GameManager Instance
    {
        get { return instance; }
    }
}

/// <summary>
/// プレイヤー情報
/// </summary>
public class PlayerData
{
    public int childID;
    public int team;
    public int shakeCount;
    public int lastAcceleration;
}
