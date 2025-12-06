using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 長押しノーツのオブジェクトプール
/// </summary>
public class LongNotePoolV2 : MonoBehaviour
{
    public static LongNotePoolV2 Instance { get; private set; }
    
    [Header("Pool Settings")]
    [SerializeField] private GameObject longNotePrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 10;
    
    private Queue<GameObject> longNotePool = new Queue<GameObject>();
    private List<GameObject> activeLongNotes = new List<GameObject>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePool();
    }
    
    private void InitializePool()
    {
        if (poolContainer == null)
        {
            poolContainer = transform;
        }
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            if (longNotePrefab != null)
            {
                GameObject longNote = Instantiate(longNotePrefab, poolContainer);
                longNote.SetActive(false);
                longNotePool.Enqueue(longNote);
            }
        }
        
        Debug.Log($"[LongNotePoolV2] プール初期化完了: {initialPoolSize}個");
    }
    
    public GameObject GetLongNote()
    {
        GameObject longNote;
        
        if (longNotePool.Count > 0)
        {
            longNote = longNotePool.Dequeue();
        }
        else
        {
            // プール不足時は自動拡張
            if (longNotePrefab != null)
            {
                longNote = Instantiate(longNotePrefab, poolContainer);
                Debug.LogWarning("[LongNotePoolV2] プール不足のため拡張");
            }
            else
            {
                Debug.LogError("[LongNotePoolV2] 長押しノーツプレファブが設定されていません");
                return null;
            }
        }
        
        activeLongNotes.Add(longNote);
        longNote.SetActive(true);
        return longNote;
    }
    
    public void ReturnLongNote(GameObject longNote)
    {
        if (longNote == null) return;
        
        activeLongNotes.Remove(longNote);
        longNote.SetActive(false);
        longNote.transform.SetParent(poolContainer);
        longNotePool.Enqueue(longNote);
    }
    
    public void ClearAllLongNotes()
    {
        // アクティブな長押しノーツを全て回収
        for (int i = activeLongNotes.Count - 1; i >= 0; i--)
        {
            if (activeLongNotes[i] != null)
            {
                ReturnLongNote(activeLongNotes[i]);
            }
        }
        activeLongNotes.Clear();
    }
    
    public int GetActiveCount() => activeLongNotes.Count;
    public int GetPoolCount() => longNotePool.Count;
}
