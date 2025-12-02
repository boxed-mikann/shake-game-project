using UnityEngine;
using TMPro;

/// <summary>
/// シンクロ表示UI - TextMeshPro でシンクロ人数を表示
/// SyncDetectorのOnSyncDetectedイベントを購読し、シンクロ時にシンクロ人数を表示
/// Animator + Trigger方式でポップアップアニメーション
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(TMPro.TextMeshPro))]
public class SyncDisplayUI : MonoBehaviour
{
    private TMPro.TextMeshPro syncText;
    private Animator animator; // Animator Controllerに"Show"トリガーを定義
    
    [Header("Display Settings")]
    [SerializeField] private string syncMessageFormat = "シンクロ！ {0}人"; // {0} = シンクロ人数
    [SerializeField] private float displayDuration = 2.0f; // 表示時間
    
    [Header("Colors")]
    [SerializeField] private Color syncColor = Color.cyan;
    
    private float displayTimer = 0f;
    private bool isDisplaying = false;
    
    void Awake()
    {
        syncText = GetComponent<TMPro.TextMeshPro>();
        if (syncText == null)
        {
            Debug.LogError("[SyncDisplayUI] TMPro.TextMeshPro コンポーネントが見つかりません");
        }
        
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[SyncDisplayUI] Animator コンポーネントが見つかりません");
        }
        
        // 初期状態は非表示
        if (syncText != null)
        {
            syncText.text = "";
        }
    }
    
    void Start()
    {
        // SyncDetectorのイベント購読を開始
        if (SyncDetector.Instance != null)
        {
            SyncDetector.Instance.OnSyncDetected += OnSyncDetected;
            
            if (GameConstantsV2.DEBUG_MODE)
            {
                Debug.Log("[SyncDisplayUI] SyncDetectorのイベント購読を開始");
            }
        }
        else
        {
            Debug.LogError("[SyncDisplayUI] SyncDetector.Instance が見つかりません");
        }
        
        // 初期状態は非アクティブにする
        gameObject.SetActive(false);
    }
    
    void Update()
    {
        // 表示タイマー更新
        if (isDisplaying)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0f)
            {
                isDisplaying = false;
                HidePopup();
            }
        }
    }
    
    /// <summary>
    /// シンクロ検出時に呼ばれるコールバック
    /// </summary>
    private void OnSyncDetected(int syncCount)
    {
        Show(syncCount);
    }
    
    /// <summary>
    /// ポップアップを表示
    /// </summary>
    public void Show(int syncCount)
    {
        Debug.Log($"[SyncDisplayUI] Showing sync: {syncCount}人");
        
        // キャンセルして再度非表示タイマーをリセット
        CancelInvoke(nameof(HidePopup));
        
        // テキスト設定
        if (syncText != null)
        {
            syncText.text = string.Format(syncMessageFormat, syncCount);
            syncText.color = syncColor;
        }
        
        // ゲームオブジェクトをアクティブにしてから、Animatorトリガーをセット
        gameObject.SetActive(true);
        
        if (animator != null)
        {
            // Animator の状態を完全にリセット
            animator.Rebind();
            animator.Update(0f);
            
            // アニメーション再生
            animator.SetTrigger("Show");
        }
        
        // 自動非表示タイマーをセット
        isDisplaying = true;
        displayTimer = displayDuration;
        Invoke(nameof(HidePopup), displayDuration);
    }
    
    private void HidePopup()
    {
        Debug.Log("[SyncDisplayUI] Hiding popup");
        gameObject.SetActive(false);
        isDisplaying = false;
    }
}
