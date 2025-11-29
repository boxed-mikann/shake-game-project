using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ポップアップのオブジェクトプール
/// Version1のEffectPoolを参考に実装
/// </summary>
public class PopupPool : MonoBehaviour
{
    public static PopupPool Instance { get; private set; }
    
    [Header("Pool Settings")]
    [SerializeField] private GameObject syncPopupPrefab;
    [SerializeField] private GameObject judgePopupPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = 10;
    
    private Queue<GameObject> syncPopupPool = new Queue<GameObject>();
    private Queue<GameObject> judgePopupPool = new Queue<GameObject>();
    
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
        // SyncPopupの初期プール
        for (int i = 0; i < initialPoolSize; i++)
        {
            if (syncPopupPrefab != null)
            {
                GameObject popup = Instantiate(syncPopupPrefab, poolContainer);
                popup.SetActive(false);
                syncPopupPool.Enqueue(popup);
            }
        }
        
        // JudgePopupの初期プール
        for (int i = 0; i < initialPoolSize; i++)
        {
            if (judgePopupPrefab != null)
            {
                GameObject popup = Instantiate(judgePopupPrefab, poolContainer);
                popup.SetActive(false);
                judgePopupPool.Enqueue(popup);
            }
        }
    }
    
    public GameObject GetSyncPopup()
    {
        return GetPopup(syncPopupPool, syncPopupPrefab);
    }
    
    public GameObject GetJudgePopup()
    {
        return GetPopup(judgePopupPool, judgePopupPrefab);
    }
    
    private GameObject GetPopup(Queue<GameObject> pool, GameObject prefab)
    {
        GameObject popup;
        
        if (pool.Count > 0)
        {
            popup = pool.Dequeue();
        }
        else
        {
            // プール不足時は自動拡張
            if (prefab != null)
            {
                popup = Instantiate(prefab, poolContainer);
                Debug.LogWarning($"[PopupPool] プール不足のため拡張: {prefab.name}");
            }
            else
            {
                Debug.LogError("[PopupPool] プレファブが設定されていません");
                return null;
            }
        }
        
        popup.SetActive(true);
        return popup;
    }
    
    public void ReturnSyncPopup(GameObject popup)
    {
        ReturnPopup(popup, syncPopupPool);
    }
    
    public void ReturnJudgePopup(GameObject popup)
    {
        ReturnPopup(popup, judgePopupPool);
    }
    
    private void ReturnPopup(GameObject popup, Queue<GameObject> pool)
    {
        if (popup == null) return;
        
        popup.SetActive(false);
        popup.transform.SetParent(poolContainer);
        pool.Enqueue(popup);
    }
}
