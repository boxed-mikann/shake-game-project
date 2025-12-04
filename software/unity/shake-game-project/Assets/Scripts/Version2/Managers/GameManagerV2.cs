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

    //-------------------

    public void ShowResister()
    {
        CurrentGameState = GameState.IdleRegister;
        OnResisterStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to IdleRegister");
        //TODO イベント購読コーディング
        // TODO バック映像の再生をスタートをイベント駆動にする
        //消去if (VideoManager.Instance != null) VideoManager.Instance.PlayLoop();
    }

    public void ShowIdle()
    {
        CurrentGameState = GameState.IdleStarting;
        OnIdleStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to IdleStarting");
        //TODO イベント購読コーディング
    }

    public void StartGame()
    {
        CurrentGameState = GameState.Game;
        //TODO ビデオスタートもイベント駆動(でもって譜面が参照するゲームスタートタイミングはVideoを開始するところが記録して、他からアクセスできるようにする)
        //if (VideoManager.Instance != null) VideoManager.Instance.PlayFromStart();
        OnGameStart?.Invoke();
        Debug.Log("[GameManagerV2] State changed to Game");
    }

    public void EndGame()
    {
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

//TODO どこかに移譲する
    // public float GetMusicTime()
    // {
    //     if (!IsGameStarted) return 0f;
    //     return (float)(AudioSettings.dspTime - gameStartDspTime);
    // }

    // public float GetRelativeTime(double absoluteDspTime)
    // {
    //     return (float)(absoluteDspTime - gameStartDspTime);
    // }

    // public double GetGameStartDspTime() => gameStartDspTime;
}
