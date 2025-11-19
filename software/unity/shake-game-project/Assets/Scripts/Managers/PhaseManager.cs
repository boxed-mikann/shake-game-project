using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// ========================================
/// PhaseManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：ゲームフェーズの時系列管理と切り替え
/// - GameConstants.PHASE_SEQUENCE を順次実行
/// - Coroutine で各フェーズの継続時間を管理
/// - フェーズ切り替え時に OnPhaseChanged イベント発行
/// 
/// イベント購読：
/// - GameManager.OnGameStart → フェーズシーケンス開始
/// 
/// イベント発行：
/// - OnPhaseChanged(PhaseChangeData) → ShakeResolver, NoteSpawner, UI層に通知
/// 
/// ========================================
/// </summary>
public class PhaseManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static PhaseManager _instance;
    public static PhaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhaseManager>();
            }
            return _instance;
        }
    }
    
    // フェーズ変更イベント（PhaseChangeData 構造体を引数）
    public static UnityEvent<PhaseChangeData> OnPhaseChanged = new UnityEvent<PhaseChangeData>();
    
    // 現在のフェーズ
    private Phase _currentPhase = Phase.NotePhase;
    private int _currentPhaseIndex = -1;
    
    // Coroutine 管理
    private Coroutine _phaseSequenceCoroutine = null;
    
    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PhaseManager] Initialized");
    }
    
    private void OnEnable()
    {
        // GameManager.OnGameStart を購読
        GameManager.OnGameStart.AddListener(OnGameStart);
        GameManager.OnShowTitle.AddListener(ResetPhaseManager);
    }
    
    private void OnDisable()
    {
        // イベント購読解除
        GameManager.OnGameStart.RemoveListener(OnGameStart);
        GameManager.OnShowTitle.RemoveListener(ResetPhaseManager);
    }
    
    /// <summary>
    /// ゲーム開始時のコールバック
    /// </summary>
    private void OnGameStart()
    {
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PhaseManager] Starting phase sequence...");
        
        // フェーズシーケンス開始
        if (_phaseSequenceCoroutine != null)
        {
            StopCoroutine(_phaseSequenceCoroutine);
        }
        _phaseSequenceCoroutine = StartCoroutine(ExecutePhaseSequence());
    }
    
    /// <summary>
    /// フェーズシーケンス実行
    /// PHASE_SEQUENCE を順に処理
    /// </summary>
    private IEnumerator ExecutePhaseSequence()
    {
        _currentPhaseIndex = -1;
        
        foreach (var phaseConfig in GameConstants.PHASE_SEQUENCE)
        {
            _currentPhaseIndex++;
            yield return StartCoroutine(ExecutePhase(phaseConfig, _currentPhaseIndex));
        }
        
        // 全フェーズ終了
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PhaseManager] All phases completed!");
        
        // ゲーム終了
        GameManager.EndGame();
    }
    
    /// <summary>
    /// 個別フェーズを実行
    /// </summary>
    private IEnumerator ExecutePhase(GameConstants.PhaseConfig config, int phaseIndex)
    {
        _currentPhase = config.phase;
        
        // spawnFrequency 計算（フェーズに応じた倍率適用）
        float spawnFrequency = GameConstants.BASE_SPAWN_FREQUENCY;
        
        // LastSprintPhase では生成速度を倍増
        if (config.phase == Phase.LastSprintPhase)
        {
            spawnFrequency /= GameConstants.LAST_SPRINT_MULTIPLIER;
        }
        
        // PhaseChangeData 構築
        PhaseChangeData phaseData = new PhaseChangeData
        {
            phaseType = config.phase,
            duration = config.duration,
            spawnFrequency = spawnFrequency,
            phaseIndex = phaseIndex
        };
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[PhaseManager] 🔄 Phase changed: {phaseData}");
        
        // OnPhaseChanged イベント発行
        OnPhaseChanged.Invoke(phaseData);
        
        // フェーズ継続時間だけ待機
        yield return new WaitForSeconds(config.duration);
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public Phase GetCurrentPhase()
    {
        return _currentPhase;
    }
    
    /// <summary>
    /// 現在のフェーズインデックスを取得
    /// </summary>
    public int GetCurrentPhaseIndex()
    {
        return _currentPhaseIndex;
    }
    
    /// <summary>
    /// PhaseManagerの状態をリセット
    /// タイトル画面復帰時に呼ばれる
    /// </summary>
    private void ResetPhaseManager()
    {
        // Coroutine停止
        if (_phaseSequenceCoroutine != null)
        {
            StopCoroutine(_phaseSequenceCoroutine);
            _phaseSequenceCoroutine = null;
        }
        
        // 状態変数リセット
        _currentPhaseIndex = -1;
        _currentPhase = Phase.NotePhase;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[PhaseManager] Reset to initial state");
    }
}