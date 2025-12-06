using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 単ノーツのオブジェクトプール
/// 軽量化のためプール機構を採用
/// </summary>
public class NotePoolV2 : MonoBehaviour
{
    public static NotePoolV2 Instance { get; private set; }
    
    [Header("Pool Settings")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 50;
    
    private Queue<GameObject> notePool = new Queue<GameObject>();
    private List<GameObject> activeNotes = new List<GameObject>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        InitializePool();
    }
    
    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            if (notePrefab != null)
            {
                GameObject note = Instantiate(notePrefab, poolContainer);
                note.SetActive(false);
                notePool.Enqueue(note);
            }
        }
    }
    
    public GameObject GetNote()
    {
        GameObject note;
        
        if (notePool.Count > 0)
        {
            note = notePool.Dequeue();
        }
        else
        {
            // プール不足時は自動拡張
            if (notePrefab != null)
            {
                note = Instantiate(notePrefab, poolContainer);
                Debug.LogWarning("[NotePoolV2] プール不足のため拡張");
            }
            else
            {
                Debug.LogError("[NotePoolV2] ノーツプレファブが設定されていません");
                return null;
            }
        }
        
        activeNotes.Add(note);
        note.SetActive(true);
        return note;
    }
    
    public void ReturnNote(GameObject note)
    {
        if (note == null) return;
        
        activeNotes.Remove(note);
        note.SetActive(false);
        note.transform.SetParent(poolContainer);
        notePool.Enqueue(note);
    }
    
    public void ClearAllNotes()
    {
        // アクティブなノーツを全て回収
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i] != null)
            {
                ReturnNote(activeNotes[i]);
            }
        }
        activeNotes.Clear();
    }
    
    public int GetActiveCount() => activeNotes.Count;
    public int GetPoolCount() => notePool.Count;
}
