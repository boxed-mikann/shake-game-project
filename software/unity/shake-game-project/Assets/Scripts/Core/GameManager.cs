using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ゲーム全体を統括するマネージャー
/// 責務：フェーズ管理、ゲームモード管理、イベント処理
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private SerialDataParser serialDataParser;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private VideoManager videoManager;
    [SerializeField] private VictoryManager victoryManager;
    [SerializeField] private GamePhaseManager gamePhaseManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameMode[] gameModePrefabs;
    
    private Dictionary<string, GameMode> activeModes = new Dictionary<string, GameMode>();
    private GameMode currentGameMode;
    private Transform gameModeContainer;
    
    private static GameManager instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        gameModeContainer = new GameObject("---GameModes---").transform;
        RegisterGameModes();
        InitializeEvents();
        gamePhaseManager.SetPhase(GamePhase.Menu);
    }
    
    void Update()
    {
        if (gamePhaseManager.IsPlaying() && currentGameMode != null)
        {
            ProcessShakeData();
        }
    }
    
    private void RegisterGameModes()
    {
        foreach (GameMode modePrefab in gameModePrefabs)
        {
            GameMode modeInstance = Instantiate(modePrefab, gameModeContainer);
            modeInstance.gameObject.SetActive(false);
            
            string modeName = modeInstance.GetModeName();
            activeModes[modeName] = modeInstance;
            
            Debug.Log($"✅ Registered: {modeName}");
        }
    }
    
    public void SelectGameMode(string modeName)
    {
        if (!activeModes.ContainsKey(modeName))
        {
            Debug.LogError($"❌ Game mode not found: {modeName}");
            return;
        }
        
        if (currentGameMode != null)
        {
            currentGameMode.gameObject.SetActive(false);
            currentGameMode.ResetGame();
        }
        
        currentGameMode = activeModes[modeName];
        currentGameMode.gameObject.SetActive(true);
        
        Debug.Log($"🎮 Switched to: {modeName}");
        
        if (uiManager != null)
            uiManager.SetCurrentGameMode(modeName);
        
        gamePhaseManager.SetPhase(GamePhase.Playing);
    }
    
    private void ProcessShakeData()
    {
        var shakeDataQueue = serialDataParser.GetReceivedMessages();  // ← ここ
        
        while (shakeDataQueue.Count > 0)
        {
            ShakeDataPacket shake = shakeDataQueue.Dequeue();  // ★ 修正：Dequeue から直接取得
            
            if (shake.childID >= 0)
            {
                if (soundManager != null)
                    soundManager.PlayShakeSound();
                
                if (currentGameMode != null)
                    currentGameMode.OnShakeDetected(shake);
            }
        }
    }
    
    private ShakeDataPacket ParseMessage(string message)
    {
        ShakeDataPacket packet = new ShakeDataPacket { childID = -1 };
        
        try
        {
            string[] parts = message.Split(',');
            if (parts.Length >= 3)
            {
                packet.childID = int.Parse(parts[0]);
                packet.shakeCount = int.Parse(parts[1]);
                packet.acceleration = float.Parse(parts[2]);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Parse error: {e.Message}");
        }
        
        return packet;
    }
    
    private void InitializeEvents()
    {
        if (gamePhaseManager != null)
            gamePhaseManager.OnPhaseChanged += OnGamePhaseChanged;
        
        if (currentGameMode != null)
        {
            currentGameMode.OnPlayerWon += OnPlayerWon;
        }
    }
    
    private void OnGamePhaseChanged(GamePhase newPhase)
    {
        switch (newPhase)
        {
            case GamePhase.Menu:
                OnMenuPhase();
                break;
            case GamePhase.Playing:
                OnPlayingPhase();
                break;
            case GamePhase.Victory:
                OnVictoryPhase();
                break;
            case GamePhase.Result:
                OnResultPhase();
                break;
        }
    }
    
    private void OnMenuPhase()
    {
        Debug.Log("📋 Menu Phase");
        
        if (currentGameMode != null)
        {
            currentGameMode.gameObject.SetActive(false);
            currentGameMode.ResetGame();
        }
        
        if (victoryManager != null)
            victoryManager.HideVictoryUI();
        
        if (videoManager != null)
            videoManager.PlayGameplayVideo();
        
        if (uiManager != null)
            uiManager.ShowMenuScreen();
    }
    
    private void OnPlayingPhase()
    {
        Debug.Log("▶️ Playing Phase");
        
        if (currentGameMode == null)
        {
            Debug.LogError("❌ Game mode not selected!");
            return;
        }
        
        currentGameMode.Initialize();
        
        if (videoManager != null)
            videoManager.PlayGameplayVideo();
        
        if (victoryManager != null)
            victoryManager.HideVictoryUI();
        
        if (uiManager != null)
            uiManager.ShowGameplayScreen();
        
        // イベント登録
        currentGameMode.OnPlayerWon += OnPlayerWon;
    }
    
    private void OnVictoryPhase()
    {
        Debug.Log("🏆 Victory Phase");
        
        if (uiManager != null)
            uiManager.ShowVictoryScreen();
        
        if (videoManager != null)
            videoManager.PlayVictoryVideo();
    }
    
    private void OnResultPhase()
    {
        Debug.Log("📊 Result Phase");
        
        if (uiManager != null)
            uiManager.ShowResultScreen();
        
        StartCoroutine(ReturnToMenuAfterDelay(Constants.RESULT_DISPLAY_TIME));
    }
    
    private void OnPlayerWon(int winnerId)
    {
        Debug.Log($"🏆 Team {winnerId} won!");
        
        if (victoryManager != null)
            victoryManager.ShowVictoryUI(winnerId);
        
        gamePhaseManager.SetPhase(GamePhase.Victory);
        StartCoroutine(TransitionToResultAfterDelay(Constants.VICTORY_DISPLAY_TIME));
    }
    
    private IEnumerator TransitionToResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gamePhaseManager.SetPhase(GamePhase.Result);
    }
    
    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gamePhaseManager.SetPhase(GamePhase.Menu);
    }
    
    public List<string> GetAvailableGameModes() => new List<string>(activeModes.Keys);
    public GameMode GetCurrentGameMode() => currentGameMode;
    public static GameManager Instance => instance;
    
    void OnDestroy()
    {
        if (gamePhaseManager != null)
            gamePhaseManager.OnPhaseChanged -= OnGamePhaseChanged;
    }
}