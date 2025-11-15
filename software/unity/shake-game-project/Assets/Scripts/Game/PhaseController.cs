using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// フェーズ管理 - 音符フェーズ ↔ 休符フェーズを自動切り替え
/// 責務：フェーズの定期切り替え、すべての音符に現在のフェーズを通知
/// </summary>
public enum Phase { NotePhase, RestPhase }

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
    private bool _isGameRunning = false;
    
    // イベント
    public delegate void OnPhaseChangedEvent(Phase newPhase);
    public event OnPhaseChangedEvent OnPhaseChanged;
    
    public void Initialize()
    {
        _currentPhase = Phase.NotePhase;
        _phaseTimer = 0f;
        _isGameRunning = true;
    }
    
    public void StopGame()
    {
        _isGameRunning = false;
    }
    
    private void Update()
    {
        if (!_isGameRunning)
            return;
        
        _phaseTimer += Time.deltaTime;
        
        if (_phaseTimer >= GameConstants.PHASE_DURATION)
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
        _currentPhase = (_currentPhase == Phase.NotePhase) ? Phase.RestPhase : Phase.NotePhase;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[PhaseController] 🔄 Phase switched to: {_currentPhase}");
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
    
    public Phase GetCurrentPhase() => _currentPhase;
    public float GetPhaseProgress() => _phaseTimer / GameConstants.PHASE_DURATION;
}