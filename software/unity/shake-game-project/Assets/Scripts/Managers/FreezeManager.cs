using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// ========================================
/// FreezeManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：フリーズエフェクト（画面フラッシュ、入力禁止）の管理
/// - Phase*ShakeHandler から StartFreeze(duration) で呼ばれる
/// - Coroutine で duration 待機後に解除
/// - LastSprintPhase 中は StartFreeze() は何もしない（無効）
/// 
/// イベント発行：
/// - OnFreezeChanged(bool) → UI層に通知（true=凍結開始、false=凍結解除）
/// 
/// 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の TriggerFreeze() ロジック
/// 
/// ========================================
/// </summary>
public class FreezeManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static FreezeManager _instance;
    public static FreezeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<FreezeManager>();
            }
            return _instance;
        }
    }
    
    // フリーズ変更イベント（true=凍結開始、false=凍結解除）
    public static UnityEvent<bool> OnFreezeChanged = new UnityEvent<bool>();
    
    // 凍結状態
    private bool _isFrozen = false;
    private Coroutine _freezeCoroutine = null;
    
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
            Debug.Log("[FreezeManager] Initialized");
    }
    
    private void OnEnable()
    {
        // タイトル復帰時にリセット
        GameManager.OnShowTitle.AddListener(ResetFreezeState);
    }
    
    private void OnDisable()
    {
        // イベント購読解除
        GameManager.OnShowTitle.RemoveListener(ResetFreezeState);
    }
    
    /// <summary>
    /// フリーズ状態をリセット
    /// </summary>
    private void ResetFreezeState()
    {
        // Coroutine停止
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
            _freezeCoroutine = null;
        }
        
        // 凍結解除
        if (_isFrozen)
        {
            _isFrozen = false;
            OnFreezeChanged.Invoke(false);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[FreezeManager] Reset to initial state");
    }
    
    /// <summary>
    /// フリーズ開始
    /// </summary>
    public void StartFreeze(float duration)
    {
        // 既に凍結中の場合はスキップ
        if (_isFrozen)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[FreezeManager] Already frozen, skipping...");
            return;
        }
        
        // LastSprintPhase 中は凍結しない（無効化）
        if (PhaseManager.Instance != null && 
            PhaseManager.Instance.GetCurrentPhase() == Phase.LastSprintPhase)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[FreezeManager] LastSprintPhase detected, freeze disabled");
            return;
        }
        
        // 凍結開始
        _isFrozen = true;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[FreezeManager] ⏸️ Freeze triggered! Duration: {duration}s");
        
        // OnFreezeChanged イベント発行（凍結開始）
        OnFreezeChanged.Invoke(true);
        
        // Coroutine 開始
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
        }
        _freezeCoroutine = StartCoroutine(FreezeCoroutine(duration));
    }
    
    /// <summary>
    /// フリーズ Coroutine
    /// </summary>
    private IEnumerator FreezeCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // 凍結解除
        _isFrozen = false;
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[FreezeManager] ❌ Freeze released");
        
        // OnFreezeChanged イベント発行（凍結解除）
        OnFreezeChanged.Invoke(false);
    }
    
    /// <summary>
    /// 凍結状態を取得
    /// </summary>
    public bool IsFrozen
    {
        get { return _isFrozen; }
    }
}