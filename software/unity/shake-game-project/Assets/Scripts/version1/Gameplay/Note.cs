using UnityEngine;

/// <summary>
/// ========================================
/// Note（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：音符 GameObject の表現（見た目・状態のみ）
/// - SpriteRenderer で見た目表示
/// - 現在フェーズに応じた Sprite 表示（音符 or 休符）
/// - PhaseManager.OnPhaseChanged を購読
/// 
/// 処理ロジックは含めない（Handlers が担当）
/// 
/// 参照元：Assets/Scripts/FormerCodes/Game/NotePrefab.cs
/// 
/// ========================================
/// </summary>
public class Note : MonoBehaviour
{
    [SerializeField] private Sprite noteSprite;        // 音符の画像（非推奨・SpriteManager使用推奨）
    [SerializeField] private Sprite restSprite;        // 休符の画像（非推奨・SpriteManager使用推奨）
    
    private Phase _currentPhase = Phase.NotePhase;
    private SpriteRenderer _spriteRenderer;
    
    // ★ 新規追加：音符種類ID（生成時にNoteSpawnerから設定される）
    private int _spriteID = 0;
    
    // ★ 新規追加：キャッシュされた画像参照（パフォーマンス最適化）
    private Sprite _cachedNoteSprite;   // この音符の音符画像（参照）
    private Sprite _cachedRestSprite;   // この音符の休符画像（参照）
    
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("[Note] SpriteRenderer not found!");
        }
    }
    
    private void OnEnable()
    {
        // PhaseManager.OnPhaseChanged を購読
        PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
    }
    
    private void OnDisable()
    {
        // イベント購読解除（メモリリーク防止）
        PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
    }
    
    /// <summary>
    /// フェーズ変更イベントハンドラ
    /// PhaseManager.OnPhaseChanged から呼び出される
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData phaseData)
    {
        SetPhase(phaseData.phaseType);
    }
    
    /// <summary>
    /// 音符種類IDを設定（生成時にNoteSpawnerから呼ばれる）
    /// </summary>
    public void SetSpriteID(int id)
    {
        _spriteID = id;
        
        // ★ ID設定時に画像参照をキャッシュ（1回だけSpriteManagerにアクセス）
        if (SpriteManager.Instance != null)
        {
            _cachedNoteSprite = SpriteManager.Instance.GetNoteSpriteByID(id);
            _cachedRestSprite = SpriteManager.Instance.GetRestSpriteByID(id);
        }
        else
        {
            // フォールバック：Inspector設定の画像を使用
            _cachedNoteSprite = noteSprite;
            _cachedRestSprite = restSprite;
        }
        
        // IDが設定されたら、現在のフェーズに応じた画像を表示
        UpdateSprite();
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新（Sprite）
    /// 
    /// フェーズに応じた画像を表示：
    ///   - NotePhase → noteSprite（音符画像）
    ///   - RestPhase → restSprite（休符画像）
    ///   - LastSprintPhase → noteSprite（音符画像）
    /// </summary>
    public void SetPhase(Phase phase)
    {
        _currentPhase = phase;
        UpdateSprite();
    }
    
    /// <summary>
    /// 現在のフェーズに基づいて画像を更新（キャッシュから取得・高速）
    /// </summary>
    private void UpdateSprite()
    {
        if (_spriteRenderer == null) return;
        
        // ★ キャッシュされた参照から取得（SpriteManagerへのアクセスなし・高速）
        if (_currentPhase == Phase.NotePhase || _currentPhase == Phase.LastSprintPhase)
        {
            // 音符フェーズ：キャッシュされた音符画像を表示
            if (_cachedNoteSprite != null)
            {
                _spriteRenderer.sprite = _cachedNoteSprite;
            }
            else if (noteSprite != null)
            {
                // フォールバック：Inspector設定の画像を使用
                _spriteRenderer.sprite = noteSprite;
            }
        }
        else if (_currentPhase == Phase.RestPhase)
        {
            // 休符フェーズ：キャッシュされた休符画像を表示
            if (_cachedRestSprite != null)
            {
                _spriteRenderer.sprite = _cachedRestSprite;
            }
            else if (restSprite != null)
            {
                // フォールバック：Inspector設定の画像を使用
                _spriteRenderer.sprite = restSprite;
            }
        }
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public Phase GetCurrentPhase()
    {
        return _currentPhase;
    }
    
    /// <summary>
    /// 状態をリセット（プール返却時に呼ばれる）
    /// </summary>
    public void ResetState()
    {
        // 位置・回転をリセット
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        // フェーズをリセット
        _currentPhase = Phase.NotePhase;
        
        // ★ IDとキャッシュもリセット
        _spriteID = 0;
        _cachedNoteSprite = null;
        _cachedRestSprite = null;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[Note] State reset");
    }
}