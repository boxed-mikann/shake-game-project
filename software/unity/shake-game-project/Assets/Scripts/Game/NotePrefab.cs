using UnityEngine;

/// <summary>
/// 音符・休符オブジェクト
/// 責務：自身の見た目管理（画像・色）、フェーズ反映
/// 
/// シェイク入力で GameManager が このオブジェクトを破壊・スコア更新する
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
        // 初期フェーズを反映
        SetPhase(PhaseController.Instance.GetCurrentPhase());
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新（Sprite と色）
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
        }
    }
    
    /// <summary>
    /// 現在のフェーズを取得
    /// </summary>
    public Phase GetCurrentPhase() => _currentPhase;
}