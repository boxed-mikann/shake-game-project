using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体を管理する Singleton マネージャー
/// 状態遷移、ゲームループ、イベント管理を担当
/// </summary>
public class GameManager : MonoBehaviour
{
    // ===== Singleton =====
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    // ===== ゲーム状態 =====
    private GameState _currentState = GameState.Title;
    private GamePhase _currentPhase = GamePhase.Practice;
    
    // ===== 時間管理 =====
    private float _gameElapsedTime = 0f;
    private float _phaseElapsedTime = 0f;
    private bool _isGameRunning = false;
    
    // ===== ボス・プレイヤー =====
    private float _bossCurrentHP;
    private float _bossMaxHP = GameConstants.BOSS_MAX_HP;
    private Dictionary<int, PlayerData> _players = new Dictionary<int, PlayerData>();
    
    // ===== スコア =====
    private int _totalScore = 0;
    private int _totalShakeCount = 0;
    private int _totalSyncSuccessCount = 0;
    
    // ===== イベント =====
    public delegate void GameStateChangeEvent(GameState newState);
    public event GameStateChangeEvent OnGameStateChanged;
    
    public delegate void GamePhaseChangeEvent(GamePhase newPhase);
    public event GamePhaseChangeEvent OnGamePhaseChanged;
    
    public delegate void OnGameTickEvent(float elapsedTime);
    public event OnGameTickEvent OnGameTick;
    
    public delegate void OnPlayerShakeEvent(int childId, int shakeCount);
    public event OnPlayerShakeEvent OnPlayerShake;
    
    public delegate void OnBossDamageEvent(float damage, DamageType damageType);
    public event OnBossDamageEvent OnBossDamage;
    
    // ===== 参照キャッシュ =====
    private InputManager _inputManager;
    private PhaseController _phaseController;
    private BossController _bossController;
    private PlayerManager _playerManager;
    private UIManager _uiManager;
    private AudioManager _audioManager;
    
    private void Awake()
    {
        // Singleton化
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // マネージャーの取得
        _inputManager = InputManager.Instance;
        _phaseController = PhaseController.Instance;
        _bossController = BossController.Instance;
        _playerManager = PlayerManager.Instance;
        _uiManager = UIManager.Instance;
        _audioManager = AudioManager.Instance;
        
        // ボスHP初期化
        _bossCurrentHP = _bossMaxHP;
        
        // 初期状態をタイトルに設定
        ChangeGameState(GameState.Title);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[GameManager] Initialize completed");
        }
    }
    
    private void Update()
    {
        if (!_isGameRunning)
            return;
        
        // ゲーム時間を更新
        _gameElapsedTime += Time.deltaTime;
        _phaseElapsedTime += Time.deltaTime;
        
        // ゲーム時間チェック
        if (_gameElapsedTime >= GameConstants.TOTAL_GAME_TIME)
        {
            EndGame(false); // 時間切れ = 敗北
            return;
        }
        
        // フェーズ管理
        UpdatePhase();
        
        // イベント発火
        OnGameTick?.Invoke(_gameElapsedTime);
        
        // 勝利判定
        if (_bossCurrentHP <= 0)
        {
            EndGame(true); // ボスHP 0 = 勝利
        }
    }
    
    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        if (_currentState == GameState.Running)
            return;
        
        ChangeGameState(GameState.Loading);
        
        // ローディング処理
        _gameElapsedTime = 0f;
        _phaseElapsedTime = 0f;
        _bossCurrentHP = _bossMaxHP;
        _totalScore = 0;
        _totalShakeCount = 0;
        _totalSyncSuccessCount = 0;
        _players.Clear();
        
        // 初期フェーズは練習
        _currentPhase = GamePhase.Practice;
        
        // マネージャーのリセット
        _phaseController?.Reset();
        _bossController?.Reset();
        _playerManager?.Reset();
        _uiManager?.Reset();
        
        // BGM再生
        _audioManager?.PlayBGM();
        
        _isGameRunning = true;
        ChangeGameState(GameState.Running);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[GameManager] Game started");
        }
    }
    
    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void EndGame(bool victory)
    {
        _isGameRunning = false;
        
        if (victory)
        {
            _totalScore += GameConstants.SCORE_VICTORY_BONUS;
            ChangeGameState(GameState.Victory);
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log("[GameManager] Victory!");
            }
        }
        else
        {
            ChangeGameState(GameState.Defeat);
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log("[GameManager] Defeat");
            }
        }
        
        // 音楽停止
        _audioManager?.StopBGM();
        
        // 結果画面表示
        ChangeGameState(GameState.Result);
    }
    
    /// <summary>
    /// フェーズ管理・遷移
    /// </summary>
    private void UpdatePhase()
    {
        GamePhase newPhase = _phaseController.GetPhaseForTime(_gameElapsedTime);
        
        if (newPhase != _currentPhase)
        {
            ChangeGamePhase(newPhase);
        }
    }
    
    /// <summary>
    /// ゲーム状態を変更
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (_currentState == newState)
            return;
        
        _currentState = newState;
        OnGameStateChanged?.Invoke(newState);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] State changed to: {newState}");
        }
    }
    
    /// <summary>
    /// ゲームフェーズを変更
    /// </summary>
    public void ChangeGamePhase(GamePhase newPhase)
    {
        if (_currentPhase == newPhase)
            return;
        
        _currentPhase = newPhase;
        _phaseElapsedTime = 0f;
        OnGamePhaseChanged?.Invoke(newPhase);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] Phase changed to: {newPhase}");
        }
    }
    
    /// <summary>
    /// ボスにダメージを与える
    /// </summary>
    public void DamageBoss(float damage, DamageType damageType)
    {
        if (!_isGameRunning)
            return;
        
        _bossCurrentHP -= damage;
        _bossCurrentHP = Mathf.Max(0, _bossCurrentHP);
        
        OnBossDamage?.Invoke(damage, damageType);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] Boss took {damage} damage ({damageType}). HP: {_bossCurrentHP}/{_bossMaxHP}");
        }
    }
    
    /// <summary>
    /// プレイヤーがシェイクした
    /// </summary>
    public void OnPlayerShakeInput(int childId, int shakeCount, float acceleration)
    {
        if (!_isGameRunning)
            return;
        
        // プレイヤーデータを登録・更新
        if (!_players.ContainsKey(childId))
        {
            _players[childId] = new PlayerData();
        }
        
        _players[childId].childId = childId;
        _players[childId].shakeCount = shakeCount;
        _players[childId].acceleration = acceleration;
        _players[childId].lastUpdateTime = Time.time;
        
        _totalShakeCount++;
        _totalScore += GameConstants.SCORE_PER_SHAKE;
        
        OnPlayerShake?.Invoke(childId, shakeCount);
        
        // ダメージ計算・ボス攻撃
        float damage = _phaseController.CalculateDamageForCurrentPhase(shakeCount);
        DamageBoss(damage, DamageType.Normal);
    }
    
    /// <summary>
    /// 同期成功時のコールバック
    /// </summary>
    public void OnSyncSuccess(int syncPlayerCount)
    {
        _totalSyncSuccessCount++;
        _totalScore += GameConstants.SCORE_PER_SYNC;
        
        // 同期ダメージ計算
        float syncDamage = GameConstants.NORMAL_DAMAGE_PER_SHAKE * syncPlayerCount * GameConstants.SYNC_BONUS_MULTIPLIER;
        DamageBoss(syncDamage, DamageType.Sync);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] Sync success by {syncPlayerCount} players! Sync score: {GameConstants.SCORE_PER_SYNC}");
        }
    }
    
    // ===== Getter =====
    public GameState CurrentState => _currentState;
    public GamePhase CurrentPhase => _currentPhase;
    public float GameElapsedTime => _gameElapsedTime;
    public float PhaseElapsedTime => _phaseElapsedTime;
    public float BossCurrentHP => _bossCurrentHP;
    public float BossMaxHP => _bossMaxHP;
    public int TotalScore => _totalScore;
    public int TotalShakeCount => _totalShakeCount;
    public int TotalSyncSuccessCount => _totalSyncSuccessCount;
    public bool IsGameRunning => _isGameRunning;
    public Dictionary<int, PlayerData> Players => _players;
}

/// <summary>
/// プレイヤーのゲーム内データ
/// </summary>
public class PlayerData
{
    public int childId;
    public int shakeCount;
    public float acceleration;
    public float lastUpdateTime;
    public float energyLevel;
}
