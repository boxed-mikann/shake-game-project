using UnityEngine;

/// <summary>
/// ========================================
/// Phase6ShakeHandler（新アーキテクチャ版）
/// ========================================
/// 
/// フェーズ：RestPhase（休符フェーズ）
/// 責務：休符を叩いた時の処理（ペナルティ）
/// - スコア減算：-1
/// - フリーズあり（INPUT_LOCK_DURATION）
/// 
/// ========================================
/// </summary>
public class Phase6ShakeHandler : MonoBehaviour, IShakeHandler
{
    [SerializeField] private int _scoreValue = -1;                                  // 休符フェーズのペナルティ
    [SerializeField] private float _freezeDuration = GameConstants.INPUT_LOCK_DURATION;  // フリーズ時間
    
    public void HandleShake()
    {
        // 1. 最古 Note 取得
        if (NoteManager.Instance == null)
        {
            Debug.LogWarning("[Phase6ShakeHandler] NoteManager instance not found!");
            return;
        }
        
        Note oldest = NoteManager.Instance.GetOldestNote();
        if (oldest == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[Phase6ShakeHandler] No notes to destroy");
            return;
        }
        
        // 2. 最古 Note を破棄
        NoteManager.Instance.DestroyOldestNote();
        
        // 3. 効果音再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("hit");
        }
        
        // 4. スコア減算（ペナルティ）
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scoreValue);
        }
        
        // 5. フリーズ開始（RestPhase のペナルティ）
        if (_freezeDuration > 0 && FreezeManager.Instance != null)
        {
            FreezeManager.Instance.StartFreeze(_freezeDuration);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[Phase6ShakeHandler] Rest note hit! Penalty: {_scoreValue}, Freeze: {_freezeDuration}s");
    }
}