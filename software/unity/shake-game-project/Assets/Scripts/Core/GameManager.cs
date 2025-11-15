using UnityEngine;
using System.Collections;

/// <summary>
/// ゲーム全体を統括するマネージャー
/// 責務：ゲーム進行管理、タイマー管理、入力処理、フリーズ効果、画面遷移
/// 新設計：1チーム協力型、60秒ゲーム、音符はじけメカニクス
/// </summary>
public enum GameState { Start, Playing, Result }

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform notesContainer;  // 音符の親オブジェクト
    [SerializeField] private GameObject notePrefab;     // 音符Prefab
    
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }

    private GameState _gameState = GameState.Start;
    private float _gameTimer = 0f;
    private bool _isGameRunning = false;
    private bool _isFrozen = false;
    private float _freezeRemainingTime = 0f;
    private int _currentSpawnRate = GameConstants.SPAWN_RATE_BASE;
    private float _spawnTimer = 0f;
    
    // イベント
    public delegate void OnGameStateChangedEvent(GameState newState);
    public event OnGameStateChangedEvent OnGameStateChanged;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    private void Start()
    {
        // InputManager のイベント購読
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnShakeDetected += OnShakeInput;
        }
        
        // ScoreManager 初期化
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.Initialize();
        }
    }
    
    private void Update()
    {
        if (_gameState == GameState.Playing)
        {
            UpdateGameTimer();
            UpdateFreezeEffect();
            UpdateNoteSpawning();
        }
    }
    
    /// <summary>
    /// ゲーム開始
    /// </summary>
    public void StartGame()
    {
        _gameState = GameState.Playing;
        _gameTimer = GameConstants.GAME_DURATION;
        _isGameRunning = true;
        _isFrozen = false;
        _freezeRemainingTime = 0f;
        _currentSpawnRate = GameConstants.SPAWN_RATE_BASE;
        _spawnTimer = 0f;
        
        ScoreManager.Instance.Initialize();
        PhaseController.Instance.Initialize();
        
        OnGameStateChanged?.Invoke(_gameState);
        
        Debug.Log("[GameManager] ▶️ Game started!");
    }
    
    /// <summary>
    /// ゲームタイマー更新
    /// </summary>
    private void UpdateGameTimer()
    {
        _gameTimer -= Time.deltaTime;
        
        // ラストスパート判定（最後10秒）
        if (_gameTimer <= GameConstants.LAST_SPRINT_DURATION && _gameTimer > GameConstants.LAST_SPRINT_DURATION - 0.1f)
        {
            _currentSpawnRate = (int)(GameConstants.SPAWN_RATE_BASE * GameConstants.LAST_SPRINT_MULTIPLIER);
            Debug.Log("[GameManager] ⚡ Last sprint! Spawn rate x2");
        }
        
        // タイムアップ
        if (_gameTimer <= 0f)
        {
            EndGame();
        }
    }
    
    /// <summary>
    /// フリーズエフェクト更新
    /// </summary>
    private void UpdateFreezeEffect()
    {
        if (_isFrozen)
        {
            _freezeRemainingTime -= Time.deltaTime;
            
            if (_freezeRemainingTime <= 0f)
            {
                _isFrozen = false;
                Time.timeScale = 1f;
                Debug.Log("[GameManager] ❌ Freeze released");
            }
        }
    }
    
    /// <summary>
    /// 音符のスポーン管理
    /// </summary>
    private void UpdateNoteSpawning()
    {
        if (notePrefab == null || notesContainer == null)
        {
            Debug.LogWarning("[GameManager] notePrefab or notesContainer is not assigned!");
            return;
        }
        
        _spawnTimer += Time.deltaTime;
        float spawnInterval = 1f / _currentSpawnRate;  // 秒/個
        
        while (_spawnTimer >= spawnInterval)
        {
            SpawnNote();
            _spawnTimer -= spawnInterval;
        }
    }
    
    /// <summary>
    /// 音符を1個スポーン
    /// </summary>
    private void SpawnNote()
    {
        Vector3 randomPos = new Vector3(
            Random.Range(-300f, 300f),
            Random.Range(-200f, 200f),
            0f
        );
        
        GameObject noteGO = Instantiate(notePrefab, randomPos, Quaternion.identity, notesContainer);
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] 🎵 Note spawned at {randomPos}");
        }
    }
    
    /// <summary>
    /// シェイク入力を処理
    /// </summary>
    private void OnShakeInput(int deviceId, int shakeCount, float acceleration)
    {
        if (_gameState != GameState.Playing || _isFrozen)
            return;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] 📊 Shake input: DeviceID={deviceId}, Count={shakeCount}, Accel={acceleration}");
        }
        
        // 画面上の音符をランダムにはじける
        // （NotePrefab.OnNoteClicked が呼ばれて、スコア処理される）
    }
    
    /// <summary>
    /// フリーズ効果を発動
    /// </summary>
    public void TriggerFreeze()
    {
        if (_isFrozen)
            return;
        
        _isFrozen = true;
        _freezeRemainingTime = GameConstants.FREEZE_DURATION;
        Time.timeScale = GameConstants.FREEZE_TIME_SCALE;
        
        // ホワイトフラッシュなど視覚効果（UIManager 等で実装）
        
        Debug.Log("[GameManager] ⏸️ Freeze triggered!");
    }
    
    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void EndGame()
    {
        _isGameRunning = false;
        Time.timeScale = 1f;  // フリーズを解除
        PhaseController.Instance.StopGame();
        
        _gameState = GameState.Result;
        OnGameStateChanged?.Invoke(_gameState);
        
        int finalScore = ScoreManager.Instance.GetFinalScore();
        Debug.Log($"[GameManager] 🏁 Game ended! Final score: {finalScore}");
    }
    
    // ===== Getter =====
    public GameState CurrentGameState => _gameState;
    public float GameTimer => _gameTimer;
    public bool IsGameRunning => _isGameRunning;
    public bool IsFrozen => _isFrozen;
}