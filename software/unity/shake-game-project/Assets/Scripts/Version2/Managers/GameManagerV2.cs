using UnityEngine;
using System;

public class GameManagerV2 : MonoBehaviour
{
    public static GameManagerV2 Instance { get; private set; }

    public event Action OnIdleStart;
    public event Action OnGameStart;
    public event Action OnGameEnd;

    [Header("Canvas References")]
    [SerializeField] private Canvas idleCanvas;
    [SerializeField] private Canvas gamePlayCanvas;
    [SerializeField] private Canvas resultCanvas;

    private double gameStartDspTime;
    public bool IsGameStarted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        ShowIdle();
    }

    public void ShowIdle()
    {
        IsGameStarted = false;
        SetCanvasState(idle: true, gameplay: false, result: false);
        OnIdleStart?.Invoke();
        if (VideoManager.Instance != null) VideoManager.Instance.PlayLoop();
    }

    public void StartGame()
    {
        gameStartDspTime = AudioSettings.dspTime;
        IsGameStarted = true;
        SetCanvasState(idle: false, gameplay: true, result: false);
        if (VideoManager.Instance != null) VideoManager.Instance.PlayFromStart();
        OnGameStart?.Invoke();
    }

    public void ShowResult()
    {
        IsGameStarted = false;
        SetCanvasState(idle: false, gameplay: false, result: true);
        OnGameEnd?.Invoke();
    }

    public void EndGame()
    {
        ShowResult();
    }

    private void SetCanvasState(bool idle, bool gameplay, bool result)
    {
        if (idleCanvas != null) idleCanvas.gameObject.SetActive(idle);
        if (gamePlayCanvas != null) gamePlayCanvas.gameObject.SetActive(gameplay);
        if (resultCanvas != null) resultCanvas.gameObject.SetActive(result);
    }

    public float GetMusicTime()
    {
        if (!IsGameStarted) return 0f;
        return (float)(AudioSettings.dspTime - gameStartDspTime);
    }

    public float GetRelativeTime(double absoluteDspTime)
    {
        return (float)(absoluteDspTime - gameStartDspTime);
    }

    public double GetGameStartDspTime() => gameStartDspTime;
}
