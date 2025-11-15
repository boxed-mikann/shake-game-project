using UnityEngine;
using TMPro;

/// <summary>
/// UI管理 - Canvas 表示・非表示管理、画面遷移
/// 責務：3Canvas（Start/Game/Result）の管理、タイマー・スコア表示更新
/// </summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvasStart;
    [SerializeField] private Canvas canvasGame;
    [SerializeField] private Canvas canvasResult;
    
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI phaseIndicatorText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button titleButton;
    
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        Debug.Log("[UIManager] ✅ UIManager Singleton initialized");
    }
    
    private void Start()
    {
        // ボタンイベント登録
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
            Debug.Log("[UIManager] ✅ Play button listener registered");
        }
        else
        {
            Debug.LogWarning("[UIManager] ⚠️ Play button is NULL - not assigned in Inspector!");
        }
        
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(OnTitleButtonClicked);
            Debug.Log("[UIManager] ✅ Title button listener registered");
        }
        else
        {
            Debug.LogWarning("[UIManager] ⚠️ Title button is NULL - not assigned in Inspector!");
        }
        
        // GameManager イベント購読
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        
        // ScoreManager イベント購読
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        }
        
        // PhaseController イベント購読
        if (PhaseController.Instance != null)
        {
            PhaseController.Instance.OnPhaseChanged += OnPhaseChanged;
        }
        
        // 初期状態：スタート画面を表示
        ShowStartScreen();
    }
    
    private void Update()
    {
        // ゲーム中のタイマー表示更新
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Playing)
        {
            UpdateTimerDisplay();
        }
    }
    
    /// <summary>
    /// スタート画面を表示
    /// </summary>
    public void ShowStartScreen()
    {
        ActivateCanvasOnly(canvasStart);
        Debug.Log("[UIManager] 📺 Start screen shown");
    }
    
    /// <summary>
    /// ゲーム画面を表示
    /// </summary>
    public void ShowGameScreen()
    {
        ActivateCanvasOnly(canvasGame);
        Debug.Log("[UIManager] 🎮 Game screen shown");
    }
    
    /// <summary>
    /// リザルト画面を表示
    /// </summary>
    public void ShowResultScreen()
    {
        ActivateCanvasOnly(canvasResult);
        
        if (ScoreManager.Instance != null)
        {
            int finalScore = ScoreManager.Instance.GetFinalScore();
            if (finalScoreText != null)
                finalScoreText.text = finalScore.ToString();
        }
        
        Debug.Log("[UIManager] 📊 Result screen shown");
    }
    
    /// <summary>
    /// 指定したCanvas だけを Active にする
    /// </summary>
    private void ActivateCanvasOnly(Canvas target)
    {
        if (canvasStart != null) canvasStart.gameObject.SetActive(target == canvasStart);
        if (canvasGame != null) canvasGame.gameObject.SetActive(target == canvasGame);
        if (canvasResult != null) canvasResult.gameObject.SetActive(target == canvasResult);
    }
    
    /// <summary>
    /// タイマー表示更新
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerText != null && GameManager.Instance != null)
        {
            float remainingTime = Mathf.Max(0f, GameManager.Instance.GameTimer);
            timerText.text = remainingTime.ToString("F1");
        }
    }
    
    /// <summary>
    /// スコア表示更新
    /// </summary>
    private void OnScoreChanged(int newScore)
    {
        if (scoreText != null)
            scoreText.text = newScore.ToString();
    }
    
    /// <summary>
    /// フェーズ表示更新
    /// </summary>
    private void OnPhaseChanged(Phase newPhase)
    {
        if (phaseIndicatorText != null)
        {
            phaseIndicatorText.text = (newPhase == Phase.NotePhase) ? "♪ NOTES" : "𝄽 RESTS";
        }
    }
    
    /// <summary>
    /// GameState変更時の処理
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Start:
                ShowStartScreen();
                break;
            case GameState.Playing:
                ShowGameScreen();
                break;
            case GameState.Result:
                ShowResultScreen();
                break;
        }
    }
    
    /// <summary>
    /// Play ボタンが押された
    /// </summary>
    private void OnPlayButtonClicked()
    {
        Debug.Log("[UIManager] ▶️ Play button clicked");
        if (GameManager.Instance != null)
        {
            Debug.Log("[UIManager] ✅ GameManager.Instance found, calling StartGame()");
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("[UIManager] ❌ ERROR: GameManager.Instance is NULL!");
        }
    }
    
    /// <summary>
    /// Title ボタンが押された
    /// </summary>
    private void OnTitleButtonClicked()
    {
        Debug.Log("[UIManager] 🏠 Title button clicked");
        ShowStartScreen();
    }
}