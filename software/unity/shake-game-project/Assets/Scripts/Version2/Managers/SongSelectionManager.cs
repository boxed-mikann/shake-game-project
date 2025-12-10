using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SongSelectionManager : MonoBehaviour
{
    public static SongSelectionManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private List<SongData> availableSongs;
    
    [Header("Settings")]
    [SerializeField] private float shakeThresholdForDecision = 50f; // 決定に必要な総シェイク数
    [SerializeField] private float selectionChangeInterval = 5.0f; // 自動で曲が切り替わる間隔

    private int currentIndex = 0;
    private float currentShakeEnergy = 0f;
    private float autoRotateTimer = 0f;
    private bool isCustomMode = false;

    // イベント
    public System.Action<SongData> OnSongChanged;
    public System.Action<float> OnDecisionProgress; // 0.0f - 1.0f

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnIdleStart += OnIdleStart;
        }
    }

    private void OnDisable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnIdleStart -= OnIdleStart;
        }
    }

    private void OnIdleStart()
    {
        // Idleに戻ったらカスタムモードを解除して通常選曲に戻る
        isCustomMode = false;
        currentShakeEnergy = 0f;
        OnDecisionProgress?.Invoke(0f);
    }

    private void Start()
    {
        // 初期化時に最初の曲を通知
        if (availableSongs != null && availableSongs.Count > 0)
        {
            // 少し遅らせてUIの準備完了を待つ（必要であれば）
            // 今回はStart同士なので、実行順序によってはUI側が購読前に呼ばれる可能性がある
            // そのため、UI側でStart時に現在の選択を取得するか、ここで少し待つか、
            // あるいはUIのStartで購読した直後に手動で更新を呼ぶのが良いが、
            // ここではシンプルに初期値をセットしておく。
            OnSongChanged?.Invoke(availableSongs[currentIndex]);
            
            // ゲージの初期化通知
            OnDecisionProgress?.Invoke(0f);
        }
    }

    private void Update()
    {
        // IdleStarting以外では処理しない
        if (GameManagerV2.Instance == null || GameManagerV2.Instance.CurrentGameState != GameManagerV2.GameState.IdleStarting)
            return;

        if (isCustomMode) return;

        // 自動回転ロジック（ぐるぐる回る）
        autoRotateTimer += Time.deltaTime;
        if (autoRotateTimer >= selectionChangeInterval)
        {
            NextSong();
            autoRotateTimer = 0f;
            // currentShakeEnergy = 0f; // NextSong内でリセットするのでここは不要
            // OnDecisionProgress?.Invoke(0f);
        }
    }

    public void NextSong()
    {
        if (availableSongs == null || availableSongs.Count == 0) return;

        currentIndex = (currentIndex + 1) % availableSongs.Count;
        
        // 曲が変わったらシェイクエネルギーをリセット
        currentShakeEnergy = 0f;
        OnDecisionProgress?.Invoke(0f);
        
        OnSongChanged?.Invoke(availableSongs[currentIndex]);
    }

    public void PreviousSong()
    {
        if (availableSongs == null || availableSongs.Count == 0) return;

        currentIndex = (currentIndex - 1 + availableSongs.Count) % availableSongs.Count;
        
        // 曲が変わったらシェイクエネルギーをリセット
        currentShakeEnergy = 0f;
        OnDecisionProgress?.Invoke(0f);
        
        OnSongChanged?.Invoke(availableSongs[currentIndex]);
    }

    /// <summary>
    /// シェイク入力があったときに呼ばれる（IdleStartingHandlerから呼ぶ）
    /// </summary>
    public void AddShakeEnergy(float amount = 1.0f)
    {
        if (isCustomMode) return;
        if (availableSongs == null || availableSongs.Count == 0) return;

        currentShakeEnergy += amount;
        
        // 決定進捗を通知
        float progress = Mathf.Clamp01(currentShakeEnergy / shakeThresholdForDecision);
        OnDecisionProgress?.Invoke(progress);

        // 閾値を超えたら決定
        if (currentShakeEnergy >= shakeThresholdForDecision)
        {
            DecideSong();
        }
        
        // シェイクされたら回転タイマーを少しリセット（選んでいる間は勝手に変わらないように）
        autoRotateTimer = Mathf.Max(0f, autoRotateTimer - 0.5f);
    }

    public void DecideSongForce()
    {
        DecideSong();
    }

    private void DecideSong()
    {
        if (availableSongs == null || availableSongs.Count == 0) return;
        SongData selected = availableSongs[currentIndex];
        StartGameWithSong(selected);
    }

    /// <summary>
    /// 選択された曲でゲームを開始する
    /// </summary>
    public void StartGameWithSong(SongData song)
    {
        Debug.Log($"[SongSelectionManager] Selected: {song.title}");

        // 1. 譜面ロード
        if (ChartManagerV2.Instance != null)
        {
            ChartManagerV2.Instance.LoadSong(song);
        }

        // 2. 動画パス設定（LoadVideoではなくパスを設定し、GameStart時にVideoManagerにロードさせる）
        if (VideoManager.Instance != null)
        {
            // nullチェックを追加
            string path = song.videoPath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[SongSelectionManager] Video path for {song.title} is empty.");
                path = ""; // 空文字にしておく（VideoManager側でエラーになるかもしれないが、前の値を保持するよりマシ）
            }
            VideoManager.Instance.SetGameVideoPath(path); 
        }

        // 3. ゲーム開始
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.StartGame();
        }
    }

    /// <summary>
    /// デバッグ用：カスタムJSONで開始
    /// </summary>
    public void StartCustomGame(string jsonContent, string videoPath)
    {
        isCustomMode = true;
        
        bool loadSuccess = false;
        if (ChartManagerV2.Instance != null)
        {
            loadSuccess = ChartManagerV2.Instance.LoadChartFromJson(jsonContent);
        }
        
        if (!loadSuccess)
        {
            Debug.LogError("[SongSelectionManager] Failed to load custom chart. Game start aborted.");
            isCustomMode = false;
            return;
        }
        
        // 動画パスが空ならデフォルト維持、指定があれば設定
        // 修正: デバッグモードで動画パスが空の場合、前の曲の動画が残らないように空文字を設定する
        if (VideoManager.Instance != null)
        {
            // デバッグモードでは、指定されたパスをそのまま使う（空なら空）
            // これにより、前の曲の動画が残るのを防ぐ
            VideoManager.Instance.SetGameVideoPath(videoPath);
            
            if (string.IsNullOrEmpty(videoPath))
            {
                Debug.LogWarning("[SongSelectionManager] Custom game started with empty video path. Video will not play.");
            }
        }

        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.StartGame();
        }
    }
}
