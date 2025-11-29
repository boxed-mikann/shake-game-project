using UnityEngine;

/// <summary>
/// ノーツ - 各デバイスアイコンに向かって流下
/// 判定ラインに到達したら判定処理
/// </summary>
public class NoteV2 : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private string targetDeviceId;
    private float noteTime;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isInitialized = false;
    
    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    /// <summary>
    /// ノーツを初期化
    /// </summary>
    public void Initialize(string deviceId, float time, Vector3 start, Vector3 target, Sprite noteSprite = null)
    {
        targetDeviceId = deviceId;
        noteTime = time;
        startPosition = start;
        targetPosition = target;
        isInitialized = true;
        
        transform.position = startPosition;
        
        if (spriteRenderer != null && noteSprite != null)
        {
            spriteRenderer.sprite = noteSprite;
        }
    }
    
    void Update()
    {
        if (!isInitialized) return;
        if (!GameManagerV2.Instance.IsGameStarted) return;
        
        // 音楽時刻に基づいて移動
        float currentMusicTime = GameManagerV2.Instance.GetMusicTime();
        float timeToNote = noteTime - currentMusicTime;
        
        // 判定ラインまでの距離を時間に基づいて計算
        float visibleTime = 2f; // ノーツが見える時間（秒）
        float progress = 1f - (timeToNote / visibleTime);
        
        if (progress < 0f)
        {
            // まだ表示タイミングではない
            return;
        }
        else if (progress >= 1f)
        {
            // 判定ラインを通過
            OnNoteMiss();
            return;
        }
        
        // 位置更新
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
    }
    
    private void OnNoteMiss()
    {
        // ミス判定
        if (JudgeManager.Instance != null)
        {
            JudgeManager.Instance.OnNoteMiss(targetDeviceId);
        }
        
        ReturnToPool();
    }
    
    public void OnNoteHit(float timingDiff)
    {
        // ヒット判定
        if (JudgeManager.Instance != null)
        {
            JudgeManager.Instance.OnNoteHit(targetDeviceId, timingDiff);
        }
        
        ReturnToPool();
    }
    
    private void ReturnToPool()
    {
        isInitialized = false;
        
        if (NotePoolV2.Instance != null)
        {
            NotePoolV2.Instance.ReturnNote(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public string GetTargetDeviceId() => targetDeviceId;
    public float GetNoteTime() => noteTime;
}
