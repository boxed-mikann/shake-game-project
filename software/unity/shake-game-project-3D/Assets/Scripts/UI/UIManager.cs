using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI全体を管理するマネージャー
/// </summary>
public class UIManager : MonoBehaviour
{
    // ===== Singleton =====
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UIManager");
                    _instance = go.AddComponent<UIManager>();
                }
            }
            return _instance;
        }
    }

    // ===== UI References (Inspector) =====
    [SerializeField] private CanvasGroup _titleScreenCanvasGroup;
    [SerializeField] private GameObject _gamePlayScreenCanvasGroup;
    [SerializeField] private Button _startButton;
    [SerializeField] private Slider _hpBarSlider;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private Slider _beatSlider;
    [SerializeField] private Image _syncIndicator;
    [SerializeField] private CanvasGroup _resultScreenCanvasGroup;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _shakeCountText;
    [SerializeField] private TextMeshProUGUI _syncCountText;
    [SerializeField] private Button _retryButton;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // GameManager のイベントをリッスン
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameTick += UpdateUI;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnBossDamage += OnBossDamage;
        }
        
        // スタートボタンの登録
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        // リトライボタンの登録
        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }
        //ゲームプレイ画面を非表示
        HideGamePlayScreen();
        // 結果画面を非表示
        HideResultScreen();
        
        // タイトル画面を表示
        ShowTitleScreen();
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] Initialize completed");
        }
    }
    
    private void UpdateUI(float elapsedTime)
    {
        // HP BAR更新（Sliderベース）
        if (_hpBarSlider != null)
        {
            float fillAmount = GameManager.Instance.BossCurrentHP / GameManager.Instance.BossMaxHP;
            _hpBarSlider.value = fillAmount;
        }
        
        // タイマー更新
        if (_timerText != null)
        {
            float remainingTime = Mathf.Max(0, GameConstants.TOTAL_GAME_TIME - elapsedTime);
            int minutes = (int)(remainingTime / 60f);
            int seconds = (int)(remainingTime % 60f);
            _timerText.text = $"{minutes:D1}:{seconds:D2}";
        }
        
        // タイミングインジケーター更新
        if (_beatSlider != null)
        {
            TimingSystem timingSystem = TimingSystem.Instance;
            if (timingSystem != null)
            {
                _beatSlider.value = timingSystem.BeatProgress;
            }
        }
        
        // 同期期の場合、インジケーターを表示
        if (_syncIndicator != null)
        {
            GamePhase currentPhase = GameManager.Instance.CurrentPhase;
            _syncIndicator.enabled = (currentPhase == GamePhase.BossSyncRequired);
        }
    }
    
    /// <summary>
    /// ボスダメージ時のコールバック
    /// </summary>
    private void OnBossDamage(float damage, DamageType damageType)
    {
        ShowDamagePopup(damage, damageType);
    }
    
    /// <summary>
    /// ゲーム状態が変更された
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Title)
        {
            ShowTitleScreen();
            HideResultScreen();
            HideGamePlayScreen();
        }
        else if (newState == GameState.Running)
        {
            HideTitleScreen();
            HideResultScreen();
            ShowGamePlayScreen();
        }
        else if (newState == GameState.Result)
        {
            HideTitleScreen();
            ShowResultScreen();
        }
    }
    
    /// <summary>
    /// ダメージポップアップ表示
    /// </summary>
    public void ShowDamagePopup(float damage, DamageType damageType)
    {
        string damageText = damageType switch
        {
            DamageType.Normal => $"-{damage:F0}",
            DamageType.Sync => $"-{damage:F0} SYNC!",
            DamageType.Boss => $"-{damage:F0} DAMAGE!",
            _ => $"-{damage:F0}"
        };
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[UIManager] Damage popup: {damageText}");
        }
    }
    
    /// <summary>
    /// 結果画面を表示
    /// </summary>
    private void ShowResultScreen()
    {
        if (_resultScreenCanvasGroup != null)
        {
            _resultScreenCanvasGroup.alpha = 1f;
            _resultScreenCanvasGroup.interactable = true;
            _resultScreenCanvasGroup.blocksRaycasts = true;
        }
        
        // 結果情報を表示
        GameManager gm = GameManager.Instance;
        
        if (_resultText != null)
        {
            bool isVictory = gm.CurrentState == GameState.Victory;
            _resultText.text = isVictory ? "勝利！" : "敗北...";
        }
        
        if (_scoreText != null)
        {
            _scoreText.text = $"スコア: {gm.TotalScore}";
        }
        
        if (_shakeCountText != null)
        {
            _shakeCountText.text = $"シェイク回数: {gm.TotalShakeCount}";
        }
        
        if (_syncCountText != null)
        {
            _syncCountText.text = $"同期成功: {gm.TotalSyncSuccessCount}";
        }
    }
    
    /// <summary>
    /// 結果画面を非表示
    /// </summary>
    private void HideResultScreen()
    {
        if (_resultScreenCanvasGroup != null)
        {
            _resultScreenCanvasGroup.alpha = 0f;
            _resultScreenCanvasGroup.interactable = false;
            _resultScreenCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 結果画面を表示
    /// </summary>
    private void ShowGamePlayScreen()
    {
        if (_gamePlayScreenCanvasGroup != null)
        {
            _gamePlayScreenCanvasGroup.SetActive(true);
            //_gamePlayScreenCanvasGroup.alpha = 1f;
            //_gamePlayScreenCanvasGroup.interactable = true;
            //_gamePlayScreenCanvasGroup.blocksRaycasts = true;
        }
    }
    
    /// <summary>
    /// ゲームプレイ画面を非表示
    /// </summary>
    private void HideGamePlayScreen()
    {
        if (_gamePlayScreenCanvasGroup != null)
        {
            _gamePlayScreenCanvasGroup.SetActive(false);
            //_gamePlayScreenCanvasGroup.alpha = 0f;
            //_gamePlayScreenCanvasGroup.interactable = false;
            //_gamePlayScreenCanvasGroup.blocksRaycasts = false;
        }
    }
    
    private void OnRetryClicked()
    {
        GameManager.Instance.StartGame();
    }
    
    /// <summary>
    /// UIマネージャーをリセット
    /// </summary>
    public void Reset()
    {
        HideResultScreen();
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] Reset");
        }
    }
    
    /// <summary>
    /// タイトル画面を表示
    /// </summary>
    private void ShowTitleScreen()
    {
        if (_titleScreenCanvasGroup != null)
        {
            _titleScreenCanvasGroup.alpha = 1f;
            _titleScreenCanvasGroup.interactable = true;
            _titleScreenCanvasGroup.blocksRaycasts = true;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] ShowTitleScreen");
        }
    }
    
    /// <summary>
    /// タイトル画面を非表示
    /// </summary>
    private void HideTitleScreen()
    {
        if (_titleScreenCanvasGroup != null)
        {
            _titleScreenCanvasGroup.alpha = 0f;
            _titleScreenCanvasGroup.interactable = false;
            _titleScreenCanvasGroup.blocksRaycasts = false;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] HideTitleScreen");
        }
    }
    
    /// <summary>
    /// スタートボタンが押された
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] StartButton clicked");
        }
        GameManager.Instance.StartGame();
    }
}

