using UnityEngine;

/// <summary>
/// ========================================
/// Phase7ShakeHandler（新アーキテクチャ版）
/// ========================================
/// 
/// フェーズ：LastSprintPhase（ラストスパートフェーズ）
/// 責務：ラストスパート時の音符を叩いた時の処理
/// - スコア加算：+2（ボーナス）
/// - フリーズなし
/// 
/// ========================================
/// </summary>
public class Phase7ShakeHandler : MonoBehaviour, IShakeHandler
{
    [SerializeField] private int _scoreValue = 2;           // ラストスパートボーナス
    [SerializeField] private float _freezeDuration = 0f;    // フリーズなし
    
    public void HandleShake()
    {
        // 1. 最古 Note 取得
        if (NoteManager.Instance == null)
        {
            Debug.LogWarning("[Phase7ShakeHandler] NoteManager instance not found!");
            return;
        }
        
        Note oldest = NoteManager.Instance.GetOldestNote();
        if (oldest == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[Phase7ShakeHandler] No notes to destroy");
            return;
        }
        
        // 2. 最古 Note を破棄
        NoteManager.Instance.DestroyOldestNote();
        
        // 3. 効果音再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("hit");
        }
        
        // 4. スコア加算（ボーナス）
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scoreValue);
        }
        
        // 5. フリーズなし（LastSprintPhase）
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[Phase7ShakeHandler] LastSprint note hit! Bonus Score: +{_scoreValue}");
    }
}