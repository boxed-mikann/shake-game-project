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
    
    private void OnEnable()
    {
        // ゲームフェーズイベントを購読
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnResisterStart += OnResisterStart;
            GameManagerV2.Instance.OnIdleStart += OnIdleStart;
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
            //デバッグログ
            Debug.Log("[PhaseCanvasUIManager] Subscribed to game phase events.");
        }
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

    private void Awake()
    {
        // 初期状態として全キャンバスを非アクティブにする
        if (resisterCanvas != null) resisterCanvas.gameObject.SetActive(false);
        if (idleCanvas != null) idleCanvas.gameObject.SetActive(false);
        if (GameCanvas != null) GameCanvas.gameObject.SetActive(false);
        if (ResultCanvas != null) ResultCanvas.gameObject.SetActive(false);
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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
