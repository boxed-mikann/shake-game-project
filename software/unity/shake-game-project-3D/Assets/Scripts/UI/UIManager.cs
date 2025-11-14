using UnityEngine;

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

    // ===== UI参照 =====
    private HPBarUI _hpBarUI;
    private TimerUI _timerUI;
    private TimingIndicatorUI _timingIndicatorUI;
    private PlayerStatusUI _playerStatusUI;
    private ResultScreenUI _resultScreenUI;
    
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
        // UI要素を取得
        _hpBarUI = FindObjectOfType<HPBarUI>();
        _timerUI = FindObjectOfType<TimerUI>();
        _timingIndicatorUI = FindObjectOfType<TimingIndicatorUI>();
        _playerStatusUI = FindObjectOfType<PlayerStatusUI>();
        _resultScreenUI = FindObjectOfType<ResultScreenUI>();
        
        // GameManager のイベントをリッスン
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameTick += UpdateUI;
            GameManager.Instance.OnBossDamage += OnBossDamage;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] Initialize completed");
        }
    }
    
    private void UpdateUI(float elapsedTime)
    {
        // HP BAR更新
        if (_hpBarUI != null)
        {
            _hpBarUI.UpdateHP(GameManager.Instance.BossCurrentHP, GameManager.Instance.BossMaxHP);
        }
        
        // タイマー更新
        if (_timerUI != null)
        {
            float remainingTime = GameConstants.TOTAL_GAME_TIME - elapsedTime;
            _timerUI.UpdateTimer(remainingTime);
        }
        
        // タイミングインジケーター更新
        if (_timingIndicatorUI != null)
        {
            _timingIndicatorUI.UpdateIndicator(elapsedTime);
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
        
        // 画面中央上にポップアップ生成
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[UIManager] Showing damage popup: {damageText}");
        }
    }
    
    /// <summary>
    /// UIマネージャーをリセット
    /// </summary>
    public void Reset()
    {
        if (_resultScreenUI != null)
        {
            _resultScreenUI.Hide();
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[UIManager] Reset");
        }
    }
    
    // ===== Getter =====
    public HPBarUI HPBarUI => _hpBarUI;
    public TimerUI TimerUI => _timerUI;
    public TimingIndicatorUI TimingIndicatorUI => _timingIndicatorUI;
    public ResultScreenUI ResultScreenUI => _resultScreenUI;
}

/// <summary>
/// ボスHP BAR表示
/// </summary>
public class HPBarUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image _hpFillImage;
    [SerializeField] private UnityEngine.UI.Text _hpText;
    
    public void UpdateHP(float currentHP, float maxHP)
    {
        if (_hpFillImage != null)
        {
            _hpFillImage.fillAmount = currentHP / maxHP;
        }
        
        if (_hpText != null)
        {
            _hpText.text = $"HP: {currentHP:F0} / {maxHP:F0}";
        }
    }
}

/// <summary>
/// 残り時間表示
/// </summary>
public class TimerUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text _timerText;
    
    public void UpdateTimer(float remainingTime)
    {
        remainingTime = Mathf.Max(0, remainingTime);
        int minutes = (int)(remainingTime / 60f);
        int seconds = (int)(remainingTime % 60f);
        
        if (_timerText != null)
        {
            _timerText.text = $"{minutes:D1}:{seconds:D2}";
        }
    }
}

/// <summary>
/// タイミングインジケーター表示
/// </summary>
public class TimingIndicatorUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider _beatSlider;
    [SerializeField] private UnityEngine.UI.Image _syncIndicator;
    
    public void UpdateIndicator(float gameTime)
    {
        // ビート進捗を更新
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
}

/// <summary>
/// プレイヤーステータス表示
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] private Transform _playerSlotContainer;
    [SerializeField] private GameObject _playerSlotPrefab;
    
    private void Start()
    {
        // プレイヤーが接続されたら自動的にスロットを生成
        GameManager.Instance.OnPlayerShake += OnPlayerShake;
    }
    
    private void OnPlayerShake(int childId, int shakeCount)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        playerManager.UpdateAllPlayerUI();
    }
}

/// <summary>
/// 結果画面表示
/// </summary>
public class ResultScreenUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private UnityEngine.UI.Text _resultText;
    [SerializeField] private UnityEngine.UI.Text _scoreText;
    [SerializeField] private UnityEngine.UI.Text _shakeCountText;
    [SerializeField] private UnityEngine.UI.Text _syncCountText;
    [SerializeField] private UnityEngine.UI.Button _retryButton;
    
    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        
        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Result)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    public void Show()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
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
    
    public void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
        }
    }
    
    private void OnRetryClicked()
    {
        GameManager.Instance.StartGame();
    }
}
