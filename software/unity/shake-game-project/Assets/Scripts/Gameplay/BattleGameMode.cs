using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 基本モード：2チーム対戦
/// </summary>
public class BattleGameMode : GameMode
{
    [SerializeField] private Image team0Gauge;
    [SerializeField] private Image team1Gauge;
    
    private int team0Score = 0;
    private int team1Score = 0;
    
    void Start()
    {
        modeName = "Battle Mode";
        modeDescription = "2チーム対戦 - 100点先取";
    }
    
    public override void Initialize()
    {
        base.Initialize();
        team0Score = 0;
        team1Score = 0;
        UpdateGaugeUI();
    }
    
    public override void OnShakeDetected(ShakeDataPacket shake)
    {
        if (!isActive) return;
        
        int scoreIncrease = ScoreCalculator.AccelerationToScore(shake.acceleration);
        
        if (shake.childID == 0)
        {
            team0Score += scoreIncrease;
            Debug.Log($"🎮 Team 0: +{scoreIncrease} (Total: {team0Score})");
            RaiseScoreChangedEvent(0);
            
            if (team0Score >= Constants.MAX_SCORE)
            {
                isActive = false;
                RaisePlayerWonEvent(0);
            }
        }
        else if (shake.childID == 1)
        {
            team1Score += scoreIncrease;
            Debug.Log($"🎮 Team 1: +{scoreIncrease} (Total: {team1Score})");
            RaiseScoreChangedEvent(1);
            
            if (team1Score >= Constants.MAX_SCORE)
            {
                isActive = false;
                RaisePlayerWonEvent(1);
            }
        }
        
        UpdateGaugeUI();
    }
    
    public override void ResetGame()
    {
        team0Score = 0;
        team1Score = 0;
        isActive = false;
        UpdateGaugeUI();
        RaiseGameResetEvent();
    }
    
    private void UpdateGaugeUI()
    {
        if (team0Gauge != null)
            team0Gauge.fillAmount = Mathf.Min(team0Score / Constants.MAX_SCORE, 1f);
        
        if (team1Gauge != null)
            team1Gauge.fillAmount = Mathf.Min(team1Score / Constants.MAX_SCORE, 1f);
    }
    
    public int GetTeamScore(int teamId) => teamId == 0 ? team0Score : team1Score;
}
