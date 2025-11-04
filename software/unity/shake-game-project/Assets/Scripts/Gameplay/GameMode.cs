using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// すべてのゲームモードの基底クラス
/// </summary>
public abstract class GameMode : MonoBehaviour
{
    [SerializeField] protected string modeName = "Game Mode";
    [SerializeField] protected string modeDescription = "Description";
    
    public event Action<int> OnScoreChanged;
    public event Action<int> OnPlayerWon;
    public event Action OnGameReset;
    
    protected bool isActive = false;
    
    public virtual void Initialize()
    {
        isActive = true;
        Debug.Log($"🎮 {modeName} initialized");
    }
    
    public abstract void OnShakeDetected(ShakeDataPacket shake);
    public abstract void ResetGame();
    
    public string GetModeName() => modeName;
    public string GetModeDescription() => modeDescription;
    
    protected void RaiseScoreChangedEvent(int playerId)
    {
        OnScoreChanged?.Invoke(playerId);
    }
    
    protected void RaisePlayerWonEvent(int winnerId)
    {
        OnPlayerWon?.Invoke(winnerId);
    }
    
    protected void RaiseGameResetEvent()
    {
        OnGameReset?.Invoke();
    }
}