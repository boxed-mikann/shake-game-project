using UnityEngine;

/// <summary>
/// 長押しノートオブジェクト - LineRendererで開始〜終了を繋ぐ
/// 連打モード対応（期間中は判定OK）
/// </summary>
public class LongNoteObjectV2 : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private SpriteRenderer headSprite;  // 開始マーカー
    [SerializeField] private SpriteRenderer tailSprite;  // 終了マーカー
    [SerializeField] private float lineWidth = 0.2f;
    
    [Header("Color Settings")]
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color completedColor = Color.gray;
    
    private Vector3 centerPos;
    private Vector3 iconPos;
    private double startTime;
    private double endTime;
    private int deviceId;
    private bool isInitialized = false;
    private bool isCompleted = false;
    
    void Awake()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
    }
    
    /// <summary>
    /// 長押しノートを初期化
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="center">中心位置</param>
    /// <param name="iconPos">デバイスアイコン位置</param>
    /// <param name="startTime">開始時刻</param>
    /// <param name="endTime">終了時刻</param>
    /// <param name="sprite">表示スプライト</param>
    public void Initialize(int deviceId, Vector3 center, Vector3 iconPos, double startTime, double endTime, Sprite sprite = null)
    {
        this.deviceId = deviceId;
        // Z座標を設定
        this.centerPos = center;
        this.centerPos.z = 0f;
        this.iconPos = iconPos;
        this.iconPos.z = 0f;
        this.startTime = startTime;
        this.endTime = endTime;
        this.isInitialized = true;
        this.isCompleted = false;
        
        // LineRenderer設定
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = activeColor;
            lineRenderer.enabled = false; // 最初は非表示
        }
        
        // スプライト設定とSorting Order
        if (headSprite != null)
        {
            if (sprite != null)
            {
                headSprite.sprite = sprite;
            }
            // Sorting Orderを0に設定(デバイスアイコンは10)
            headSprite.sortingOrder = 0;
            Debug.Log($"[LongNoteObjectV2] headSprite: sprite={headSprite.sprite != null}, sortingOrder={headSprite.sortingOrder}, enabled={headSprite.enabled}");
        }
        if (tailSprite != null)
        {
            if (sprite != null)
            {
                tailSprite.sprite = sprite;
            }
            // Sorting Orderを0に設定(デバイスアイコンは10)
            tailSprite.sortingOrder = 0;
            Debug.Log($"[LongNoteObjectV2] tailSprite: sprite={tailSprite.sprite != null}, sortingOrder={tailSprite.sortingOrder}, enabled={tailSprite.enabled}");
        }
        
        // LineRendererのSorting Orderも設定
        if (lineRenderer != null)
        {
            lineRenderer.sortingOrder = 0;
            Debug.Log($"[LongNoteObjectV2] lineRenderer: sortingOrder={lineRenderer.sortingOrder}, enabled={lineRenderer.enabled}");
        }
        
        Debug.Log($"[LongNoteObjectV2] 初期化: centerPos={centerPos}, iconPos={iconPos}, startTime={startTime}, endTime={endTime}");
        
        gameObject.SetActive(true);
    }
    
    void Update()
    {
        if (!isInitialized) return;
        if (GameManagerV2.Instance == null || !GameManagerV2.Instance.IsGameStarted) return;
        
        double currentMusicTime = GameManagerV2.Instance.GetMusicTime();
        
        // スポーン時間を取得
        double spawnAheadTime = NoteSpawnerV2.Instance != null ? NoteSpawnerV2.Instance.GetSpawnAheadTime() : 2.0;
        double spawnTime = startTime - spawnAheadTime;
        
        // 長押し開始前
        if (currentMusicTime < startTime)
        {
            // 開始マーカーのみ表示（通常ノートと同じように流れる）
            if (currentMusicTime >= spawnTime)
            {
                float progress = (float)(1.0 - ((startTime - currentMusicTime) / spawnAheadTime));
                progress = Mathf.Clamp01(progress);
                
                if (headSprite != null)
                {
                    headSprite.transform.position = Vector3.Lerp(centerPos, iconPos, progress);
                    headSprite.transform.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.15f, progress);
                    headSprite.gameObject.SetActive(true);
                }
                
                lineRenderer.enabled = false;
            }
        }
        // 長押し期間中
        else if (currentMusicTime >= startTime && currentMusicTime <= endTime)
        {
            // ラインを表示
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, centerPos);
                lineRenderer.SetPosition(1, iconPos);
                lineRenderer.enabled = true;
            }
            
            // マーカーを配置
            if (headSprite != null)
            {
                headSprite.transform.position = centerPos;
                headSprite.gameObject.SetActive(true);
            }
            if (tailSprite != null)
            {
                tailSprite.transform.position = iconPos;
                tailSprite.gameObject.SetActive(true);
            }
        }
        // 長押し終了後
        else if (currentMusicTime > endTime && !isCompleted)
        {
            OnComplete();
        }
    }
    
    /// <summary>
    /// 長押し完了処理
    /// </summary>
    private void OnComplete()
    {
        isCompleted = true;
        ReturnToPool();
    }
    
    /// <summary>
    /// 外部からヒット通知（連打モード用）
    /// </summary>
    public void OnHit()
    {
        // 色を変えるなどのフィードバック
        if (lineRenderer != null)
        {
            // エフェクトを追加する場合はここに実装
        }
    }
    
    /// <summary>
    /// プールに返却
    /// </summary>
    private void ReturnToPool()
    {
        // 状態を完全にリセット
        isInitialized = false;
        isCompleted = false;
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        if (headSprite != null)
        {
            headSprite.gameObject.SetActive(false);
            headSprite.transform.localScale = Vector3.one * 0.01f; // 初期スケールに戻す
        }
        if (tailSprite != null)
        {
            tailSprite.gameObject.SetActive(false);
            tailSprite.transform.localScale = Vector3.one * 0.15f; // 初期スケールに戻す
        }
        
        gameObject.SetActive(false);
        
        if (LongNotePoolV2.Instance != null)
        {
            LongNotePoolV2.Instance.ReturnLongNote(gameObject);
        }
    }
    
    // Getter
    public int GetDeviceId() => deviceId;
    public double GetStartTime() => startTime;
    public double GetEndTime() => endTime;
    public bool IsInPeriod(double time) => time >= startTime && time <= endTime;
    public bool IsInitialized() => isInitialized;
}
