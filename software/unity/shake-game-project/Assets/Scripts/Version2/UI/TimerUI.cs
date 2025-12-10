using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text; // 追加

/// <summary>
/// タイマーUI管理のためのスクリプト
/// GameManagerV2のStartGameイベントでタイマーを開始し、終わったら、GameManagerV2のEndGame関数を呼び出す。
/// UIを頻繁に更新することになるため、処理が重くならないように工夫する。
/// タイマーのTMPテキストオブジェクトと、時間制限の設定はインスペクタで行う。
/// </summary>
public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 120f;

    private float elapsedTime = 0f;
    private bool isTimerRunning = false;
    private int lastDisplayedSecond = -1;

    private int lastDisplayedRemainingSecond = -1;
    private StringBuilder sb = new StringBuilder(4);


    private void Start()
    {
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= StartTimer;
            GameManagerV2.Instance.OnGameEnd -= StopTimer;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnGameStart -= StartTimer;
            GameManagerV2.Instance.OnGameStart += StartTimer;
            GameManagerV2.Instance.OnGameEnd -= StopTimer;
            GameManagerV2.Instance.OnGameEnd += StopTimer;
            Debug.Log("[TimerUI] Subscribed to GameManagerV2 game phase events.");
        }
        else
        {
            Debug.Log("[TimerUI] not Subscribed to GameManagerV2 game phase events.");
        }
    }

    private void StartTimer()
    {
        elapsedTime = 0f;
        isTimerRunning = true;
        lastDisplayedRemainingSecond = -1;
        sb.Clear();
        UpdateTimerDisplay( Mathf.CeilToInt(timeLimit) );
    }

    private void StopTimer()
    {
        isTimerRunning = false;
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        // VideoManager.GetMusicTime()を使用して正確な音楽時間を取得
        if (VideoManager.Instance != null)
        {
            elapsedTime = (float)VideoManager.Instance.GetMusicTime();
        }
        else
        {
            elapsedTime += Time.deltaTime;
        }

        float remainingTime = timeLimit - elapsedTime;

        if (remainingTime <= 0f)
        {
            elapsedTime = timeLimit;
            isTimerRunning = false;
            lastDisplayedRemainingSecond = -1;
            UpdateTimerDisplay(0);
            GameManagerV2.Instance?.EndGame();
            return;
        }

        int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));

        if (remainingSeconds != lastDisplayedRemainingSecond)
        {
            UpdateTimerDisplay(remainingSeconds);
            lastDisplayedRemainingSecond = remainingSeconds;
        }
    }

    private void UpdateTimerDisplay(int remainingSeconds)
    {
        if (timerText == null) return;

        sb.Clear();
        sb.Append(remainingSeconds.ToString("D2"));
        sb.Append('s');
        timerText.text = sb.ToString();
    }
}
