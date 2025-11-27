using UnityEngine;

/// <summary>
/// ========================================
/// SpriteManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：ゲーム全体の音符・休符画像を管理（共通スプライト・プリロード方式）
/// - 複数種類の音符/休符画像をペアで管理
/// - IDベースで音符⇔休符の対応を保持
/// - Inspector上で画像配列を設定
/// 
/// 設計原則：
/// - 共通スプライト：メモリ上に1つの実体、複数のNoteから参照
/// - プリロード：ゲーム開始時に画像をメモリ上に確保
/// - 疎結合：他のクラスはSpriteManager経由でのみ画像にアクセス
/// 
/// 対応関係：
/// - noteSprites[0] と restSprites[0] は対応する音符・休符のペア
/// - 例：ID=0なら quarter_note.png ⇔ quarter_rest.png
/// 
/// ========================================
/// </summary>
public class SpriteManager : MonoBehaviour
{
    [Header("音符画像配列（Inspectorで設定）")]
    [SerializeField] private Sprite[] noteSprites;     // 音符画像配列
    
    [Header("休符画像配列（Inspectorで設定）")]
    [SerializeField] private Sprite[] restSprites;     // 休符画像配列
    
    private static SpriteManager _instance;
    
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static SpriteManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SpriteManager>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // 配列の検証
        if (noteSprites == null || noteSprites.Length == 0)
        {
            Debug.LogWarning("[SpriteManager] Note sprites array is empty!");
        }
        
        if (restSprites == null || restSprites.Length == 0)
        {
            Debug.LogWarning("[SpriteManager] Rest sprites array is empty!");
        }
        
        if (noteSprites != null && restSprites != null && 
            noteSprites.Length != restSprites.Length)
        {
            Debug.LogWarning("[SpriteManager] Note and Rest sprite arrays have different lengths!");
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[SpriteManager] Initialized with {GetSpriteTypeCount()} sprite types");
    }
    
    /// <summary>
    /// 音符種類の総数を取得
    /// </summary>
    public int GetSpriteTypeCount()
    {
        if (noteSprites == null || restSprites == null)
            return 0;
        
        return Mathf.Min(noteSprites.Length, restSprites.Length);
    }
    
    /// <summary>
    /// ランダムな音符種類IDを取得（0 ～ GetSpriteTypeCount()-1）
    /// </summary>
    public int GetRandomSpriteID()
    {
        int count = GetSpriteTypeCount();
        return count > 0 ? Random.Range(0, count) : 0;
    }
    
    /// <summary>
    /// 指定IDの音符画像を取得
    /// </summary>
    /// <param name="id">音符種類ID（0 ～ GetSpriteTypeCount()-1）</param>
    /// <returns>音符画像（見つからない場合はnull）</returns>
    public Sprite GetNoteSpriteByID(int id)
    {
        if (noteSprites == null || id < 0 || id >= noteSprites.Length)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning($"[SpriteManager] Invalid note sprite ID: {id}");
            return null;
        }
        
        return noteSprites[id];
    }
    
    /// <summary>
    /// 指定IDの休符画像を取得
    /// </summary>
    /// <param name="id">音符種類ID（0 ～ GetSpriteTypeCount()-1）</param>
    /// <returns>休符画像（見つからない場合はnull）</returns>
    public Sprite GetRestSpriteByID(int id)
    {
        if (restSprites == null || id < 0 || id >= restSprites.Length)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning($"[SpriteManager] Invalid rest sprite ID: {id}");
            return null;
        }
        
        return restSprites[id];
    }
}
