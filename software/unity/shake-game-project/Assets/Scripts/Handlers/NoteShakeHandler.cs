using UnityEngine;

/// <summary>
/// 音符フェーズのシェイク処理（NotePhase, LastSprintPhase共通）
/// 処理：最古Note破棄 + SE + スコア加算
/// </summary>
public class NoteShakeHandler : MonoBehaviour, IShakeHandler
{
    [SerializeField] private int _scoreValue = 1;  // スコア値（Inspector設定可能）
    
    public void HandleShake(string data, double timestamp)
    {
        // 1. 効果音
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("hit");

        // 2. 最古Note取得
        if (NoteManager.Instance == null)
        {
            Debug.LogWarning("[NoteShakeHandler] NoteManager instance not found!");
            return;
        }
        
        Note oldest = NoteManager.Instance.GetOldestNote();
        if (oldest == null)
        {
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[NoteShakeHandler] No notes to destroy");
            return;
        }
        
        // 3. 位置を記録（破棄前に取得）
        Vector3 notePosition = oldest.transform.position;
        
        // 4. 最古Note破棄
        NoteManager.Instance.DestroyOldestNote();
        
        // 5. エフェクト再生
        if (EffectPool.Instance != null)
            EffectPool.Instance.PlayEffect(notePosition, Quaternion.identity);
        
        // 6. スコア加算
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(_scoreValue);
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteShakeHandler] Note destroyed with effect, score +{_scoreValue}");
    }
    
    /// <summary>
    /// Inspector または PhaseManager から呼び出してスコア値を設定
    /// </summary>
    public void SetScoreValue(int score) 
    { 
        _scoreValue = score;
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteShakeHandler] Score value set to: {score}");
    }
}
