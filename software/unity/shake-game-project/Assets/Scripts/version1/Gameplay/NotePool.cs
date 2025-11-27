using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ========================================
/// NotePool（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：音符オブジェクトのプール管理（生成・再利用）
/// - Note.prefab から事前に複数生成
/// - SetActive() で有効化・無効化管理
/// - Queue<Note> でキューイング（返却順）
/// 
/// Object Pool パターンによる最適化：
/// - Instantiate/Destroy のコスト削減
/// - GC 負荷軽減
/// 
/// ========================================
/// </summary>
public class NotePool : MonoBehaviour
{
    [SerializeField] private GameObject notePrefab;            // Note Prefab
    [SerializeField] private Transform poolContainer;          // プールのコンテナ（親オブジェクト）
    [SerializeField] private int initialPoolSize = 100;        // 初期プールサイズ
    [SerializeField] private int expandSize = 20;              // 不足時の拡張サイズ
    
    private Queue<Note> _availableNotes = new Queue<Note>();
    private List<Note> _allNotes = new List<Note>();
    
    // シングルトンインスタンス
    private static NotePool _instance;
    public static NotePool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NotePool>();
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
        
        // プール初期化
        InitializePool();
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NotePool] Initialized with {initialPoolSize} notes");
    }
    
    /// <summary>
    /// プール初期化
    /// </summary>
    private void InitializePool()
    {
        // notePrefab の確認
        if (notePrefab == null)
        {
            // Resources から読み込み
            notePrefab = Resources.Load<GameObject>("Prefabs/Note");
            
            if (notePrefab == null)
            {
                Debug.LogError("[NotePool] Note prefab not found! Please assign or place in Resources/Prefabs/Note");
                return;
            }
        }
        
        // poolContainer の確認
        if (poolContainer == null)
        {
            GameObject container = new GameObject("NotePoolContainer");
            poolContainer = container.transform;
            poolContainer.SetParent(transform);
        }
        
        // 初期プール生成
        ExpandPool(initialPoolSize);
    }
    
    /// <summary>
    /// プール拡張
    /// </summary>
    private void ExpandPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject noteGO = Instantiate(notePrefab, poolContainer);
            Note note = noteGO.GetComponent<Note>();
            
            if (note == null)
            {
                Debug.LogWarning("[NotePool] Note component not found on prefab!");
                Destroy(noteGO);
                continue;
            }
            
            // 初期状態は非アクティブ
            noteGO.SetActive(false);
            
            // プールに追加
            _availableNotes.Enqueue(note);
            _allNotes.Add(note);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NotePool] Expanded pool by {count} notes (Total: {_allNotes.Count})");
    }
    
    /// <summary>
    /// プールから Note 取得（ない場合は自動拡張）
    /// </summary>
    public Note GetNote()
    {
        // プールが空の場合は拡張
        if (_availableNotes.Count == 0)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[NotePool] Pool exhausted, expanding...");
            
            ExpandPool(expandSize);
        }
        
        // プールから取得
        Note note = _availableNotes.Dequeue();
        
        // 状態リセット
        note.ResetState();
        
        // アクティブ化
        note.gameObject.SetActive(true);
        
        return note;
    }
    
    /// <summary>
    /// プールに Note 返却
    /// </summary>
    public void ReturnNote(Note note)
    {
        if (note == null)
        {
            Debug.LogWarning("[NotePool] Tried to return null note!");
            return;
        }
        
        // 非アクティブ化
        note.gameObject.SetActive(false);
        
        // 状態リセット
        note.ResetState();
        
        // プールに返却
        _availableNotes.Enqueue(note);
    }
    
    /// <summary>
    /// プール情報取得（デバッグ用）
    /// </summary>
    public (int total, int available, int active) GetPoolInfo()
    {
        int total = _allNotes.Count;
        int available = _availableNotes.Count;
        int active = total - available;
        
        return (total, available, active);
    }
}