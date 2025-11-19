using UnityEngine;

/// <summary>
/// 休符フェーズのシェイク処理（RestPhase）
/// 処理：フリーズ状態でなければフリーズ開始
/// </summary>
public class RestShakeHandler : MonoBehaviour, IShakeHandler
{
    public void HandleShake(string data, double timestamp)
    {
        // FreezeManager確認
        if (FreezeManager.Instance == null)
        {
            Debug.LogWarning("[RestShakeHandler] FreezeManager instance not found!");
            return;
        }
        
        // フリーズ中なら何もしない
        if (FreezeManager.Instance.IsFrozen)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[RestShakeHandler] Already frozen, ignoring input");
            return;
        }
        
        // フリーズ開始
        FreezeManager.Instance.StartFreeze(GameConstants.INPUT_LOCK_DURATION);
        
        // 効果音
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("freeze_start");
        
        // スコア減算
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(GameConstants.REST_PENALTY);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[RestShakeHandler] Freeze started, score penalty applied");
    }
}
