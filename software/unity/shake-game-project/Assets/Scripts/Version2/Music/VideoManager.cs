using System.IO;
using UnityEngine;
using UnityEngine.Video;

//大体チェック済み

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Settings")] 
    [Tooltip("StreamingAssets からの相対パス。例: 'Videos/test_video.mp4'")]
    [SerializeField] private string streamingRelativePath = "Videos/UTA Live.mp4";
    [SerializeField] private bool autoLoadOnStart = true;

    private bool isPrepared = false;

    public VideoPlayer Player => videoPlayer;
    //ゲーム開始タイミングを記録する変数
    private float gameStartTime;

    private void OnEnable()
    {
        // ゲームフェーズイベントを購読
        if (GameManagerV2.Instance != null)
        {
            //GameManagerV2.Instance.OnResisterStart += OnResisterStart;
            //GameManagerV2.Instance.OnIdleStart += OnIdleStart;
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            //GameManagerV2.Instance.OnGameEnd += OnGameEnd;
        }
    }

    private void OnDisable()
    {
        // unsubscribe to avoid memory leaks
        if (GameManagerV2.Instance != null)
        {
            //GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
            //GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            //GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

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
        // // GameConstantsV2 に定義があれば優先
        // if (!string.IsNullOrEmpty(GameConstantsV2VideoPath))
        //     streamingRelativePath = GameConstantsV2VideoPath;

        if (autoLoadOnStart)
        {
            LoadFromStreamingAssets(streamingRelativePath);
            //パスをデバッグログ
            Debug.Log($"[VideoManager] Loading video from StreamingAssets: {streamingRelativePath}");
            Prepare();
        }
    }

    // private static string GameConstantsV2VideoPath
    // {
    //     get
    //     {
    //         try
    //         {
    //             // ある場合のみ使う（存在しない場合でも例外は出さない）
    //             var field = typeof(GameConstantsV2).GetField("VIDEO_RELATIVE_PATH");
    //             return field != null ? (string)field.GetValue(null) : null;
    //         }
    //         catch { return null; }
    //     }
    // }

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
            VideoPlay();
        }
    }

    private void PlayOnPreparedLoopOnce(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= PlayOnPreparedLoopOnce;
        if (videoPlayer == null) return;
        VideoPlay();
    }

    public void PlayFromStart()
    {
        if (videoPlayer == null) return;
        videoPlayer.isLooping = true;//false;TODO ずっとループで、ゲーム開始時にかいしりせっとだけということにしようかな
        if (!isPrepared)
        {
            Prepare();
            videoPlayer.prepareCompleted += PlayFromStartOnPreparedOnce;
        }
        else
        {
            videoPlayer.time = 0;
            VideoPlay();
        }
    }

    private void PlayFromStartOnPreparedOnce(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= PlayFromStartOnPreparedOnce;
        if (videoPlayer == null) return;
        videoPlayer.time = 0;
        VideoPlay();
    }

    public void Stop()
    {
        if (videoPlayer == null) return;
        videoPlayer.Stop();
    }

    //videoPlayer.Play()の時にゲーム開始タイミングを記録する。videoPlayer.Play()をこれで置き換える
    private void VideoPlay()
    {
        if (videoPlayer == null) return;
        videoPlayer.Play();
        gameStartTime = (float)AudioSettings.dspTime - (float)videoPlayer.time;
    }

    // 音楽時刻（dspTimeベース）。音楽時刻はVideoManagerが管理することとする。
    public float GetMusicTime()
    {
        return (float)(AudioSettings.dspTime - gameStartTime);
    }

    // 参考: 実動画時間（同期検証やデバッグ用）
    public float GetVideoTime()
    {
        return videoPlayer != null ? (float)videoPlayer.time : 0f;
    }
    //同期検証用
    public float GetLag()
    {
        if (videoPlayer == null) return 0f;
        float videoTime = (float)videoPlayer.time;
        float musicTime = (float)(AudioSettings.dspTime - gameStartTime);
        return musicTime - videoTime;
    }
    //ラグリセット用,dspTimeベースの曲内時間とvideoPlayer.timeがずれたなら、デバッグログを出してgameStartTimeを調整する
    public float ResetRag()
    {
        if (videoPlayer == null) return 0f;
        float videoTime = (float)videoPlayer.time;
        float musicTime = (float)(AudioSettings.dspTime - gameStartTime);
        float lag = musicTime - videoTime;
        Debug.LogWarning($"[VideoManager] ResetRag: videoTime={videoTime:F3}, musicTime={musicTime:F3}, lag={lag:F3}");
        //gameStartTimeを調整してラグをリセット
        gameStartTime = (float)AudioSettings.dspTime - videoTime;
        return lag;
    }

    private void OnGameStart()
    {
        PlayFromStart();
    }

    private void Update()
    {
        // デバッグ用: 動画の準備状態を表示
        // Debug.Log($"[VideoManager] isPrepared: {isPrepared}, isPlaying: {videoPlayer.isPlaying}, time: {videoPlayer.time}");
        // デバッグ用: 動画時間と音楽時間のずれが大きい場合に警告を表示
        if (isPrepared && videoPlayer.isPlaying)
        {
            float lag = GetLag();
            if (Mathf.Abs(lag) > 0.5f)
            {
                //Debug.LogWarning($"[VideoManager] Large lag detected: {lag:F3} seconds.");
            }
        }
    }
}
