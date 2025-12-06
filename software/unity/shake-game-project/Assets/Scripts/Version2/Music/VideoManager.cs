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
    private double gameStartTime = -1.0; // -1は未初期化を示す

    private void Start()
    {
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
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
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameStart += OnGameStart;
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
        LoadVideo();
    }

    private void LoadVideo()
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
        
        // 既に再生中の場合は一旦停止してから巻き戻す
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
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
        
        // 念のため時刻を0にリセット
        videoPlayer.time = 0;
        videoPlayer.Play();
        // dspTimeベースで正確にゲーム開始時刻を記録
        gameStartTime = AudioSettings.dspTime;
        ResetRag(); // ラグをリセットしておく
        Debug.Log($"[VideoManager] VideoPlay called: gameStartTime={gameStartTime:F6}, dspTime={AudioSettings.dspTime:F6}, videoTime={videoPlayer.time:F6}");
    }

    // 音楽時刻(dspTimeベース)。音楽時刻はVideoManagerが管理することとする。
    public double GetMusicTime()
    {
        // gameStartTimeが未初期化(-1)の場合は0を返す
        if (gameStartTime < 0.0) return 0.0;
        return AudioSettings.dspTime - gameStartTime;
    }

    // 参考: 実動画時間（同期検証やデバッグ用）
    public double GetVideoTime()
    {
        return videoPlayer != null ? videoPlayer.time : 0.0;
    }
    //同期検証用
    public double GetLag()
    {
        if (videoPlayer == null) return 0.0;
        double videoTime = videoPlayer.time;
        double musicTime = AudioSettings.dspTime - gameStartTime;
        return musicTime - videoTime;
    }
    //ラグリセット用,dspTimeベースの曲内時間とvideoPlayer.timeがずれたなら、デバッグログを出してgameStartTimeを調整する
    public double ResetRag()
    {
        if (videoPlayer == null) return 0.0;
        double videoTime = videoPlayer.time;
        double musicTime = AudioSettings.dspTime - gameStartTime;
        double lag = musicTime - videoTime;
        Debug.LogWarning($"[VideoManager] ResetRag: videoTime={videoTime:F6}, musicTime={musicTime:F6}, lag={lag:F6}");
        //gameStartTimeを調整してラグをリセット
        gameStartTime = AudioSettings.dspTime - videoTime;
        return lag;
    }

    private void OnGameStart()
    {
        Debug.Log("[VideoManager] OnGameStart called");
        PlayFromStart();
    }

    private void Update()
    {
        // デバッグ用: 動画の準備状態を表示
        // Debug.Log($"[VideoManager] isPrepared: {isPrepared}, isPlaying: {videoPlayer.isPlaying}, time: {videoPlayer.time}");
        // デバッグ用: 動画時間と音楽時間のずれが大きい場合に警告を表示
        if (isPrepared && videoPlayer.isPlaying)
        {
            double lag = GetLag();
            if (System.Math.Abs(lag) > 0.01)
            {
                //Debug.LogWarning($"[VideoManager] Large lag detected: {lag:F6} seconds.");
            }
            if (System.Math.Abs(lag) > 0.03)
            {
                ResetRag();
                Debug.LogWarning($"[VideoManager] Lag exceeded 30ms, resetting rag.");
            }
        }

    }
}
