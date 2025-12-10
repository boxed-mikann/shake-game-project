using UnityEngine;
using System;

/// <summary>
/// ========================================
/// GameManagerV2
/// ========================================
/// 
/// 責務：ゲーム全体のライフサイクル管理
/// - ゲーム開始・終了の状態管理
/// - 静的イベントによる全システムへの通知
/// 
/// - イベント駆動で疎結合
/// - シングルトンパターン採用
/// 
/// 状態:待機登録、待機開始待ち、ゲーム、リザルト
/// ========================================
/// </summary>
/// 
/// 大体チェック済み
/// TODO: 状態変数を追加せなあかんくなるかも

public class GameManagerV2 : MonoBehaviour
{
    public static GameManagerV2 Instance { get; private set; }

    // ゲーム状態の定義
    public enum GameState
    {
        IdleRegister,   // デバイス登録待機
        IdleStarting,   // 全員登録確認待機
        Game,           // ゲーム進行中
        Result          // リザルト表示
    }

    // 現在のゲーム状態（外部から参照可能）
    public GameState CurrentGameState { get; private set; } = GameState.IdleRegister;

    // イベント定義
    //public static UnityEvent OnShowTitle = new UnityEvent();よりも高速で安全らしい、ただし、インスペクタはやりにくい
    public event Action OnResisterStart;
    public event Action OnIdleStart;
    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    // ゲームが開始されているか
    public bool IsGameStarted => CurrentGameState == GameState.Game;


    //Unityで呼ばれる関数、Awake Start
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(this.gameObject);
        Debug.Log("[GameManagerV2] Instance set and marked as DontDestroyOnLoad.");
    }

    private void Start()
    {
        ShowResister();
        //debugログ
        Debug.Log("[GameManagerV2] GameManagerV2 started and showing Resister phase.");
    }

    private void Update()
    {
        // デバッグ用：Enterキーでフェーズ遷移
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            HandleDebugPhaseTransition();
        }
    }

    /// <summary>
    /// デバッグ用：Enterキー押下時のフェーズ遷移処理
    /// IdleRegister → IdleStarting
    /// IdleStarting → Game
    /// Result → IdleRegister
    /// </summary>
    private void HandleDebugPhaseTransition()
    {
        switch (CurrentGameState)
        {
            case GameState.IdleRegister:
                Debug.Log("[GameManagerV2] [DEBUG] Enter pressed: IdleRegister → IdleStarting");
                ShowIdle();
                break;
            
            case GameState.IdleStarting:
                Debug.Log("[GameManagerV2] [DEBUG] Enter pressed: IdleStarting → Game (via SongSelectionManager)");
                // 直接StartGame()を呼ぶと、選曲ロジック（譜面ロードなど）がスキップされるため、
                // SongSelectionManager経由で開始する
                if (SongSelectionManager.Instance != null)
                {
                    SongSelectionManager.Instance.DecideSongForce();
                }
                else
                {
                    Debug.LogWarning("[GameManagerV2] SongSelectionManager not found, starting game directly (might have no chart loaded)");
                    StartGame();
                }
                break;
            
            case GameState.Result:
                Debug.Log("[GameManagerV2] [DEBUG] Enter pressed: Result → IdleRegister");
                ShowResister();
                break;
            
            case GameState.Game:
                Debug.Log("[GameManagerV2] [DEBUG] Enter pressed during Game (no action)");
                break;
        }
    }

    //-------------------

    public void ShowResister()
    {
        if (CurrentGameState == GameState.IdleRegister)
        {
            Debug.Log("[GameManagerV2] Already in IdleRegister state, skipping event");
            return;
        }
        CurrentGameState = GameState.IdleRegister;
        OnResisterStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to IdleRegister");
        //TODO イベント購読コーディング
        // TODO バック映像の再生をスタートをイベント駆動にする
        //消去if (VideoManager.Instance != null) VideoManager.Instance.PlayLoop();
    }

    public void ShowIdle()
    {
        if (CurrentGameState == GameState.IdleStarting)
        {
            Debug.Log("[GameManagerV2] Already in IdleStarting state, skipping event");
            return;
        }
        CurrentGameState = GameState.IdleStarting;
        OnIdleStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to IdleStarting");
        //TODO イベント購読コーディング
    }

    public void StartGame()
    {
        if (CurrentGameState == GameState.Game)
        {
            Debug.Log("[GameManagerV2] Already in Game state, skipping event");
            return;
        }
        CurrentGameState = GameState.Game;
        OnGameStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to Game");
    }

    public void EndGame()
    {
        if (CurrentGameState == GameState.Result)
        {
            Debug.Log("[GameManagerV2] Already in Result state, skipping event");
            return;
        }
        CurrentGameState = GameState.Result;
        //TODO Canvasの操作はイベントを購読してUIマネがやる
        //SetCanvasState(idle: false, gameplay: false, result: true);
        OnGameEnd?.Invoke();
        Debug.Log("[GameManagerV2] State changed to Result");
    }

    // public void EndGame()
    // {
    //     ShowResult();
    // }

    // private void SetCanvasState(bool idle, bool gameplay, bool result)
    // {
    //     if (idleCanvas != null) idleCanvas.gameObject.SetActive(idle);
    //     if (gamePlayCanvas != null) gamePlayCanvas.gameObject.SetActive(gameplay);
    //     if (resultCanvas != null) resultCanvas.gameObject.SetActive(result);
    // }

    /// <summary>
    /// ゲーム開始からの経過時間を取得（音楽時刻）
    /// VideoManagerに委譲
    /// </summary>
    public double GetMusicTime()
    {
        if (!IsGameStarted) return 0.0;
        if (VideoManager.Instance != null)
        {
            return VideoManager.Instance.GetMusicTime();
        }
        Debug.LogWarning("[GameManagerV2] VideoManager not found, returning 0");
        return 0.0;
    }

    /// <summary>
    /// 強制的にリザルト画面へ遷移（デバッグ用）
    /// </summary>
    public void ForceSkipToResult()
    {
        Debug.Log("[GameManagerV2] Force Skip To Result Requested");
        EndGame();
    }
}
