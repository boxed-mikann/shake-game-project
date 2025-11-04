using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 勝利画面管理
/// 映像表示 + 勝者デバイスへのフィードバック送信
/// </summary>
public class VictoryManager : MonoBehaviour
{
    [SerializeField] private RawImage gameplayRawImage;
    [SerializeField] private RawImage victoryRawImage;
    [SerializeField] private TextMeshProUGUI victoryMessageText;
    
    void Start()
    {
        HideVictoryUI();
    }
    
    public void ShowVictoryUI(int winnerTeam)
    {
        // 映像切り替え
        if (gameplayRawImage != null)
            gameplayRawImage.enabled = false;
        
        if (victoryRawImage != null)
            victoryRawImage.enabled = true;
        
        // メッセージ表示
        if (victoryMessageText != null)
        {
            victoryMessageText.text = $"🏆 Team {winnerTeam} の勝利！";
            victoryMessageText.gameObject.SetActive(true);
        }
        
        // ★ 勝者デバイスにフィードバック送信（ライト点灯など）
        SendVictoryFeedback(winnerTeam);
        
        Debug.Log("🏆 Victory UI shown");
    }
    
    /// <summary>
    /// ★ 勝者に対してESP32へフィードバック送信
    /// </summary>
    private void SendVictoryFeedback(int winnerTeam)
    {
        if (CommandSender.Instance == null)
            return;
        
        // 勝者のデバイスにライト点滅を指示
        CommandSender.Instance.SendLightBlink(winnerTeam, 5);
        CommandSender.Instance.SendVibrationOn(winnerTeam);
    }
    
    public void HideVictoryUI()
    {
        if (gameplayRawImage != null)
            gameplayRawImage.enabled = true;
        
        if (victoryRawImage != null)
            victoryRawImage.enabled = false;
        
        if (victoryMessageText != null)
        {
            victoryMessageText.gameObject.SetActive(false);
            victoryMessageText.text = "";
        }
        
        // ★ 全デバイスのライトと振動をオフ
        if (CommandSender.Instance != null)
        {
            CommandSender.Instance.SendLightOff(0);
            CommandSender.Instance.SendLightOff(1);
            CommandSender.Instance.SendVibrationOff(0);
            CommandSender.Instance.SendVibrationOff(1);
        }
        
        Debug.Log("🏆 Victory UI hidden");
    }
}
