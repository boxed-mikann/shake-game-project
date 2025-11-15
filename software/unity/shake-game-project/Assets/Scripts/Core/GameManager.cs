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
    [SerializeField] private AudioClip burstSoundClip;  // 音符破裂音
    [SerializeField] private GameObject panelWarning;   // ビジュアル警告（フリーズ時に表示）
    
    private AudioSource _audioSource;                   // 音声再生用（事前生成で遅延回避）
    
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
        // AudioSource の初期化（遅延回避のため）
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        // panelWarning を非表示にする
        if (panelWarning != null)
        {
            panelWarning.SetActive(false);
        }
        
        ScoreManager.Instance.Initialize();
        PhaseController.Instance.Initialize();
        
        // PhaseIndicatorSlider をリセット
        PhaseIndicatorSlider[] sliders = FindObjectsOfType<PhaseIndicatorSlider>();
        foreach (var slider in sliders)
        {
            slider.Reset();
        }
        
        OnGameStateChanged?.Invoke(_gameState);
        
        if (GameConstants.DEBUG_MODE)
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
            PhaseController.Instance.EnterLastSprint();
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[GameManager] ⚡ Last sprint! Spawn rate x2, Phase switching disabled");
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
                
                // PanelWarning は TriggerFreeze() で非表示にする（ゲーム進行中のみ表示）
                // ゲーム終了/開始時には StartGame() で非表示にする
                
                if (GameConstants.DEBUG_MODE)
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
        
        // 生成数上限チェック
        if (notesContainer.childCount >= GameConstants.MAX_NOTE_COUNT)
        {
            return;
        }
        
        // 休符フェーズで既に Note が存在する場合は生成しない
        if (PhaseController.Instance.GetCurrentPhase() == Phase.RestPhase && notesContainer.childCount > 0)
        {
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
    /// 音符を1個スポーン - 回転とランダムカラー付き
    /// </summary>
    private void SpawnNote()
    {
        Vector3 randomPos = new Vector3(
            Random.Range(-6f, 6f),
            Random.Range(-4f, 4f),
            0f
        );
        
        // ±30度の範囲でランダムに回転
        float randomRotation = Random.Range(-30f, 30f);
        Quaternion rotationQuaternion = Quaternion.Euler(0f, 0f, randomRotation);
        
        GameObject noteGO = Instantiate(notePrefab, randomPos, rotationQuaternion, notesContainer);
        
        // ランダムカラー設定（SpriteRenderer がある場合）
        SpriteRenderer sr = noteGO.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = GetRandomColor();
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] 🎵 Note spawned at {randomPos}, rotation: {randomRotation}°");
        }
    }
    
    /// <summary>
    /// ランダムカラーを取得
    /// </summary>
    private Color GetRandomColor()
    {
        Color[] colors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f),     // Orange
            new Color(0.5f, 0f, 0.5f)    // Purple
        };
        
        return colors[Random.Range(0, colors.Length)];
    }
    
    /// <summary>
    /// シェイク入力を処理 - 既存の音符を破壊してスコア更新
    /// </summary>
    private void OnShakeInput(int deviceId, int shakeCount, float acceleration)
    {
        if (_gameState != GameState.Playing || _isFrozen)
            return;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] 📊 Shake input: DeviceID={deviceId}, Count={shakeCount}, Accel={acceleration}");
        }
        
        // 画面上に存在する音符を探す
        NotePrefab[] allNotes = FindObjectsOfType<NotePrefab>();
        
        if (allNotes.Length == 0)
        {
            Debug.Log("[GameManager] No notes to destroy");
            return;
        }
        
        // 最新（最後に生成された）の音符を取得
        NotePrefab targetNote = allNotes[allNotes.Length - 1];
        Phase currentPhase = PhaseController.Instance.GetCurrentPhase();
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] 💥 Destroying note (Phase: {currentPhase})");
        }
        
        if (currentPhase == Phase.NotePhase)
        {
            // 音符フェーズ → スコア加算
            ScoreManager.Instance.AddNoteScore(1);
        }
        else if (currentPhase == Phase.RestPhase)
        {
            // 休符フェーズ → ペナルティ＋フリーズ
            ScoreManager.Instance.SubtractRestPenalty(1);
            TriggerFreeze();
        }
        
        // 破裂音を再生
        PlayBurstSound(targetNote.transform.position);
        
        // 音符を破壊
        Destroy(targetNote.gameObject);
    }
    
    /// <summary>
    /// 破裂音を再生
    /// </summary>
    private void PlayBurstSound(Vector3 position)
    {
        if (burstSoundClip == null || _audioSource == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning("[GameManager] burstSoundClip or _audioSource is not assigned!");
            return;
        }
        
        // 事前割り当て済みの AudioSource を使用して再生
        _audioSource.transform.position = position;
        _audioSource.PlayOneShot(burstSoundClip, 0.7f);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[GameManager] 🔊 Burst sound played");
    }
    
    /// <summary>
    /// フリーズ効果を発動（入力ロック + ビジュアルフィードバック）
    /// ラストスパート中には表示しない（ラストスパートフェーズが優先）
    /// </summary>
    public void TriggerFreeze()
    {
        if (_isFrozen)
            return;
        
        _isFrozen = true;
        _freezeRemainingTime = GameConstants.INPUT_LOCK_DURATION;
        
        // ビジュアルフィードバック：警告パネルを表示（ラストスパート中は表示しない）
        if (panelWarning != null && _gameTimer > GameConstants.LAST_SPRINT_DURATION)
        {
            panelWarning.SetActive(true);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[GameManager] ⏸️ Freeze triggered! PanelWarning shown for {GameConstants.INPUT_LOCK_DURATION}s");
        }
        else if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] ⏸️ Freeze triggered! (PanelWarning suppressed in LastSprint)");
        }
    }
    
    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void EndGame()
    {
        _isGameRunning = false;
        Time.timeScale = 1f;  // フリーズを解除
        PhaseController.Instance.StopGame();
        
        // 画面上の音符をすべてクリーンアップ
        foreach (Transform child in notesContainer)
        {
            Destroy(child.gameObject);
        }
        
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