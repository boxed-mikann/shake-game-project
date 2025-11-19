using UnityEngine;
using System.Collections.Concurrent;

/// <summary>
/// シェイク入力を現在のハンドラーに振り分け（統一キュー方式）
/// Strategyパターン：フェーズ変更時にハンドラーを差し替え
/// フリーズ対応：2段階ハンドラー切り替え（_currentHandler / _activeHandler）
/// </summary>
public class ShakeResolver : MonoBehaviour
{
    // ★ 統一された入力キュー（static）
    private static ConcurrentQueue<(string data, double timestamp)> _sharedInputQueue 
        = new ConcurrentQueue<(string data, double timestamp)>();
    
    // ★ 外部から入力を追加するメソッド
    public static void EnqueueInput(string data, double timestamp)
    {
        _sharedInputQueue.Enqueue((data, timestamp));
    }
    
    [Header("Freeze & Phase Handlers")]
    [SerializeField] private NullShakeHandler _nullHandler;
    [SerializeField] private NoteShakeHandler _noteHandler;
    [SerializeField] private RestShakeHandler _restHandler;
    
    private IShakeHandler _currentHandler;   // Update()で呼ばれる最終ハンドラー
    private IShakeHandler _activeHandler;    // 通常時のハンドラー（変数のみ）
    
    void Start()
    {
        // 初期状態：nullに設定（OnPhaseChangedで設定される）
        _currentHandler = null;
        
        // イベント購読
        FreezeManager.OnFreezeChanged.AddListener(OnFreezeChanged);
        PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        GameManager.OnShowTitle.AddListener(ResetResolver);
    }
    
    void Update()
    {
        // ★ 統一キューから取り出して処理
        while (_sharedInputQueue.TryDequeue(out var input))
        {
            _currentHandler?.HandleShake(input.data, input.timestamp);
        }
    }
    
    void OnFreezeChanged(bool isFrozen)
    {
        // フリーズ層の切り替え
        _currentHandler = isFrozen ? _nullHandler : _activeHandler;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Freeze: {isFrozen}, Handler: {_currentHandler?.GetType().Name}");
    }
    
    void OnPhaseChanged(PhaseChangeData data)
    {
        // フェーズ層の切り替え（_activeHandlerを変更）
        switch (data.phaseType)
        {
            case Phase.NotePhase:
                _activeHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.NOTE_SCORE);
                break;
            case Phase.LastSprintPhase:
                _activeHandler = _noteHandler;
                _noteHandler.SetScoreValue(GameConstants.LAST_SPRINT_SCORE);
                break;
            case Phase.RestPhase:
                _activeHandler = _restHandler;
                break;
        }
        
        // ★ 重要：フリーズ中でない場合のみ_currentHandlerを更新
        // （フリーズ中は_nullHandlerのまま、解除時にOnFreezeChangedで更新される）
        if (FreezeManager.Instance != null && !FreezeManager.Instance.IsFrozen)
        {
            _currentHandler = _activeHandler;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[ShakeResolver] Phase changed, active: {_activeHandler?.GetType().Name}");
    }
    
    private void ResetResolver()
    {
        // ★ 統一キューをクリア
        while (_sharedInputQueue.TryDequeue(out _)) { }
        // ハンドラーはOnPhaseChangedで再設定される
    }
    
    void OnDestroy()
    {
        FreezeManager.OnFreezeChanged.RemoveListener(OnFreezeChanged);
        PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        GameManager.OnShowTitle.RemoveListener(ResetResolver);
    }
}