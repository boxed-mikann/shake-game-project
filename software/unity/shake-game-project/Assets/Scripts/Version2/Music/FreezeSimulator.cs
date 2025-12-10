using System.Collections;
using UnityEngine;

public class FreezeSimulator : MonoBehaviour
{
    [Header("Freeze Settings")]
    [Tooltip("フリーズ持続時間(秒)")]
    [SerializeField] private float freezeDuration = 1.0f;
    [Tooltip("Unity全体を停止してdspTimeだけ進める実験")]
    [SerializeField] private bool simulateRealFreeze = true;
    [Tooltip("フリーズ開始キー")]
    [SerializeField] private KeyCode freezeKey = KeyCode.F;

    private bool isFreezeTestRunning = false;

    private void Update()
    {
        if (Input.GetKeyDown(freezeKey) && !isFreezeTestRunning)
        {
            if (VideoManager.Instance != null && VideoManager.Instance.Player.isPlaying)
            {
                if (simulateRealFreeze)
                {
                    StartCoroutine(SimulateRealFreeze());
                }
                else
                {
                    StartCoroutine(SimulateSimpleFreeze());
                }
            }
        }
    }

    /// <summary>
    /// 実際のフリーズを再現: Unity全体をブロックしてdspTimeだけ進める
    /// </summary>
    private IEnumerator SimulateRealFreeze()
    {
        isFreezeTestRunning = true;
        
        double startDspTime = AudioSettings.dspTime;
        double startVideoTime = VideoManager.Instance.Player.time;
        double startMusicTime = VideoManager.Instance.GetMusicTime();
        
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        Debug.LogWarning($"[FreezeSimulator] === REAL FREEZE START ===");
        Debug.LogWarning($"[FreezeSimulator] Before: Video={startVideoTime:F6}s | Music={startMusicTime:F6}s | dspTime={startDspTime:F6}");
        Debug.LogWarning($"[FreezeSimulator] Blocking Unity for {freezeDuration:F1}s (System.Threading)...");
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        
        // 次のフレームまで待機してからブロック開始
        yield return null;
        
        // System.Threading.Thread.Sleepでメインスレッドをブロック
        // これによりUnity全体が完全に停止し、dspTimeだけがOSレベルで進む
        int sleepMs = Mathf.RoundToInt(freezeDuration * 1000f);
        System.Threading.Thread.Sleep(sleepMs);
        
        // ブロック解除後
        double endDspTime = AudioSettings.dspTime;
        double endVideoTime = VideoManager.Instance.Player.time;
        double endMusicTime = VideoManager.Instance.GetMusicTime();
        
        double dspTimeDiff = endDspTime - startDspTime;
        double videoTimeDiff = endVideoTime - startVideoTime;
        
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        Debug.LogWarning($"[FreezeSimulator] === REAL FREEZE END (Blocked for {freezeDuration:F3}s) ===");
        Debug.LogWarning($"[FreezeSimulator] After: Video={endVideoTime:F6}s | Music={endMusicTime:F6}s | dspTime={endDspTime:F6}");
        Debug.LogWarning($"[FreezeSimulator] dspTime advanced: {dspTimeDiff:F6}s (expected: ~{freezeDuration:F1}s)");
        Debug.LogWarning($"[FreezeSimulator] VideoTime advanced: {videoTimeDiff:F6}s (expected: ~0.0s if frozen)");
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        
        isFreezeTestRunning = false;
    }
    
    /// <summary>
    /// 簡易フリーズ: VideoPlayerだけ停止
    /// </summary>
    private IEnumerator SimulateSimpleFreeze()
    {
        isFreezeTestRunning = true;
        var videoPlayer = VideoManager.Instance.Player;
        
        double startVideoTime = videoPlayer.time;
        double startMusicTime = VideoManager.Instance.GetMusicTime();
        
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        Debug.LogWarning($"[FreezeSimulator] === SIMPLE FREEZE START ===");
        Debug.LogWarning($"[FreezeSimulator] Before: Video={startVideoTime:F6}s | Music={startMusicTime:F6}s");
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        
        videoPlayer.Pause();
        
        yield return new WaitForSecondsRealtime(freezeDuration);
        
        videoPlayer.Play();
        
        double endVideoTime = videoPlayer.time;
        double endMusicTime = VideoManager.Instance.GetMusicTime();
        
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        Debug.LogWarning($"[FreezeSimulator] === SIMPLE FREEZE END ===");
        Debug.LogWarning($"[FreezeSimulator] After: Video={endVideoTime:F6}s | Music={endMusicTime:F6}s");
        Debug.LogWarning($"[FreezeSimulator] ========================================");
        
        isFreezeTestRunning = false;
    }
}
