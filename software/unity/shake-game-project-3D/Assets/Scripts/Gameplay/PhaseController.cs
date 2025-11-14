using UnityEngine;

/// <summary>
/// ゲームフェーズを管理するコントローラー
/// 時間に応じてフェーズを切り替え、各フェーズのロジックを担当
/// </summary>
public class PhaseController : MonoBehaviour
{
    // ===== Singleton =====
    private static PhaseController _instance;
    public static PhaseController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhaseController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PhaseController");
                    _instance = go.AddComponent<PhaseController>();
                }
            }
            return _instance;
        }
    }

    // ===== フェーズ時間管理 =====
    private float _practiceEndTime = GameConstants.PRACTICE_PHASE_DURATION;
    private float _highDamageEndTime = GameConstants.PRACTICE_PHASE_DURATION + GameConstants.HIGH_DAMAGE_PHASE_DURATION;
    private float _totalGameTime = GameConstants.TOTAL_GAME_TIME;
    
    // ===== タイミングシステム =====
    private TimingSystem _timingSystem;
    private int _lastSyncCheckTime = -1;
    
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
        _timingSystem = TimingSystem.Instance;
    }
    
    /// <summary>
    /// 与えられた時刻に対応するゲームフェーズを返す
    /// </summary>
    public GamePhase GetPhaseForTime(float gameTime)
    {
        if (gameTime < _practiceEndTime)
        {
            return GamePhase.Practice;
        }
        else if (gameTime < _highDamageEndTime)
        {
            return GamePhase.BossHighDamage;
        }
        else
        {
            return GamePhase.BossSyncRequired;
        }
    }
    
    /// <summary>
    /// 現在のフェーズでシェイク1回当たりのダメージを計算
    /// </summary>
    public float CalculateDamageForCurrentPhase(int shakeCount)
    {
        GamePhase currentPhase = GameManager.Instance.CurrentPhase;
        float baseDamage = GameConstants.NORMAL_DAMAGE_PER_SHAKE * shakeCount;
        
        // フェーズによってダメージを調整
        switch (currentPhase)
        {
            case GamePhase.Practice:
                // 練習フェーズは低ダメージ
                return baseDamage * 0.5f;
            
            case GamePhase.BossHighDamage:
                // 高ダメージ期はフルダメージ
                return baseDamage;
            
            case GamePhase.BossSyncRequired:
                // 同期期は低ダメージ（同期で大ダメージ狙い）
                return baseDamage * 0.3f;
            
            default:
                return baseDamage;
        }
    }
    
    /// <summary>
    /// 現在のタイミングが同期タイミングか判定
    /// </summary>
    public bool IsCurrentlySyncTiming()
    {
        GamePhase currentPhase = GameManager.Instance.CurrentPhase;
        return currentPhase == GamePhase.BossSyncRequired;
    }
    
    /// <summary>
    /// フェーズをリセット
    /// </summary>
    public void Reset()
    {
        if (_timingSystem != null)
        {
            _timingSystem.Reset();
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PhaseController] Reset");
        }
    }
    
    // ===== Getter =====
    public float PracticeEndTime => _practiceEndTime;
    public float HighDamageEndTime => _highDamageEndTime;
    public float TotalGameTime => _totalGameTime;
}
