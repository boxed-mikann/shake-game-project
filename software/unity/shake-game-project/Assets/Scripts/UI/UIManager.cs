using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// ========================================
/// UIManager（フェーズインジケータスライダー統合版）
/// ========================================
/// 
/// ◎ 責務
///   1. Canvas 管理：Start/Game/Result 画面の表示切り替え
///   2. タイマー・スコア表示：毎フレーム GameManager から値を取得して表示
///   3. フェーズテキスト＆スライダー表示：GetCurrentSegment() で Phase を確認して色更新
///   4. ボタン操作：[PLAY] [BACK TO TITLE]
/// 
/// ◎ フェーズ判定の一貫性
///   - LastSprintPhase 判定：GetCurrentSegment().phase == Phase.LastSprintPhase
///   - PHASE_SEQUENCE に LastSprintPhase が明示的に含まれるので、分岐なしで判定可能
///   - GameTimer の値に依存しない（PHASE_SEQUENCE のみで管理）
/// 
/// ◎ フェーズスライダー機能
///   - スライダー値：毎フレーム GetPhaseProgress() で更新（フェーズ進度 0-1）
///   - スライダー色：フェーズ種別に応じて自動更新
///   - _isLastSprint フラグ：フェーズ変更を追跡して色を同期
/// 
/// ========================================
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
    
    // ===== フェーズスライダー（PhaseIndicatorSlider.cs から統合） =====
    /// <summary>
    /// フェーズ進度スライダー
    /// 値の範囲：0.0～1.0（毎フレーム GetPhaseProgress() で更新）
    /// 視覚的には 1.0→0.0 に減少（進度を逆転して表示）
    /// </summary>
    [SerializeField] private Slider phaseSlider;
    
    /// <summary>フェーズ色設定</summary>
    [SerializeField] private Color notePhaseColor = new Color(1f, 0.7f, 0f);      // オレンジ：音符フェーズ
    [SerializeField] private Color restPhaseColor = new Color(0.3f, 0.8f, 1f);    // シアン：休符フェーズ
    [SerializeField] private Color lastSprintColor = new Color(1f, 0.2f, 0.2f);   // 赤：ラストスパント
    
    /// <summary>スライダー fillImage の参照（フェーズ色を動的に変更用）</summary>
    private Image _fillImage;
    
    /// <summary>現在のフェーズ（OnPhaseChanged で更新）</summary>
    private Phase _currentPhase = Phase.NotePhase;
    
    /// <summary>
    /// LastSprint フラグ
    /// - false → true：GameTimer が 10s 以下になった時点で色を赤に変更
    /// - true → false：GameTimer が 10s より大きくなった時点で元の色に復帰
    /// </summary>
    private bool _isLastSprint = false;
    
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
        // ===== スライダー初期化 =====
        if (phaseSlider == null)
        {
            phaseSlider = GetComponentInChildren<Slider>();
        }
        
        if (phaseSlider != null)
        {
            phaseSlider.minValue = 0f;
            phaseSlider.maxValue = 1f;
            
            _fillImage = phaseSlider.fillRect.GetComponent<Image>();
            if (_fillImage == null)
            {
                _fillImage = phaseSlider.GetComponentInChildren<Image>();
            }
            
            Debug.Log("[UIManager] ✅ Phase slider initialized");
        }
        
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
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }
        
        // ScoreManager イベント購読
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        }
        
        // 初期状態：スタート画面を表示
        ShowStartScreen();
    }
    
    private void Update()
    {
        // ゲーム中のタイマー表示・スライダー更新
        if (GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Playing)
        {
            UpdateTimerDisplay();
            UpdatePhaseSlider();
        }
    }
    
    /// <summary>
    /// フェーズインジケータスライダーを更新（毎フレーム）
    /// 
    /// 手順：
    ///   1. GetPhaseProgress() を呼び出して、現在のフェーズ内での進度（0～1）を取得
    ///   2. phaseSlider.value = 1 - progress で値を設定（逆方向に減少）
    ///   3. GameTimer ≤ 10s で LastSprint 判定
    ///   4. LastSprint 開始時：色を赤に変更
    ///   5. LastSprint 終了時：元の色に復帰（GetCurrentSegment で現在フェーズを確認）
    /// 
    /// ⚡ パフォーマンス特性
    ///   - GetPhaseProgress()：O(n) の GetSegmentAtTime() 呼び出し（毎フレーム）
    ///   - LastSprint 判定：GameTimer 値の単純な比較（毎フレーム）
    ///   - UpdateSliderColor()：フェーズ変更またはLastSprint ON/OFF 時のみ呼び出し
    /// </summary>
    private void UpdatePhaseSlider()
    {
        if (phaseSlider == null || GameManager.Instance == null)
            return;
        
        // スライダー値を更新（フェーズ内の進度をビジュアル表示）
        float progress = GameManager.Instance.GetPhaseProgress();
        phaseSlider.value = 1f - progress;  // 逆方向にする（減っていく）
        
        // 現在のフェーズを確認して LastSprintPhase 判定
        var currentSegment = GameManager.Instance.GetCurrentSegment();
        bool isLastSprintNow = currentSegment.phase == Phase.LastSprintPhase;
        
        if (isLastSprintNow && !_isLastSprint)
        {
            _isLastSprint = true;
            UpdateSliderColor(Phase.LastSprintPhase);
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[UIManager] ⚡ LastSprint activated!");
        }
        else if (!isLastSprintNow && _isLastSprint)
        {
            _isLastSprint = false;
            UpdateSliderColor(currentSegment.phase);
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[UIManager] ⚡ LastSprint ended");
        }
    }
    
    /// <summary>
    /// スライダーの色を更新
    /// 
    /// フェーズに応じた色マッピング：
    ///   - NotePhase → オレンジ（notePhaseColor）
    ///   - RestPhase → シアン（restPhaseColor）
    ///   - LastSprintPhase → 赤（lastSprintColor）
    /// 
    /// 呼び出し元：
    ///   - OnPhaseChanged() - フェーズ変更時
    ///   - UpdatePhaseSlider() - LastSprint ON/OFF 時
    /// </summary>
    private void UpdateSliderColor(Phase phase)
    {
        Color newColor = phase switch
        {
            Phase.NotePhase => notePhaseColor,
            Phase.RestPhase => restPhaseColor,
            Phase.LastSprintPhase => lastSprintColor,
            _ => notePhaseColor
        };
        
        if (_fillImage != null)
        {
            _fillImage.color = newColor;
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
    /// フェーズ変更イベント購読メソッド
    /// GameManager.OnPhaseChanged から呼び出される（フェーズ変更時のみ）
    /// 
    /// 処理：
    ///   1. LastSprint 判定：_isLastSprint が true なら色更新をスキップ
    ///      理由：UpdatePhaseSlider() が同じフレームで色を赤に上書きするため
    ///   2. _currentPhase を更新
    ///   3. UpdateSliderColor(newPhase) でスライダー色を変更
    ///   4. phaseIndicatorText を更新（テキストは常に更新）
    /// 
    /// 購読者リスト：
    ///   - UIManager（この処理）
    ///   - NotePrefab（SetPhase() で画像を更新）
    /// 
    /// ⚠ LastSprint ケース
    ///   - GameTimer が 10s 以下になった時点で、UpdatePhaseSlider() が
    ///     色を赤に上書きしている
    ///   - その直後に OnPhaseChanged() が呼び出されても、元の色に戻さない
    ///     ために _isLastSprint チェックを入れている
    /// </summary>
    private void OnPhaseChanged(Phase newPhase, float duration)
    {
        // ラストスパント中は色更新をスキップ（UpdatePhaseSlider が管理）
        if (_isLastSprint)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[UIManager] Phase changed to {newPhase}, but LastSprint is active, ignoring color update");
            return;
        }
        
        _currentPhase = newPhase;
        UpdateSliderColor(newPhase);
        
        // フェーズテキストを常に更新
        if (phaseIndicatorText != null)
        {
            if (newPhase == Phase.NotePhase)
            {
                phaseIndicatorText.text = "♪ NOTES";
            }
            else if (newPhase == Phase.RestPhase)
            {
                phaseIndicatorText.text = "𝄽 RESTS";
            }
            else if (newPhase == Phase.LastSprintPhase)
            {
                phaseIndicatorText.text = "🔥 ラストスパート！";
            }
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[UIManager] 🔄 Phase changed to {newPhase} (duration: {duration:F1}s)");
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