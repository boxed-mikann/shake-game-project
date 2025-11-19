using UnityEngine;

/// <summary>
/// ========================================
/// FreezeEffectUI（新アーキテクチャ版）
/// ========================================
/// 
/// 責補：フリーズ凍結ビジュアル表示
/// 主機能：
/// - FreezeManager.OnFreezeChanged を購読
/// - true: 半透明フラッシュ表示
/// - false: 非表示
/// 
/// ========================================
/// </summary>
public class FreezeEffectUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup _freezeOverlay;
    
    [Header("Effect Settings")]
    [SerializeField] private float _freezeAlpha = 0.5f;
    [SerializeField] private float _normalAlpha = 0f;
    
    void Start()
    {
        // FreezeManager のイベントを購読
        if (FreezeManager.Instance != null)
        {
            FreezeManager.OnFreezeChanged.AddListener(OnFreezeChanged);
        }
        else
        {
            Debug.LogError("[FreezeEffectUI] FreezeManager instance not found!");
        }
        
        // 初期状態：非表示
        if (_freezeOverlay != null)
        {
            _freezeOverlay.alpha = _normalAlpha;
        }
    }
    
    /// <summary>
    /// フリーズ状態変更時のハンドラ
    /// </summary>
    private void OnFreezeChanged(bool isFrozen)
    {
        if (_freezeOverlay == null)
        {
            Debug.LogWarning("[FreezeEffectUI] Freeze overlay is not assigned!");
            return;
        }
        
        // フリーズ状態に応じて透明度を変更
        _freezeOverlay.alpha = isFrozen ? _freezeAlpha : _normalAlpha;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[FreezeEffectUI] Freeze effect {(isFrozen ? "activated" : "deactivated")}");
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (FreezeManager.OnFreezeChanged != null)
        {
            FreezeManager.OnFreezeChanged.RemoveListener(OnFreezeChanged);
        }
    }
}