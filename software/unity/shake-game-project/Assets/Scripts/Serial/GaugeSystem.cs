using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GaugeSystem : MonoBehaviour
{
    [SerializeField] private SerialDataParser dataParser;
    [SerializeField] private Image team0Gauge;  // チーム0のゲージUI
    [SerializeField] private Image team1Gauge;  // チーム1のゲージUI
    
    private int team0Score = 0;
    private int team1Score = 0;
    private float maxScore = 100f;
    
    void Update()
    {
        ProcessIncomingShakes();
        UpdateGaugeUI();
    }
    
    private void ProcessIncomingShakes()
    {
        var shakeDataQueue = dataParser.GetParsedDataQueue();
        
        while (shakeDataQueue.Count > 0)
        {
            ShakeDataPacket shake = shakeDataQueue.Dequeue();
            
            // childID に応じてスコアを加算
            if (shake.childID == 0)
            {
                team0Score += (int)(shake.acceleration/10000);
                Debug.Log($"🎮 Team 0: +{(int)(shake.acceleration/10000)} (Total: {team0Score})");
            }
            else if (shake.childID == 1)
            {
                team1Score += (int)(shake.acceleration/10000);
                Debug.Log($"🎮 Team 1: +{(int)(shake.acceleration/10000)} (Total: {team1Score})");
            }
        }
    }
    
    private void UpdateGaugeUI()
    {
        // 正規化されたスコア（0-1）
        float team0Fill = Mathf.Min(team0Score / maxScore, 1f);
        float team1Fill = Mathf.Min(team1Score / maxScore, 1f);
        
        team0Gauge.fillAmount = team0Fill;
        team1Gauge.fillAmount = team1Fill;
    }
    
    public void ResetScores()
    {
        team0Score = 0;
        team1Score = 0;
    }
}