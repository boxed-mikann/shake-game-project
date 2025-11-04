using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum GamePhase
{
    Menu,
    Playing,
    Victory,
    Result
}

public class GamePhaseManager : MonoBehaviour
{
    private GamePhase currentPhase = GamePhase.Menu;
    
    public event Action<GamePhase> OnPhaseChanged;
    
    void Start()
    {
        SetPhase(GamePhase.Menu);
    }
    
    public void SetPhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase)
            return;
        
        Debug.Log($"🔄 Phase: {currentPhase} → {newPhase}");
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
    }
    
    public GamePhase GetCurrentPhase() => currentPhase;
    public bool IsPlaying() => currentPhase == GamePhase.Playing;
}