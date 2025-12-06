using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// ========================================
/// UtaMessage
/// ========================================
/// 
/// 責務：ウタ(キャラクター)のメッセージ表示管理
/// - メッセージのテキスト表示
/// - 表示時間の制御（時間制限あり/なし）
/// - フェードイン/アウト効果（オプション）
/// 
/// 特徴：
/// - シングルトンパターン採用
/// - SuperCanvasに配置されゲーム全体で使用
/// - 他のマネージャーから呼び出される
/// - 将来的に音声再生機能を追加可能
/// 
/// ========================================
/// </summary>
public class UtaMessage : MonoBehaviour
{
    public static UtaMessage Instance { get; private set; }

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup; // フェード制御用（オプション）

    [Header("表示設定")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Coroutine currentMessageCoroutine;

    private void Awake()
    {
        // シングルトン設定
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // 初期状態は非表示
        if (messageText != null)
        {
            messageText.text = "";
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// メッセージを表示（時間制限なし、手動で非表示にする必要あり）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    public void ShowMessage(string message)
    {
        ShowMessage(message, -1f);
    }

    /// <summary>
    /// メッセージを表示（時間制限あり）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="duration">表示時間（秒）。-1で無期限</param>
    public void ShowMessage(string message, float duration)
    {
        // 既存のコルーチンを停止
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    /// <summary>
    /// メッセージを即座に非表示
    /// </summary>
    public void HideMessage()
    {
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
            currentMessageCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    /// <summary>
    /// メッセージ表示のコルーチン
    /// </summary>
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        // テキスト設定
        if (messageText != null)
        {
            messageText.text = message;
        }

        // フェードイン
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration));
        }
        else
        {
            // CanvasGroupがない場合は即座に表示
            if (messageText != null)
            {
                messageText.alpha = 1f;
            }
        }

        // 持続時間待機（-1の場合は無期限なのでスキップ）
        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);

            // フェードアウト
            if (canvasGroup != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration));
            }

            // テキストクリア
            if (messageText != null)
            {
                messageText.text = "";
            }
        }

        currentMessageCoroutine = null;
    }

    /// <summary>
    /// CanvasGroupのアルファ値をフェード
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    /// <summary>
    /// 現在メッセージが表示されているか
    /// </summary>
    public bool IsMessageShowing()
    {
        return currentMessageCoroutine != null || (canvasGroup != null && canvasGroup.alpha > 0.01f);
    }

    // 将来の拡張用メソッド（音声再生など）
    // public void ShowMessageWithVoice(string message, float duration, AudioClip voiceClip)
    // {
    //     ShowMessage(message, duration);
    //     if (voiceClip != null)
    //     {
    //         // 音声再生処理
    //     }
    // }
}
