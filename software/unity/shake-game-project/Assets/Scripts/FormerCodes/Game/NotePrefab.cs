using UnityEngine;

/// <summary>
/// ========================================
/// NotePrefab（音符・休符オブジェクト）
/// ========================================
/// 
/// ◎ 責務
///   - 自身の見た目管理：Sprite と色の更新
///   - フェーズ反映：OnPhaseChanged イベントで画像を自動更新
/// 
/// ◎ ライフサイクル
///   [Awake] → [Start] → [OnPhaseChanged] → [OnDestroy]
/// 
/// ◎ イベント駆動設計
///   - GameManager.OnPhaseChanged を購読
///   - フェーズ変更時に自動的に SetPhase() を呼び出して Sprite 更新
///   - 購読解除：OnDestroy() で必ず実施（メモリリーク防止）
/// 
/// ◎ 入力処理は GameManager が担当
///   - NotePrefab.cs は見た目のみ管理
///   - シェイク入力で GameManager が FindObjectsOfType<Note>() して破壊・スコア更新
/// 
/// ========================================
/// </summary>
public class NotePrefab : MonoBehaviour
{
    [SerializeField] private Sprite noteSprite;        // 音符の画像
    [SerializeField] private Sprite restSprite;        // 休符の画像
    
    private Phase _currentPhase = Phase.NotePhase;
    private SpriteRenderer _spriteRenderer;
    
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Start()
    {
        // フェーズ変更イベントを購読（GameManager から）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            
            // 初期フェーズを反映（最初のイベント発火前）
            var segment = GameManager.Instance.GetCurrentSegment();
            SetPhase(segment.phase);
        }
    }
    
    /// <summary>
    /// フェーズ変更イベントハンドラ
    /// 
    /// GameManager.OnPhaseChanged から呼び出される（フェーズ変更時のみ）
    /// 
    /// 用途：
    ///   - duration パラメータは未使用（音符では不要）
    ///   - SetPhase() で Sprite を更新
    /// </summary>
    private void OnPhaseChanged(Phase newPhase, float duration)
    {
        SetPhase(newPhase);
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新（Sprite）
    /// 
    /// フェーズに応じた画像を表示：
    ///   - NotePhase → noteSprite（音符画像）
    ///   - RestPhase → restSprite（休符画像）
    ///   - LastSprintPhase → noteSprite（最後の 10s は音符）
    /// </summary>
    public void SetPhase(Phase phase)
    {
        _currentPhase = phase;
        
        if (_spriteRenderer != null)
        {
            if (phase == Phase.NotePhase)
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
            // LastSprintPhase でも何もしない（見た目は同じ）
        }
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public Phase GetCurrentPhase() => _currentPhase;
    
    /// <summary>
    /// オブジェクト破棄時、イベントをアンサブスクライブ
    /// 
    /// ⚠ 重要：必ず実施（メモリリーク防止）
    /// 
    /// 理由：
    ///   - Start() で GameManager.OnPhaseChanged += OnPhaseChanged を登録した
    ///   - オブジェクト破棄時に -= 登録解除しないと、GameManager が破棄されない限り
    ///     このオブジェクトへの参照が残り続け、メモリがリークする
    /// 
    /// 実装：
    ///   1. GameManager.Instance が存在を確認
    ///   2. OnPhaseChanged イベントから this.OnPhaseChanged() を削除
    /// </summary>
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }
}