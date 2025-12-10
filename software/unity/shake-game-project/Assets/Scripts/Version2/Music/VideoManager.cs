using System.IO;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;

//大体チェック済み

public class VideoManager : MonoBehaviour
{
    public static VideoManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    private AudioSource audioSource; // VideoPlayerに紐づくAudioSource

    [Header("Settings")]
    [Tooltip("VideoPlayerの更新モード(GameTime推奨: フリーズ時に動画も停止)")]
    [SerializeField] private VideoTimeUpdateMode updateMode = VideoTimeUpdateMode.GameTime;
    
    [Tooltip("待機画面用動画パス(StreamingAssetsからの相対パス)")]
    [SerializeField] private string idleVideoPath = "Videos/UTA Live.mp4";
    [Tooltip("ゲームプレイ用動画パス(StreamingAssetsからの相対パス)")]
    [SerializeField] private string gameVideoPath = "Videos/UTA Live.mp4";
    [Tooltip("待機画面動画をストリーミング再生(大きいファイル向け)")]
    [SerializeField] private bool idleVideoStreaming = true;
    [Tooltip("ゲーム動画をストリーミング再生(false=全ロード後再生)")]
    [SerializeField] private bool gameVideoStreaming = false;
    [SerializeField] private bool autoLoadOnStart = true;
    
    [Header("Time Comparison Debug")]
    [Tooltip("時刻比較デバッグモード(ResetRag無効化)")]
    [SerializeField] private bool enableTimeComparisonDebug = false;
    [Tooltip("時刻比較ログの表示間隔(秒)")]
    [SerializeField] private float timeComparisonLogInterval = 0.5f;
    
    [Header("Lag Correction Settings")]
    [Tooltip("自動ラグ補正を有効化(恒常的に作用)")]
    [SerializeField] private bool enableAutoLagCorrection = true;
    [Tooltip("ラグ補正の頻度(秒)")]
    [SerializeField] private float lagCorrectionInterval = 0.1f;
    [Tooltip("補正を発動するラグ閾値(秒)")]
    [SerializeField] private float lagCorrectionThreshold = 0.02f; // 20ms
    [Tooltip("手動でResetRag()を呼び出すキー")]
    [SerializeField] private KeyCode manualResetRagKey = KeyCode.R;

    private bool isPrepared = false;

    public VideoPlayer Player => videoPlayer;
    //ゲーム開始タイミングを記録する変数
    private double gameStartTime = -1.0; // -1は未初期化を示す
    
    // VideoPlayer.time急変検出用
    private double lastVideoTime = 0.0;
    private int videoTimeStableFrames = 0;
    private const int REQUIRED_STABLE_FRAMES = 5; // 5フレーム安定したら補正OK
    private const double VIDEO_TIME_JUMP_THRESHOLD = 0.5; // 0.5秒以上のジャンプを検出
    
    // 時刻比較デバッグ用
    private float lastTimeComparisonLogTime = 0f;
    
    // ラグ補正用
    private float lastLagCorrectionTime = 0f;
    private double timeOffset = 0.0; // ノーツ時刻の補正オフセット
    
    // 強制同期チェック無効化期間（再生開始直後の不安定なVideoTimeを無視するため）
    private float ignoreSyncCheckUntil = 0f;

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
            GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnResisterStart -= OnResisterStart;
            GameManagerV2.Instance.OnResisterStart += OnResisterStart;
            
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
            GameManagerV2.Instance.OnIdleStart += OnIdleStart;

            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
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
        if (autoLoadOnStart)
        {   
            // 待機画面用動画をストリーミングで読み込み
            LoadVideo(idleVideoPath, idleVideoStreaming);
            Debug.Log($"[VideoManager] Loading idle video: {idleVideoPath} (streaming={idleVideoStreaming})");
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
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnLoopPointReached;
        videoPlayer.skipOnDrop = true;
        
        // updateModeを設定
        videoPlayer.timeUpdateMode = updateMode;
        
        // AudioSourceコンポーネントを取得または追加
        audioSource = videoPlayer.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
        }
        
        // AudioSourceモードに設定してAudioSource.timeを使用可能にする
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        
        Debug.Log($"[VideoManager] VideoPlayer setup: updateMode={updateMode}, audioOutputMode={videoPlayer.audioOutputMode}");
    }
    
    private void OnVideoPrepared(VideoPlayer vp)
    {
        isPrepared = true;
        Debug.Log($"[VideoManager] Video prepared: duration={vp.length:F2}s, audioTrackCount={vp.audioTrackCount}, isPlaying={vp.isPlaying}");
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        Debug.LogWarning("[VideoManager] Loop point reached - resetting sync timing");
        SyncGameTime();
    }

    /// <summary>
    /// 動画を読み込む(ストリーミング/非ストリーミング対応)
    /// </summary>
    /// <param name="relativePath">StreamingAssetsからの相対パス</param>
    /// <param name="streaming">true=ストリーミング再生、false=全ロード後再生</param>
    public void LoadVideo(string relativePath, bool streaming)
    {
        if (videoPlayer == null) return;

        string filePath = Path.Combine(Application.streamingAssetsPath, relativePath);
        
#if UNITY_WEBGL
        string url = filePath;
        Debug.Log($"[VideoManager] WebGL URL: {url}");
#else
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[VideoManager] File not found: {filePath}");
        }

        string url = filePath;
        #if !UNITY_ANDROID
        if (!url.StartsWith("file://")) url = "file://" + url;
        #endif
        Debug.Log($"[VideoManager] Native URL: {url}");
#endif
        
        // ストリーミング設定(実際にはURL読み込みは同じ)
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        Debug.Log($"[VideoManager] Loading video: {relativePath} (streaming={streaming})");
        
        isPrepared = false;
    }
    
    /// <summary>
    /// 後方互換性のため残す
    /// </summary>
    public void LoadFromStreamingAssets(string relativePath)
    {
        LoadVideo(relativePath, true);
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
        videoPlayer.isLooping = true; // 常にループON
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
        videoPlayer.isLooping = true; // 常にループON
        
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
        
        // 時刻状態をリセット
        gameStartTime = -1.0;
        timeOffset = 0.0;
    }

    //videoPlayer.Play()の時にゲーム開始タイミングを記録する。videoPlayer.Play()をこれで置き換える
    private void VideoPlay()
    {
        if (videoPlayer == null) return;
        
        // 念のため時刻を0にリセット
        videoPlayer.time = 0;
        videoPlayer.Play();
        
        SyncGameTime();
        
        Debug.Log($"[VideoManager] VideoPlay called: updateMode={updateMode}, gameStartTime={gameStartTime:F6}, timeOffset={timeOffset:F6}");
    }

    private void SyncGameTime()
    {
        // updateModeに応じてゲーム開始時刻を記録
        if (updateMode == VideoTimeUpdateMode.GameTime)
        {
            gameStartTime = Time.time;
        }
        else // DSPTime
        {
            gameStartTime = AudioSettings.dspTime;
        }
        
        // VideoTime監視状態をリセット
        lastVideoTime = 0.0;
        videoTimeStableFrames = 0;
        
        // 補正オフセットをリセット
        timeOffset = 0.0;
        lastLagCorrectionTime = Time.time;
        
        // VideoPlayer.timeが0に更新されるまでラグがあるため、
        // 数秒間はGetMusicTimeでの強制同期チェックを無効化する
        ignoreSyncCheckUntil = Time.time + 2.0f;
        Debug.Log($"[VideoManager] SyncGameTime: Reset timing. Ignore sync check until {ignoreSyncCheckUntil:F2}");
    }

    // 音楽時刻(補正込み)。ノーツシステムが参照する時刻。
    public double GetMusicTime()
    {
        // gameStartTimeが未初期化(-1)の場合は0を返す
        if (gameStartTime < 0.0) return 0.0;
        
        double baseTime = (updateMode == VideoTimeUpdateMode.GameTime) ? Time.time : AudioSettings.dspTime;
        double calculatedTime = (baseTime - gameStartTime) + timeOffset;
        
        // 安全策: VideoPlayer.timeとの乖離が大きすぎる場合（ループやシーク発生時）は即座に同期
        // これにより、Update()の補正を待たずに正しい時刻を返せる（リザルト画面誤遷移の防止）
        // ただし、再生開始直後(ignoreSyncCheckUntil期間内)はVideoTimeが不安定な可能性があるためチェックしない
        if (videoPlayer != null && videoPlayer.isPlaying && Time.time > ignoreSyncCheckUntil)
        {
            double videoTime = videoPlayer.time;
            // 1秒以上のズレは異常（ループなど）とみなして強制同期
            if (System.Math.Abs(calculatedTime - videoTime) > 1.0)
            {
                Debug.LogWarning($"[VideoManager] Large sync error detected! Force syncing. Calc={calculatedTime:F3}, Video={videoTime:F3}, Diff={calculatedTime-videoTime:F3}");
                
                // timeOffsetを再計算してVideoTimeに合わせる
                timeOffset = videoTime - (baseTime - gameStartTime);
                return videoTime;
            }
        }
        
        return calculatedTime;
    }
    
    // 補正前の音楽時刻（内部用）
    private double GetMusicTimeRaw()
    {
        if (gameStartTime < 0.0) return 0.0;
        
        if (updateMode == VideoTimeUpdateMode.GameTime)
        {
            return Time.time - gameStartTime;
        }
        else // DSPTime
        {
            return AudioSettings.dspTime - gameStartTime;
        }
    }

    // 参考: 実動画時間(同期検証やデバッグ用)
    public double GetVideoTime()
    {
        // VideoPlayer.timeをそのまま使用
        return videoPlayer != null ? videoPlayer.time : 0.0;
    }
    //同期検証用
    public double GetLag()
    {
        if (videoPlayer == null) return 0.0;
        double videoTime = GetVideoTime();
        double musicTimeRaw = GetMusicTimeRaw(); // 補正前の時刻を使用
        return musicTimeRaw - videoTime;
    }
    /// <summary>
    /// ラグリセット: timeOffsetを調整してノーツ時刻を動画に合わせる
    /// VideoPlayer.timeは触らない（GameTimeモードなら自動的にゲームループと同期）
    /// </summary>
    public double ResetRag()
    {
        if (videoPlayer == null || !isPrepared) return 0.0;
        
        double videoTime = GetVideoTime();
        double musicTimeRaw = GetMusicTimeRaw();
        double currentOffset = timeOffset;
        double lag = musicTimeRaw - videoTime;
        
        Debug.LogWarning($"[VideoManager] ========================================");
        Debug.LogWarning($"[VideoManager] ResetRag() - Adjusting timeOffset (not touching VideoPlayer.time)");
        Debug.LogWarning($"[VideoManager] BEFORE: VideoTime={videoTime:F6}s, MusicTimeRaw={musicTimeRaw:F6}s, Lag={lag:F6}s, timeOffset={currentOffset:F6}s");
        
        // timeOffsetを調整してノーツ時刻を動画に合わせる
        // 新しいMusicTime = MusicTimeRaw + newOffset = VideoTime
        // → newOffset = VideoTime - MusicTimeRaw
        double newOffset = videoTime - musicTimeRaw;
        timeOffset = newOffset;
        
        double correctedMusicTime = GetMusicTime();
        double newLag = correctedMusicTime - videoTime;
        
        Debug.LogWarning($"[VideoManager] AFTER: timeOffset={timeOffset:F6}s, MusicTime(corrected)={correctedMusicTime:F6}s, NewLag={newLag:F6}s");
        Debug.LogWarning($"[VideoManager] Correction applied: {currentOffset:F6}s → {newOffset:F6}s (adjustment={newOffset - currentOffset:F6}s)");
        Debug.LogWarning($"[VideoManager] ========================================");
        
        return lag;
    }

    /// <summary>
    /// ゲームプレイ用の動画パスを設定する（選曲画面などから呼ぶ）
    /// </summary>
    public void SetGameVideoPath(string relativePath)
    {
        gameVideoPath = relativePath;
        Debug.Log($"[VideoManager] Game video path set to: {gameVideoPath}");
    }

    private void OnGameStart()
    {
        Debug.Log("[VideoManager] OnGameStart called - switching to game video");
        
        // 前回のゲーム時刻が残っていると、Load中のGetMusicTime()が巨大な値を返してしまい、
        // ノーツシステムが誤作動（一気に判定される）するのを防ぐためリセット
        gameStartTime = -1.0;
        timeOffset = 0.0;
        
        // パスが空の場合はロードしない（あるいはエラーログ）
        if (string.IsNullOrEmpty(gameVideoPath))
        {
            Debug.LogError("[VideoManager] Game video path is empty! Skipping video load.");
            return;
        }

        // URL(ファイルパス)モード
        LoadVideo(gameVideoPath, gameVideoStreaming);
        Prepare();
        
        // Prepare完了後に再生
        videoPlayer.prepareCompleted += OnGameVideoPrepared;
    }
    
    private void OnGameVideoPrepared(VideoPlayer vp)
    {
        videoPlayer.prepareCompleted -= OnGameVideoPrepared;
        Debug.Log("[VideoManager] Game video prepared, starting playback");
        PlayFromStart();
    }

    private void OnGameEnd()
    {
        Debug.Log("[VideoManager] OnGameEnd called - stopping video");
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    private void OnResisterStart()
    {
        Debug.Log("[VideoManager] OnResisterStart called - playing idle video");
        PlayIdleVideo();
    }

    private void OnIdleStart()
    {
        Debug.Log("[VideoManager] OnIdleStart called - playing idle video");
        PlayIdleVideo();
    }

    private void PlayIdleVideo()
    {
        // 待機画面用動画をロードして再生
        LoadVideo(idleVideoPath, idleVideoStreaming);
        PlayLoop();
    }

    private void Update()
    {
        // 手動ResetRag
        if (Input.GetKeyDown(manualResetRagKey) && isPrepared && videoPlayer.isPlaying)
        {
            Debug.LogWarning($"[VideoManager] Manual ResetRag triggered by key: {manualResetRagKey}");
            ResetRag();
            videoTimeStableFrames = 0;
        }
        
        // 時刻比較デバッグモード
        if (enableTimeComparisonDebug && isPrepared && videoPlayer.isPlaying)
        {
            if (Time.time - lastTimeComparisonLogTime >= timeComparisonLogInterval)
            {
                LogTimeComparison();
                lastTimeComparisonLogTime = Time.time;
            }
            return; // 通常のラグチェックをスキップ
        }
        
        // 自動ラグ補正（恒常的に作用）
        if (enableAutoLagCorrection && isPrepared && videoPlayer.isPlaying)
        {
            // ループ検出（バックアップ: Eventが飛んでこなかった場合用）
            double currentVideoTime = GetVideoTime();
            if (currentVideoTime < lastVideoTime - 1.0)
            {
                 Debug.LogWarning($"[VideoManager] Loop detected (Time jump). Resetting timing.");
                 SyncGameTime();
                 return;
            }

            // 頻繁にチェックするが、補正は一定間隔を空ける
            double lag = GetLag();
            
            if (System.Math.Abs(lag) > lagCorrectionThreshold)
            {
                // 前回の補正から一定時間経過している場合のみ補正
                if (Time.time - lastLagCorrectionTime >= lagCorrectionInterval)
                {
                    Debug.LogWarning($"[VideoManager] Auto lag correction: lag={lag:F6}s exceeds threshold={lagCorrectionThreshold:F3}s");
                    ResetRag();
                    lastLagCorrectionTime = Time.time; // 補正実行時刻を更新
                    videoTimeStableFrames = 0;
                }
            }
        }
        
        // 緊急ラグチェック（100ms以上の大きなラグのみ）
        if (isPrepared && videoPlayer.isPlaying)
        {
            double currentVideoTime = GetVideoTime();
            double videoTimeDelta = System.Math.Abs(currentVideoTime - lastVideoTime);
            
            const double MIN_NORMAL_DELTA = 0.01;  // 10ms以上
            const double MAX_NORMAL_DELTA = 0.06;  // 60ms以下
            
            bool isVideoTimeMoving = videoTimeDelta >= MIN_NORMAL_DELTA && videoTimeDelta <= MAX_NORMAL_DELTA;
            bool isVideoTimeJumped = videoTimeDelta > VIDEO_TIME_JUMP_THRESHOLD;
            bool isVideoTimeStuck = videoTimeDelta < MIN_NORMAL_DELTA;
            
            // 安定性カウンター更新
            if (isVideoTimeJumped)
            {
                Debug.LogWarning($"[VideoManager] VideoTime JUMP: {lastVideoTime:F6}s -> {currentVideoTime:F6}s (delta={videoTimeDelta:F6}s)");
                videoTimeStableFrames = 0;
            }
            else if (isVideoTimeMoving)
            {
                if (videoTimeStableFrames < REQUIRED_STABLE_FRAMES)
                {
                    videoTimeStableFrames++;
                }
            }
            else if (isVideoTimeStuck)
            {
                if (videoTimeStableFrames > 0)
                {
                    Debug.LogWarning($"[VideoManager] VideoTime stuck: delta={videoTimeDelta:F6}s");
                }
                videoTimeStableFrames = 0;
            }
            
            lastVideoTime = currentVideoTime;
            
            bool isStable = videoTimeStableFrames >= REQUIRED_STABLE_FRAMES;
            double lag = GetLag();
            
            // デバッグログ（100ms以上のラグのみ）
            if (System.Math.Abs(lag) > 0.1)
            {
                string status = isStable ? "[STABLE]" : "[UNSTABLE]";
                Debug.Log($"[VideoManager] {status} Lag={lag:F6}s | Video={currentVideoTime:F6}s | MusicRaw={GetMusicTimeRaw():F6}s | timeOffset={timeOffset:F6}s");
            }
            
            // 緊急補正（100ms以上のラグ、かつVideoTime安定時）
            if (enableAutoLagCorrection && System.Math.Abs(lag) > 0.1 && isStable)
            {
                Debug.LogWarning($"[VideoManager] EMERGENCY lag correction: lag={lag:F6}s exceeds 100ms");
                ResetRag();
                videoTimeStableFrames = 0;
            }
        }
    }
    
    /// <summary>
    /// 各種時刻の比較デバッグログ出力
    /// </summary>
    private void LogTimeComparison()
    {
        // 各種時刻を取得
        double dspTime = AudioSettings.dspTime;
        double videoPlayerTime = videoPlayer.time;
        double musicTime = GetMusicTime(); // dspTime - gameStartTime
        float unityTime = Time.time;
        float realtimeSinceStartup = Time.realtimeSinceStartup;
        double audioSourceTime = audioSource != null ? audioSource.time : 0.0;
        
        // ラグ計算
        double lagVideoVsMusic = musicTime - videoPlayerTime;
        double lagAudioSourceVsMusic = audioSource != null ? (musicTime - audioSourceTime) : 0.0;
        
        // フレーム情報
        int frameCount = Time.frameCount;
        float deltaTime = Time.deltaTime;
        
        // ビデオプレイヤー状態
        bool isPlaying = videoPlayer.isPlaying;
        bool isPaused = videoPlayer.isPaused;
        double videoLength = videoPlayer.length;
        long videoFrameCount = videoPlayer.frame;
        float playbackSpeed = videoPlayer.playbackSpeed;
        
        Debug.Log($"[TimeComparison] ========================================");
        Debug.Log($"[TimeComparison] Frame={frameCount}, deltaTime={deltaTime:F6}s");
        Debug.Log($"[TimeComparison] --- Unity Time ---");
        Debug.Log($"[TimeComparison] Time.time={unityTime:F6}s");
        Debug.Log($"[TimeComparison] Time.realtimeSinceStartup={realtimeSinceStartup:F6}s");
        Debug.Log($"[TimeComparison] --- Audio Time ---");
        Debug.Log($"[TimeComparison] AudioSettings.dspTime={dspTime:F6}s");
        Debug.Log($"[TimeComparison] MusicTime(dsp-start)={musicTime:F6}s");
        Debug.Log($"[TimeComparison] AudioSource.time={audioSourceTime:F6}s");
        Debug.Log($"[TimeComparison] --- Video Time ---");
        Debug.Log($"[TimeComparison] VideoPlayer.time={videoPlayerTime:F6}s");
        Debug.Log($"[TimeComparison] VideoPlayer.frame={videoFrameCount}");
        Debug.Log($"[TimeComparison] VideoPlayer.length={videoLength:F2}s");
        Debug.Log($"[TimeComparison] VideoPlayer.isPlaying={isPlaying}, isPaused={isPaused}");
        Debug.Log($"[TimeComparison] VideoPlayer.playbackSpeed={playbackSpeed}");
        Debug.Log($"[TimeComparison] --- Lag Analysis ---");
        Debug.Log($"[TimeComparison] Lag(Music-Video)={lagVideoVsMusic:F6}s ({lagVideoVsMusic * 1000:F2}ms)");
        Debug.Log($"[TimeComparison] Lag(Music-AudioSource)={lagAudioSourceVsMusic:F6}s ({lagAudioSourceVsMusic * 1000:F2}ms)");
        Debug.Log($"[TimeComparison] gameStartTime={gameStartTime:F6}s");
        Debug.Log($"[TimeComparison] ========================================");
    }
}
