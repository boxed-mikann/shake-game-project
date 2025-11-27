using UnityEngine;
using System.Collections.Generic;
using CartoonFX;

/// <summary>
/// ========================================
/// EffectPool（エフェクトプール）
/// ========================================
/// 
/// 責務：エフェクトのObject Pool管理
/// 
/// 機能：
///   - 起動時にエフェクトをプリロード
///   - 非アクティブなエフェクトを再利用
///   - プール不足時は自動拡張
///   - CFXR_Effectの自動Disable機能を活用
/// 
/// 実装詳細：
///   - CFXR_Effect.clearBehavior = Disable に設定
///   - エフェクト終了時に自動でSetActive(false)される
///   - プール側は非アクティブなエフェクトを探すだけでOK
/// 
/// パフォーマンス：
///   - 初期プールサイズ: 50
///   - 再生コスト: < 1ms
///   - List.Find: O(n)だが通常は最初の数個で見つかる
/// 
/// ========================================
/// </summary>
public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }
    
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int initialPoolSize = GameConstants.EFFECT_POOL_INITIAL_SIZE;
    
    private List<GameObject> _allEffects = new List<GameObject>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject effect = Instantiate(effectPrefab, poolContainer);
            
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null)
            {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
            }
            else
            {
                Debug.LogWarning("[EffectPool] Prefab does not have CFXR_Effect component!");
            }
            
            effect.SetActive(false);
            _allEffects.Add(effect);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Initialized with {initialPoolSize} effects");
    }
    
    public void PlayEffect(Vector3 position, Quaternion rotation)
    {
        GameObject effect = _allEffects.Find(e => !e.activeInHierarchy);
        
        if (effect == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.LogWarning("[EffectPool] Pool exhausted, creating new effect");
            
            effect = Instantiate(effectPrefab, poolContainer);
            var cfxrEffect = effect.GetComponent<CFXR_Effect>();
            if (cfxrEffect != null)
            {
                cfxrEffect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;
            }
            _allEffects.Add(effect);
        }
        
        effect.transform.position = position;
        effect.transform.rotation = rotation;
        
        var cfxr = effect.GetComponent<CFXR_Effect>();
        if (cfxr != null)
        {
            cfxr.ResetState();
        }
        
        effect.SetActive(true);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[EffectPool] Effect played at {position}");
    }
}
