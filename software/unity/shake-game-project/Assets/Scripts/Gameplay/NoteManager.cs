using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ========================================
/// NoteManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：アクティブ音符の時系列管理・最古削除
/// - 表示中の Note を Queue<Note> で時系列保持
/// - NoteSpawner から新規 Note を登録
/// - Phase*ShakeHandler が DestroyOldestNote() を呼び出す
/// 
/// FIFO（First In First Out）順序保証：
/// - 最も古い Note を常に正確に取得可能
/// 
/// ========================================
/// </summary>
public class NoteManager : MonoBehaviour
{
    // アクティブ Note のキュー（FIFO）
    private Queue<Note> _activeNotes = new Queue<Note>();
    
    // シングルトンインスタンス
    private static NoteManager _instance;
    public static NoteManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NoteManager>();
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
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NoteManager] Initialized");
    }
    
    private void OnEnable()
    {
        // GameManager.OnGameStart を購読してリセット
        GameManager.OnGameStart.AddListener(ClearAllNotes);
        GameManager.OnShowTitle.AddListener(ClearAllNotes);
    }
    
    private void OnDisable()
    {
        // イベント購読解除
        GameManager.OnGameStart.RemoveListener(ClearAllNotes);
        GameManager.OnShowTitle.RemoveListener(ClearAllNotes);
    }
    
    /// <summary>
    /// アクティブリストに Note を追加
    /// NoteSpawner から呼ばれる
    /// </summary>
    public void AddNote(Note note)
    {
        if (note == null)
        {
            Debug.LogWarning("[NoteManager] Tried to add null note!");
            return;
        }
        
        _activeNotes.Enqueue(note);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteManager] Note added (Active: {_activeNotes.Count})");
    }
    
    /// <summary>
    /// 最古の Note を取得（FIFO）
    /// Null の可能性あり（アクティブ Note がない場合）
    /// </summary>
    public Note GetOldestNote()
    {
        if (_activeNotes.Count == 0)
        {
            return null;
        }
        
        return _activeNotes.Peek();
    }
    
    /// <summary>
    /// 最古の Note をプール返却・リストから削除
    /// Phase*ShakeHandler から呼ばれる
    /// </summary>
    public void DestroyOldestNote()
    {
        if (_activeNotes.Count == 0)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[NoteManager] No notes to destroy");
            return;
        }
        
        // 最古の Note を取得
        Note oldestNote = _activeNotes.Dequeue();
        
        if (oldestNote == null)
        {
            Debug.LogWarning("[NoteManager] Oldest note was null!");
            return;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteManager] Note destroyed (Remaining: {_activeNotes.Count})");
        
        // プールに返却
        if (NotePool.Instance != null)
        {
            NotePool.Instance.ReturnNote(oldestNote);
        }
        else
        {
            // NotePool がない場合は直接破棄
            Destroy(oldestNote.gameObject);
        }
    }
    
    /// <summary>
    /// アクティブ音符数を取得
    /// </summary>
    public int GetActiveNoteCount()
    {
        return _activeNotes.Count;
    }
    
    /// <summary>
    /// すべての Note をクリア（ゲーム開始時に呼ばれる）
    /// </summary>
    private void ClearAllNotes()
    {
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteManager] Clearing all notes ({_activeNotes.Count})");
        
        // すべての Note をプールに返却
        while (_activeNotes.Count > 0)
        {
            Note note = _activeNotes.Dequeue();
            
            if (note != null && NotePool.Instance != null)
            {
                NotePool.Instance.ReturnNote(note);
            }
        }
        
        _activeNotes.Clear();
    }
}