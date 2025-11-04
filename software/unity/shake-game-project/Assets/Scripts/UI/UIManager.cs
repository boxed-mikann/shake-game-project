using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI全体を統括管理
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject menuScreen;
    [SerializeField] private GameObject gameplayScreen;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject resultScreen;
    
    [SerializeField] private Transform gameModeButtonContainer;
    [SerializeField] private GameObject gameModeButtonPrefab;
    [SerializeField] private TextMeshProUGUI gameModeTitleText;
    
    void Start()
    {
        CreateGameModeButtons();
        ShowMenuScreen();
    }
    
    private void CreateGameModeButtons()
    {
        var modes = GameManager.Instance.GetAvailableGameModes();
        
        foreach (string modeName in modes)
        {
            GameObject buttonGO = Instantiate(gameModeButtonPrefab, gameModeButtonContainer);
            Button button = buttonGO.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            
            buttonText.text = modeName;
            button.onClick.AddListener(() => GameManager.Instance.SelectGameMode(modeName));
        }
    }
    
    public void ShowMenuScreen()
    {
        HideAllScreens();
        menuScreen.SetActive(true);
    }
    
    public void ShowGameplayScreen()
    {
        HideAllScreens();
        gameplayScreen.SetActive(true);
    }
    
    public void ShowVictoryScreen()
    {
        victoryScreen.SetActive(true);
    }
    
    public void ShowResultScreen()
    {
        HideAllScreens();
        resultScreen.SetActive(true);
    }
    
    public void SetCurrentGameMode(string modeName)
    {
        if (gameModeTitleText != null)
            gameModeTitleText.text = modeName;
    }
    
    private void HideAllScreens()
    {
        menuScreen.SetActive(false);
        gameplayScreen.SetActive(false);
        victoryScreen.SetActive(false);
        resultScreen.SetActive(false);
    }
}