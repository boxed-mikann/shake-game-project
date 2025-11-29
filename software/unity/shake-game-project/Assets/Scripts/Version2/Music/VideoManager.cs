using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Settings")] 
    [Tooltip("StreamingAssets からの相対パス。例: 'Videos/test_video.mp4'")]
    [SerializeField] private string streamingRelativePath = "Videos/test_video.mp4";
    [SerializeField] private bool autoLoadOnStart = true;

    private bool isPrepared = false;

    public VideoPlayer Player => videoPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        HookPlayerEvents();
    }

    private void Start()
    {
        // GameConstantsV2 に定義があれば優先
        if (!string.IsNullOrEmpty(GameConstantsV2VideoPath))
            streamingRelativePath = GameConstantsV2VideoPath;

        if (autoLoadOnStart)
        {
            LoadFromStreamingAssets(streamingRelativePath);
            Prepare();
        }
    }

    private static string GameConstantsV2VideoPath
    {
        get
        {
            try
            {
                // ある場合のみ使う（存在しない場合でも例外は出さない）
                var field = typeof(GameConstantsV2).GetField("VIDEO_RELATIVE_PATH");
                return field != null ? (string)field.GetValue(null) : null;
            }
            catch { return null; }
        }
    }

    private void HookPlayerEvents()
    {
        videoPlayer.errorReceived += (vp, msg) => Debug.LogError($"[VideoManager] Video error: {msg}");
        videoPlayer.prepareCompleted += vp => { isPrepared = true; };
        videoPlayer.skipOnDrop = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
    }

    public void LoadFromStreamingAssets(string relativePath)
    {
        if (videoPlayer == null) return;
        streamingRelativePath = relativePath;

        string filePath = Path.Combine(Application.streamingAssetsPath, relativePath);
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[VideoManager] File not found: {filePath}");
        }

        string url = filePath;
#if !UNITY_ANDROID
        if (!url.StartsWith("file://")) url = "file://" + url;
#endif
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        isPrepared = false;
    }

    public void Prepare()
    {
        if (videoPlayer == null) return;
        isPrepared = false;
        videoPlayer.Prepare();
    }

    public void PlayLoop()
    {
        if (videoPlayer == null) return;
        videoPlayer.isLooping = true;
        if (!isPrepared)
        {
            Prepare();
            videoPlayer.prepareCompleted += PlayOnPreparedLoopOnce;
        }
        else if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    private void PlayOnPreparedLoopOnce(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= PlayOnPreparedLoopOnce;
        if (videoPlayer == null) return;
        videoPlayer.Play();
    }

    public void PlayFromStart()
    {
        if (videoPlayer == null) return;
        videoPlayer.isLooping = false;
        if (!isPrepared)
        {
            Prepare();
            videoPlayer.prepareCompleted += PlayFromStartOnPreparedOnce;
        }
        else
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
    }

    private void PlayFromStartOnPreparedOnce(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= PlayFromStartOnPreparedOnce;
        if (videoPlayer == null) return;
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void Stop()
    {
        if (videoPlayer == null) return;
        videoPlayer.Stop();
    }

    // 音楽時刻（dspTimeベース）。VideoPlayer.time の代替として利用。
    public float GetMusicTime()
    {
        return GameManagerV2.Instance != null ? GameManagerV2.Instance.GetMusicTime() : 0f;
    }

    // 参考: 実動画時間（同期検証やデバッグ用）
    public float GetVideoTime()
    {
        return videoPlayer != null ? (float)videoPlayer.time : 0f;
    }
}
