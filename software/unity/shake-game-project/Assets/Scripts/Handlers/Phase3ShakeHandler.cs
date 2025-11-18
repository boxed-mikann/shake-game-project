using UnityEngine;

/// <summary>
/// ========================================
/// Phase3ShakeHandler（新アーキテクチャ版）
/// ========================================
/// 
/// フェーズ：NotePhase（音符フェーズ）
/// 責務：音符を叩いた時の処理
/// - スコア加算：+1
/// - フリーズなし
/// 
/// ========================================
/// </summary>
public class Phase3ShakeHandler : MonoBehaviour, IShakeHandler
{
    [SerializeField] private int _scoreValue = 1;           // 音符フェーズのスコア
    [SerializeField] private float _freezeDuration = 0f;    // フリーズなし
    
    public void HandleShake()
    {
        // 1. 最古 Note 取得
        if (NoteManager.Instance == null)
        {
            Debug.LogWarning("[Phase3ShakeHandler] NoteManager instance not found!");
            return;
        }
        
        Note oldest = NoteManager.Instance.GetOldestNote();
        if (oldest == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[Phase3ShakeHandler] No notes to destroy");
            return;
        }
        
        // 2. 最古 Note を破棄
        NoteManager.Instance.DestroyOldestNote();
        
        // 3. 効果音再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("hit");
        }
        
        // 4. スコア加算
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scoreValue);
        }
        
        // 5. フリーズなし（NotePhase）
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[Phase3ShakeHandler] Note hit! Score: +{_scoreValue}");
    }
}