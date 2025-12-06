using UnityEngine;
using System.Collections;

/// <summary>
/// ========================================
/// UtaManager
/// ========================================
/// 
/// 責務：ウタのメッセージ表示タイミングを管理
/// - GameManagerV2のイベントを購読
/// - 各ゲームフェーズに応じたメッセージを表示
/// - ゲームフェーズ中の時間管理メッセージ表示
/// 
/// 特徴：
/// - イベント駆動で疎結合
/// - 各フェーズごとの適切なメッセージを自動表示
/// - 将来的に合いの手や盛り上げメッセージを追加可能
/// 
/// ========================================
/// </summary>
public class UtaManager : MonoBehaviour
{
    [Header("メッセージ設定")]
    [Header("IdleRegister")]
    [SerializeField] private string registerMessage = "10回連続シェイクでプレイヤーを登録してね!";

    [Header("IdleStarting")]
    [SerializeField] private string startingMessage = "全員シンクロでスタート!";

    [Header("Game")]
    [SerializeField] private string gameMessage1 = "流れてくるアイコンに合わせてシェイクしてね!";
    [SerializeField] private float gameMessage1Duration = 3f;
    [SerializeField] private string gameMessage2 = "ほかの人とシンクロすると高得点だよ!";
    [SerializeField] private float gameMessage2Duration = 2f;
    [SerializeField] private float gameMessage2Delay = 3.5f; // 1つ目のメッセージ終了後の待機時間

    private void OnEnable()
    {
        // GameManagerV2のイベントを購読
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart += HandleResisterStart;
            GameManagerV2.Instance.OnIdleStart += HandleIdleStart;
            GameManagerV2.Instance.OnGameStart += HandleGameStart;
            GameManagerV2.Instance.OnGameEnd += HandleGameEnd;
        }
        else
        {
            Debug.LogWarning("[UtaManager] GameManagerV2 instance not found on enable");
        }
    }
    private void Start()
    {
        // GameManagerV2のイベントを購読（念のためStartでも購読）
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart += HandleResisterStart;
            GameManagerV2.Instance.OnIdleStart += HandleIdleStart;
            GameManagerV2.Instance.OnGameStart += HandleGameStart;
            GameManagerV2.Instance.OnGameEnd += HandleGameEnd;
        }
        else
        {
            Debug.LogWarning("[UtaManager] GameManagerV2 instance not found on start");
        }
        //OnResisterStartやOnIdleStartがゲーム開始前に発生している可能性があるため、念のため現在のフェーズに応じたメッセージ表示を行う
        switch (GameManagerV2.Instance != null ? GameManagerV2.Instance.CurrentGameState : GameManagerV2.GameState.IdleRegister)
        {
            case GameManagerV2.GameState.IdleRegister:
                HandleResisterStart();
                break;
            case GameManagerV2.GameState.IdleStarting:
                HandleIdleStart();
                break;
            case GameManagerV2.GameState.Game:
                HandleGameStart();
                break;
            case GameManagerV2.GameState.Result:
                HandleGameEnd();
                break;
            default:
                break;
        }
    }

    private void OnDisable()
    {
        // イベント購読解除
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart -= HandleResisterStart;
            GameManagerV2.Instance.OnIdleStart -= HandleIdleStart;
            GameManagerV2.Instance.OnGameStart -= HandleGameStart;
            GameManagerV2.Instance.OnGameEnd -= HandleGameEnd;
        }
    }

    /// <summary>
    /// IdleRegister開始時の処理
    /// </summary>
    private void HandleResisterStart()
    {
        if (UtaMessage.Instance != null)
        {
            // 登録フェーズが終わるまで表示（無期限）
            UtaMessage.Instance.ShowMessage(registerMessage);
            Debug.Log("[UtaManager] Showing register message");
        }
    }

    /// <summary>
    /// IdleStarting開始時の処理
    /// </summary>
    private void HandleIdleStart()
    {
        if (UtaMessage.Instance != null)
        {
            // スタート待機中は無期限で表示
            UtaMessage.Instance.ShowMessage(startingMessage);
            Debug.Log("[UtaManager] Showing starting message");
        }
    }

    /// <summary>
    /// ゲーム開始時の処理
    /// </summary>
    private void HandleGameStart()
    {
        if (UtaMessage.Instance != null)
        {
            // ゲーム説明メッセージを順番に表示
            StartCoroutine(ShowGameMessagesSequence());
            Debug.Log("[UtaManager] Starting game message sequence");
        }
    }

    /// <summary>
    /// ゲーム終了時の処理
    /// </summary>
    private void HandleGameEnd()
    {
        // ゲーム終了時はメッセージを非表示
        // （ResultUIManagerがResult用メッセージを表示する）
        if (UtaMessage.Instance != null)
        {
            UtaMessage.Instance.HideMessage();
            Debug.Log("[UtaManager] Hiding message on game end");
        }
    }

    /// <summary>
    /// ゲーム中のメッセージを順番に表示
    /// </summary>
    private IEnumerator ShowGameMessagesSequence()
    {
        // 1つ目のメッセージ
        UtaMessage.Instance.ShowMessage(gameMessage1, gameMessage1Duration);
        
        // 1つ目のメッセージが終わるまで待機
        yield return new WaitForSeconds(gameMessage1Duration);
        
        // 少し間を空ける
        yield return new WaitForSeconds(gameMessage2Delay);
        
        // 2つ目のメッセージ
        UtaMessage.Instance.ShowMessage(gameMessage2, gameMessage2Duration);
    }

    // 将来の拡張用メソッド（合いの手や盛り上げメッセージ）
    // public void ShowEncouragementMessage(string message, float duration = 2f)
    // {
    //     if (utaMessage != null)
    //     {
    //         utaMessage.ShowMessage(message, duration);
    //     }
    // }
    
    // public void ShowSyncSuccessMessage()
    // {
    //     ShowEncouragementMessage("ナイスシンクロ!", 1.5f);
    // }
}
