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
    [SerializeField] private Sprite noteSprite;        // 音符の画像
    [SerializeField] private Sprite restSprite;        // 休符の画像
    
    private Phase _currentPhase = Phase.NotePhase;
    private SpriteRenderer _spriteRenderer;
    
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
        
        if (_spriteRenderer == null) return;
        
        if (phase == Phase.NotePhase || phase == Phase.LastSprintPhase)
        {
            // 音符フェーズ：音符画像を表示
            if (noteSprite != null)
                _spriteRenderer.sprite = noteSprite;
        }
        else if (phase == Phase.RestPhase)
        {
            // 休符フェーズ：休符画像を表示
            if (restSprite != null)
                _spriteRenderer.sprite = restSprite;
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
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[Note] State reset");
    }
}