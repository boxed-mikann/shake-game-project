using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ゲームフェーズの定義
/// NotePhase: 音符フェーズ（音符を叩くと加点）
/// RestPhase: 休符フェーズ（音符を叩くとペナルティ＋フリーズ）
/// LastSprintPhase: ラストスパント（最後10秒、生成速度2倍）
/// </summary>
public enum Phase { NotePhase, RestPhase, LastSprintPhase }

/// <summary>
/// ゲーム状態の定義
/// </summary>
public enum GameState { Start, Playing, Result }

/// <summary>
/// ========================================
/// アーキテクチャ概要
/// ========================================
/// 
/// ◎ GameManager
///   - ゲーム進行・タイマー管理
///   - フェーズシーケンス生成・管理（OnPhaseChanged イベント発火）
///   - 入力処理（シェイク入力 → 音符破壊 → スコア更新）
///   - フリーズ効果（入力ロック + PanelWarning 表示）
/// 
/// ◎ OnPhaseChanged イベント
///   - フェーズ変更時に全システムに (Phase, duration) を通知
///   - 購読者：NotePrefab（画像更新）、UIManager（表示更新）
///   - 毎フレーム GetPhaseAtTime() を呼ぶ無駄を削除（イベント駆動化）
/// 
/// ◎ NotePrefab
///   - GameManager.OnPhaseChanged を購読
///   - フェーズ変更時に自動的に Sprite 更新
///   - 見た目管理に特化
/// 
/// ◎ UIManager（PhaseIndicatorSlider 統合）
///   - GameManager.OnPhaseChanged を購読
///   - フェーズテキスト + スライダー色を更新
///   - スライダー値は毎フレーム GetPhaseProgress() で計算
/// 
/// ◎ GameConstants.PHASE_SEQUENCE
///   - フェーズシーケンスの定義（配列型）
///   - ゲーム調整時は継続時間をここで変更
///   - GameManager が Initialize() で PHASE_SEQUENCE を展開
/// 
/// ⚡ パフォーマンス特性
///   - GetPhaseAtTime() 呼び出し：フェーズ変更時のみ（60→1/秒）
///   - FindObjectsOfType() 呼び出し：削除（入力時のみ必要な場合）
///   - イベント駆動設計により CPU 負荷軽減
/// 
/// ========================================
/// </summary>
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
    
    // フェーズ管理（PhaseSequence から統合）
    // GameConstants.PHASE_SEQUENCE を展開して、(Phase, startTime, duration) の List を構築
    private List<(Phase phase, float startTime, float duration)> _phaseSegments = new List<(Phase, float, float)>();
    private Phase _lastPhase = Phase.NotePhase;
    
    // イベント
    public delegate void OnGameStateChangedEvent(GameState newState);
    public event OnGameStateChangedEvent OnGameStateChanged;
    
    /// <summary>
    /// フェーズ変更イベント
    /// 購読者：NotePrefab（フェーズ画像更新）、UIManager（テキスト＆スライダー色更新）
    /// </summary>
    public delegate void OnPhaseChangedEvent(Phase newPhase, float duration);
    public event OnPhaseChangedEvent OnPhaseChanged;
    
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
        _lastPhase = Phase.NotePhase;
        
        // panelWarning を非表示にする
        if (panelWarning != null)
        {
            panelWarning.SetActive(false);
        }
        
        ScoreManager.Instance.Initialize();
        
        // フェーズシーケンスを初期化
        InitializePhaseSequence(GameConstants.GAME_DURATION);
        
        OnGameStateChanged?.Invoke(_gameState);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[GameManager] ▶️ Game started!");
    }
    
    /// <summary>
    /// ゲームタイマー更新
    /// 
    /// 手順：
    ///   1. タイマー減少（GameTimer = 60 → 0）
    ///   2. フェーズ検知：GetPhaseAtTime() で現在フェーズを取得
    ///   3. フェーズ変更判定：前フレーム (_lastPhase) と比較
    ///   4. フェーズ変更時：OnPhaseChanged イベント発火
    ///      → UIManager が OnPhaseChanged を購読してテキスト＆スライダー色を更新
    ///      → NotePrefab が OnPhaseChanged を購読して画像を更新
    ///   5. ラストスパート判定：GameTimer ≤ 10s で生成速度 2 倍
    ///   6. タイムアップ判定：GameTimer ≤ 0 で EndGame() 呼び出し
    /// 
    /// ⚡ パフォーマンス特性
    ///   - GetPhaseAtTime() 呼び出し：毎フレーム（1 回 O(n)、n=フェーズ数）
    ///   - フェーズ変更検知：毎フレーム（値比較のみ O(1)）
    ///   - OnPhaseChanged 発火：フェーズ変更時のみ（毎ゲーム約 4-5 回）
    /// </summary>
    private void UpdateGameTimer()
    {
        _gameTimer -= Time.deltaTime;
        
        // フェーズ変更を検出
        float elapsedTime = GameConstants.GAME_DURATION - _gameTimer;
        Phase currentPhase = GetPhaseAtTime(elapsedTime);
        
        if (currentPhase != _lastPhase)
        {
            _lastPhase = currentPhase;
            var seg = GetSegmentAtTime(elapsedTime);
            OnPhaseChanged?.Invoke(currentPhase, seg.duration);
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log($"[GameManager] 🔄 Phase changed to: {currentPhase} (duration: {seg.duration:F1}s)");
            }
        }
        
        // フェーズに応じた生成速度を更新（LastSprintPhase なら 2 倍）
        _currentSpawnRate = currentPhase == Phase.LastSprintPhase
            ? (int)(GameConstants.SPAWN_RATE_BASE * GameConstants.LAST_SPRINT_MULTIPLIER)
            : GameConstants.SPAWN_RATE_BASE;
        
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
                
                // フリーズ終了時にPanelWarningを非表示
                if (panelWarning != null)
                {
                    panelWarning.SetActive(false);
                }
                
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
        var segment = GetCurrentSegment();
        if (segment.phase == Phase.RestPhase && notesContainer.childCount > 0)
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
        var segment = GetCurrentSegment();
        Phase currentPhase = segment.phase;
        
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
        
        // ビジュアルフィードバック：警告パネルを表示（LastSprintPhase 中も関係なく凍結される）
        var currentSegment = GetCurrentSegment();
        if (panelWarning != null )
        {
            panelWarning.SetActive(true);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[GameManager] ⏸️ Freeze triggered! PanelWarning shown for {GameConstants.INPUT_LOCK_DURATION}s");
        }
    }
    
    /// <summary>
    /// <summary>
    /// フェーズシーケンスを初期化（GameConstants.PHASE_SEQUENCE に基づく）
    /// 
    /// アルゴリズム：
    ///   1. PHASE_SEQUENCE の要素を順番に _phaseSegments に展開
    ///   2. 各要素は (Phase, startTime, duration) のタプルに変換
    ///   3. LastSprintPhase は PHASE_SEQUENCE に明示的に含まれる
    /// 
    /// 例：PHASE_SEQUENCE = [10s Note, 5s Rest, ..., 15s LastSprint]
    ///   _phaseSegments = [
    ///     (Note, 0, 10), (Rest, 10, 5),
    ///     (Note, 15, 10), (Rest, 25, 5),
    ///     ...
    ///     (LastSprint, 50, 15)
    ///   ]
    /// </summary>
    private void InitializePhaseSequence(float gameDuration)
    {
        _phaseSegments.Clear();
        
        float currentTime = 0f;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[GameManager] Initializing phase sequence: gameDuration={gameDuration}");
        }
        
        // PHASE_SEQUENCE の要素を順番に _phaseSegments に展開
        foreach (var config in GameConstants.PHASE_SEQUENCE)
        {
            _phaseSegments.Add((config.phase, currentTime, config.duration));
            currentTime += config.duration;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[GameManager] ✅ Phase sequence initialized:");
            foreach (var seg in _phaseSegments)
            {
                Debug.Log($"  [{seg.startTime:F1}s-{seg.startTime + seg.duration:F1}s] {seg.phase} ({seg.duration:F1}s)");
            }
        }
    }
    
    /// <summary>
    /// 指定時刻のフェーズセグメントを取得
    /// 
    /// 用途：フェーズ変更検知、スライダー表示、ログ出力
    /// 
    /// 戻り値：(Phase, startTime, duration) のタプル
    ///   - Phase：フェーズ種別
    ///   - startTime：セグメント開始時刻（秒）
    ///   - duration：セグメント継続時間（秒）
    /// </summary>
    private (Phase phase, float startTime, float duration) GetSegmentAtTime(float elapsedTime)
    {
        foreach (var seg in _phaseSegments)
        {
            if (elapsedTime >= seg.startTime && elapsedTime < seg.startTime + seg.duration)
            {
                return seg;
            }
        }
        
        // デフォルトはラストセグメント
        if (_phaseSegments.Count > 0)
            return _phaseSegments[_phaseSegments.Count - 1];
        
        return (Phase.NotePhase, 0f, 1f);
    }
    
    /// <summary>
    /// 指定時刻のフェーズを取得
    /// 
    /// 用途：フェーズ検知（UpdateGameTimer で _lastPhase と比較）
    /// 効率：GetSegmentAtTime の Wrapper（戻り値から Phase のみ抽出）
    /// </summary>
    private Phase GetPhaseAtTime(float elapsedTime)
    {
        var seg = GetSegmentAtTime(elapsedTime);
        return seg.phase;
    }
    
    /// <summary>
    /// 現在のフェーズセグメント内での進度（0～1）を取得
    /// 
    /// 用途：UIManager がスライダー値を計算（毎フレーム）
    /// 計算：(経過時刻 - セグメント開始時刻) / セグメント継続時間
    /// 例：Note フェーズ内で 3 秒経過した場合：3 / 10 = 0.3
    /// </summary>
    public float GetPhaseProgress()
    {
        if (_phaseSegments.Count == 0)
            return 0f;
        
        float elapsedTime = GameConstants.GAME_DURATION - _gameTimer;
        var seg = GetSegmentAtTime(elapsedTime);
        
        if (seg.duration <= 0)
            return 0f;
        
        float elapsed = elapsedTime - seg.startTime;
        return Mathf.Clamp01(elapsed / seg.duration);
    }
    
    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void EndGame()
    {
        _isGameRunning = false;
        
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
    
    /// <summary>
    /// 現在のフェーズセグメントを取得（公開：UIManager / NotePrefab 用）
    /// 
    /// 用途：
    ///   - NotePrefab.Start()：初期フェーズを取得して SetPhase() 実行
    ///   - UIManager.OnPhaseChanged()：フェーズ変更後のセグメント情報を取得
    /// 
    /// 戻り値：(Phase, startTime, duration) のタプル
    /// </summary>
    public (Phase phase, float startTime, float duration) GetCurrentSegment()
    {
        float elapsedTime = GameConstants.GAME_DURATION - _gameTimer;
        return GetSegmentAtTime(elapsedTime);
    }
    
    // ===== Getter =====
    public GameState CurrentGameState => _gameState;
    public float GameTimer => _gameTimer;
    public bool IsGameRunning => _isGameRunning;
    public bool IsFrozen => _isFrozen;
}