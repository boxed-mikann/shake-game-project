using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// フェーズ管理 - 音符フェーズ ↔ 休符フェーズを自動切り替え
/// ラストスパートフェーズは最優先
/// 責務：フェーズの定期切り替え、すべての音符に現在のフェーズを通知
/// </summary>
public enum Phase { NotePhase, RestPhase, LastSprintPhase }

public class PhaseController : MonoBehaviour
{
    private static PhaseController _instance;
    public static PhaseController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PhaseController>();
            }
            return _instance;
        }
    }

    private Phase _currentPhase = Phase.NotePhase;
    private float _phaseTimer = 0f;
    private float _currentPhaseDuration = 0f;
    private bool _isGameRunning = false;
    private bool _isLastSprint = false;
    
    // イベント
    public delegate void OnPhaseChangedEvent(Phase newPhase);
    public event OnPhaseChangedEvent OnPhaseChanged;
    
    public void Initialize()
    {
        _currentPhase = Phase.NotePhase;
        _phaseTimer = 0f;
        _currentPhaseDuration = GameConstants.NOTE_PHASE_DURATION;
        _isGameRunning = true;
        _isLastSprint = false;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PhaseController] ✅ Reinitialized - Starting with NotePhase");
        }
    }
    
    public void StopGame()
    {
        _isGameRunning = false;
    }
    
    /// <summary>
    /// ラストスパートフェーズに入る
    /// </summary>
    public void EnterLastSprint()
    {
        _isLastSprint = true;
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PhaseController] ⚡ Entered LastSprint phase!");
        }
    }
    
    private void Update()
    {
        if (!_isGameRunning)
            return;
        
        _phaseTimer += Time.deltaTime;
        
        if (_phaseTimer >= _currentPhaseDuration)
        {
            SwitchPhase();
            _phaseTimer = 0f;
        }
    }
    
    /// <summary>
    /// フェーズを切り替え
    /// </summary>
    private void SwitchPhase()
    {
        // ラストスパート時は通常フェーズ切り替えをスキップ
        if (_isLastSprint)
        {
            // ラストスパートフェーズは音符フェーズのままループ
            _phaseTimer = 0f;
            return;
        }
        
        _currentPhase = (_currentPhase == Phase.NotePhase) ? Phase.RestPhase : Phase.NotePhase;
        
        // 次のフェーズ時間を計算（短縮倍率を適用）
        if (_currentPhase == Phase.NotePhase)
        {
            _currentPhaseDuration = Mathf.Max(GameConstants.NOTE_PHASE_DURATION * GameConstants.PHASE_SHORTENING_RATE, GameConstants.PHASE_DURATION_MIN);
        }
        else
        {
            _currentPhaseDuration = Mathf.Max(GameConstants.REST_PHASE_DURATION * GameConstants.PHASE_SHORTENING_RATE, GameConstants.PHASE_DURATION_MIN);
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[PhaseController] 🔄 Phase switched to: {_currentPhase}, Duration: {_currentPhaseDuration:F2}s");
        }
        
        // すべての Note に現在のフェーズを通知
        NotePrefab[] allNotes = FindObjectsOfType<NotePrefab>();
        foreach (var note in allNotes)
        {
            note.SetPhase(_currentPhase);
        }
        
        // イベント発火
        OnPhaseChanged?.Invoke(_currentPhase);
    }
    
    public Phase GetCurrentPhase()
    {
        // ラストスパート中は常に LastSprintPhase を返す（最優先）
        if (_isLastSprint)
            return Phase.LastSprintPhase;
        
        return _currentPhase;
    }
    
    public float GetPhaseProgress() => _currentPhaseDuration > 0 ? _phaseTimer / _currentPhaseDuration : 0f;
    public float GetCurrentPhaseDuration() => _currentPhaseDuration;
    public float GetPhaseRemainingTime() => Mathf.Max(0f, _currentPhaseDuration - _phaseTimer);
}