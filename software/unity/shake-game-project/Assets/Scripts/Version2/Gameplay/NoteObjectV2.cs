using UnityEngine;

/// <summary>
/// 単ノートオブジェクト - 中心からデバイスアイコンに向かって流れる
/// スケールアニメーション付き
/// </summary>
public class NoteObjectV2 : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float scaleStart = 0.01f; // 初期スケール
    [SerializeField] private float scaleEnd = 0.15f;   // 最終スケール
    
    private Vector3 startPos;
    private Vector3 targetPos;
    private double arrivalTime; // 判定ラインに到達する時刻
    private int deviceId;
    private ChartDataV2.NoteType noteType;
    private bool isInitialized = false;
    private bool hasPassedJudgeLine = false;
    
    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    /// <summary>
    /// ノートを初期化
    /// </summary>
    /// <param name="deviceId">デバイスID</param>
    /// <param name="start">開始位置（中心）</param>
    /// <param name="target">目標位置（デバイスアイコン）</param>
    /// <param name="arrivalTime">判定ラインに到達する時刻</param>
    /// <param name="type">ノートタイプ</param>
    /// <param name="sprite">表示スプライト</param>
    public void Initialize(int deviceId, Vector3 start, Vector3 target, double arrivalTime, ChartDataV2.NoteType type, Sprite sprite = null)
    {
        this.deviceId = deviceId;
        this.startPos = start;
        this.targetPos = target;
        this.arrivalTime = arrivalTime;
        this.noteType = type;
        this.isInitialized = true;
        this.hasPassedJudgeLine = false;
        
        // 位置とスケールを設定
        transform.position = startPos;
        transform.localScale = Vector3.one * scaleStart;
        
        if (spriteRenderer != null)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            // Sorting Orderを0に設定(デバイスアイコンは10)
            spriteRenderer.sortingOrder = 0;
            
            Debug.Log($"[NoteObjectV2] 初期化: pos={transform.position}, sprite={spriteRenderer.sprite != null}, sortingOrder={spriteRenderer.sortingOrder}, sortingLayer={spriteRenderer.sortingLayerName}, enabled={spriteRenderer.enabled}, color={spriteRenderer.color}");
        }
        else
        {
            Debug.LogError("[NoteObjectV2] SpriteRendererが見つかりません！");
        }
        
        gameObject.SetActive(true);
    }
    
    void Update()
    {
        if (!isInitialized) return;
        if (GameManagerV2.Instance == null || !GameManagerV2.Instance.IsGameStarted) return;
        
        // 現在の音楽時刻を取得
        double currentMusicTime = GameManagerV2.Instance.GetMusicTime();
        double timeToArrival = arrivalTime - currentMusicTime;
        
        // 判定ラインを通過した場合
        if (timeToArrival <= 0 && !hasPassedJudgeLine)
        {
            hasPassedJudgeLine = true;
            OnMiss();
            return;
        }
        
        // ノーツが見える時間（スポーン時間）
        double visibleDuration = NoteSpawnerV2.Instance != null ? NoteSpawnerV2.Instance.GetSpawnAheadTime() : 2.0;
        
        // 進行度を計算（0.0 → 1.0）
        float progress = (float)(1.0 - (timeToArrival / visibleDuration));
        progress = Mathf.Clamp01(progress);
        
        // 位置を更新（Z座標は0で固定）
        Vector3 newPos = Vector3.Lerp(startPos, targetPos, progress);
        newPos.z = 0f;
        transform.position = newPos;
        
        // スケールアニメーション
        float scale = Mathf.Lerp(scaleStart, scaleEnd, progress);
        transform.localScale = Vector3.one * scale;
    }
    
    /// <summary>
    /// Miss処理
    /// </summary>
    private void OnMiss()
    {
        Debug.Log($"[NoteObjectV2] Miss - DeviceId={deviceId}, Time={arrivalTime}");
        // デバッグ用音を再生
        SEManager.Instance.PlayShakeHit(0);
        ReturnToPool();
    }
    
    /// <summary>
    /// ヒット処理（外部から呼ばれる）
    /// </summary>
    public void OnHit()
    {
        hasPassedJudgeLine = true;
        ReturnToPool();
    }
    
    /// <summary>
    /// プールに返却
    /// </summary>
    private void ReturnToPool()
    {
        // 状態を完全にリセット
        isInitialized = false;
        hasPassedJudgeLine = false;
        transform.localScale = Vector3.one * scaleStart;
        
        gameObject.SetActive(false);
        
        if (NotePoolV2.Instance != null)
        {
            NotePoolV2.Instance.ReturnNote(gameObject);
        }
    }
    
    // Getter
    public int GetDeviceId() => deviceId;
    public double GetArrivalTime() => arrivalTime;
    public ChartDataV2.NoteType GetNoteType() => noteType;
    public bool IsInitialized() => isInitialized;
}
