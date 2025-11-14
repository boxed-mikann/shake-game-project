using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// タイミングシステム - リズムビート生成・同期判定
/// </summary>
public class TimingSystem : MonoBehaviour
{
    // ===== Singleton =====
    private static TimingSystem _instance;
    public static TimingSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimingSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TimingSystem");
                    _instance = go.AddComponent<TimingSystem>();
                }
            }
            return _instance;
        }
    }

    // ===== ビート管理 =====
    private float _beatCounter = 0f;
    private int _currentBeatIndex = 0;
    private List<TimingEvent> _timingEvents = new List<TimingEvent>();
    
    // ===== 同期判定 =====
    private Dictionary<int, float> _playerLastShakeTime = new Dictionary<int, float>();
    private List<int> _currentSyncPlayers = new List<int>();
    private float _lastSyncCheckTime = -1f;
    
    private void Awake()
    {
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
        GenerateTimingEvents();
    }
    
    private void Update()
    {
        if (!GameManager.Instance.IsGameRunning)
            return;
        
        float gameTime = GameManager.Instance.GameElapsedTime;
        
        // ビート更新
        UpdateBeat(gameTime);
        
        // 同期判定
        CheckSyncTiming(gameTime);
    }
    
    /// <summary>
    /// ゲーム開始時にタイミングイベントを生成
    /// </summary>
    private void GenerateTimingEvents()
    {
        _timingEvents.Clear();
        
        // 0-5秒: 練習フェーズ - 通常タイミング
        for (float t = 1f; t < GameConstants.PRACTICE_PHASE_DURATION; t += GameConstants.BEAT_DURATION)
        {
            _timingEvents.Add(new TimingEvent
            {
                eventTime = t,
                type = TimingType.Normal,
                windowStart = t - GameConstants.TIMING_WINDOW / 2f,
                windowEnd = t + GameConstants.TIMING_WINDOW / 2f
            });
        }
        
        // 5-20秒: 高ダメージ期 - ビート表示のみ
        float highDamageStart = GameConstants.PRACTICE_PHASE_DURATION;
        float highDamageEnd = GameConstants.PRACTICE_PHASE_DURATION + GameConstants.HIGH_DAMAGE_PHASE_DURATION;
        for (float t = highDamageStart + GameConstants.BEAT_DURATION; t < highDamageEnd; t += GameConstants.BEAT_DURATION)
        {
            _timingEvents.Add(new TimingEvent
            {
                eventTime = t,
                type = TimingType.Normal,
                windowStart = t - GameConstants.TIMING_WINDOW / 2f,
                windowEnd = t + GameConstants.TIMING_WINDOW / 2f
            });
        }
        
        // 20-30秒: 同期期 - 同期タイミングを生成
        float syncStart = GameConstants.PRACTICE_PHASE_DURATION + GameConstants.HIGH_DAMAGE_PHASE_DURATION;
        float syncEnd = GameConstants.TOTAL_GAME_TIME;
        for (float t = syncStart + GameConstants.BEAT_DURATION * 2; t < syncEnd; t += GameConstants.BEAT_DURATION * 4)
        {
            _timingEvents.Add(new TimingEvent
            {
                eventTime = t,
                type = TimingType.Sync,
                windowStart = t - GameConstants.SYNC_WINDOW / 2f,
                windowEnd = t + GameConstants.SYNC_WINDOW / 2f
            });
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[TimingSystem] Generated {_timingEvents.Count} timing events");
        }
    }
    
    /// <summary>
    /// ビートを更新
    /// </summary>
    private void UpdateBeat(float gameTime)
    {
        _beatCounter = (gameTime % GameConstants.BEAT_DURATION) / GameConstants.BEAT_DURATION;
        _currentBeatIndex = (int)(gameTime / GameConstants.BEAT_DURATION);
    }
    
    /// <summary>
    /// 同期タイミングを判定
    /// </summary>
    private void CheckSyncTiming(float gameTime)
    {
        // 0.1秒ごとにチェック
        if (gameTime - _lastSyncCheckTime < 0.1f)
            return;
        
        _lastSyncCheckTime = gameTime;
        
        GamePhase currentPhase = GameManager.Instance.CurrentPhase;
        
        if (currentPhase != GamePhase.BossSyncRequired)
            return;
        
        // 現在の同期タイミングイベントを取得
        TimingEvent currentSyncEvent = null;
        foreach (var evt in _timingEvents)
        {
            if (evt.type == TimingType.Sync && 
                gameTime >= evt.windowStart && 
                gameTime <= evt.windowEnd)
            {
                currentSyncEvent = evt;
                break;
            }
        }
        
        if (currentSyncEvent == null)
            return;
        
        // ウィンドウ内のプレイヤーをチェック
        _currentSyncPlayers.Clear();
        
        foreach (var playerEntry in GameManager.Instance.Players)
        {
            int childId = playerEntry.Key;
            PlayerData player = playerEntry.Value;
            
            // このプレイヤーが最近シェイクしたかチェック
            if (_playerLastShakeTime.ContainsKey(childId))
            {
                float timeSinceShake = gameTime - _playerLastShakeTime[childId];
                
                // タイミングウィンドウ内でシェイクしたプレイヤー
                if (timeSinceShake >= 0 && timeSinceShake <= GameConstants.SYNC_WINDOW)
                {
                    _currentSyncPlayers.Add(childId);
                }
            }
        }
        
        // 十分なプレイヤーが同期した場合
        int syncPlayerThreshold = Mathf.Max(1, GameManager.Instance.Players.Count / 2);
        if (_currentSyncPlayers.Count >= syncPlayerThreshold && _currentSyncPlayers.Count > 0)
        {
            currentSyncEvent.isSuccessful = true;
            GameManager.Instance.OnSyncSuccess(_currentSyncPlayers.Count);
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log($"[TimingSystem] Sync success! {_currentSyncPlayers.Count} players synchronized");
            }
        }
    }
    
    /// <summary>
    /// プレイヤーのシェイク入力を記録
    /// </summary>
    public void RecordPlayerShake(int childId, float gameTime)
    {
        _playerLastShakeTime[childId] = gameTime;
    }
    
    /// <summary>
    /// 現在のビート進捗を返す (0-1)
    /// </summary>
    public float GetCurrentBeatProgress()
    {
        return _beatCounter;
    }
    
    /// <summary>
    /// タイミングシステムをリセット
    /// </summary>
    public void Reset()
    {
        _beatCounter = 0f;
        _currentBeatIndex = 0;
        _playerLastShakeTime.Clear();
        _currentSyncPlayers.Clear();
        _lastSyncCheckTime = -1f;
        GenerateTimingEvents();
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[TimingSystem] Reset");
        }
    }
    
    // ===== Getter =====
    public float BeatProgress => _beatCounter;
    public int CurrentBeatIndex => _currentBeatIndex;
    public List<TimingEvent> TimingEvents => _timingEvents;
}

/// <summary>
/// タイミングイベント
/// </summary>
public class TimingEvent
{
    public float eventTime;          // ゲーム内時刻
    public TimingType type;          // タイミングの種類
    public float windowStart;        // 判定ウィンドウ開始
    public float windowEnd;          // 判定ウィンドウ終了
    public bool isSuccessful = false; // 成功したかどうか
}
