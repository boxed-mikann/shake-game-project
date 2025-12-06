using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseCanvasUIManager : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private Canvas resisterCanvas;
    [SerializeField] private Canvas idleCanvas;
    [SerializeField] private Canvas GameCanvas;
    [SerializeField] private Canvas ResultCanvas;
    
    private void Awake()
    {
        // 初期状態では、GameManagerV2のフェーズに応じてキャンバスを非アクティブにする
        if (GameManagerV2.Instance != null)
        {
            if (resisterCanvas != null) resisterCanvas.gameObject.SetActive(GameManagerV2.Instance.CurrentGameState == GameManagerV2.GameState.IdleRegister);
            if (idleCanvas != null) idleCanvas.gameObject.SetActive(GameManagerV2.Instance.CurrentGameState == GameManagerV2.GameState.IdleStarting);
            if (GameCanvas != null) GameCanvas.gameObject.SetActive(GameManagerV2.Instance.CurrentGameState == GameManagerV2.GameState.Game);
            if (ResultCanvas != null) ResultCanvas.gameObject.SetActive(GameManagerV2.Instance.CurrentGameState == GameManagerV2.GameState.Result);
        }
        SubscribeToEvents();
    }
    
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
        // unsubscribe to avoid memory leaks
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
            GameManagerV2.Instance.OnResisterStart += OnResisterStart;
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
            GameManagerV2.Instance.OnIdleStart += OnIdleStart;
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
            Debug.Log("[PhaseCanvasUIManager] Subscribed to game phase events.");
        }
    }
    private void OnResisterStart()
    {
        // activate the correct canvas based on the phase
        resisterCanvas.gameObject.SetActive(true);
        idleCanvas.gameObject.SetActive(false);
        GameCanvas.gameObject.SetActive(false);
        ResultCanvas.gameObject.SetActive(false);
    }
    private void OnIdleStart()
    {
        // activate the correct canvas based on the phase
        resisterCanvas.gameObject.SetActive(false);
        idleCanvas.gameObject.SetActive(true);
        GameCanvas.gameObject.SetActive(false);
        ResultCanvas.gameObject.SetActive(false);
    }
    private void OnGameStart()
    {
        // activate the correct canvas based on the phase
        resisterCanvas.gameObject.SetActive(false);
        idleCanvas.gameObject.SetActive(false);
        GameCanvas.gameObject.SetActive(true);
        ResultCanvas.gameObject.SetActive(false);
    }
    private void OnGameEnd()
    {
        // activate the correct canvas based on the phase
        resisterCanvas.gameObject.SetActive(false);
        idleCanvas.gameObject.SetActive(false);
        GameCanvas.gameObject.SetActive(false);
        ResultCanvas.gameObject.SetActive(true);
    }
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        
    }
}
