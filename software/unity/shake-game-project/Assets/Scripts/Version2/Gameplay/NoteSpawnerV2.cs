using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ノーツ生成システム - 譜面データからノーツを生成
/// 中心からデバイスアイコンに向かってノーツを流す
/// </summary>
public class NoteSpawnerV2 : MonoBehaviour
{
    public static NoteSpawnerV2 Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [SerializeField] private double spawnAheadTime = 2.0; // ノーツを何秒前に生成するか
    [SerializeField] private Transform centerPoint; // 中心位置（Empty Object）
    [SerializeField] private Transform[] deviceIcons; // デバイスアイコン位置（10個）
    
    [Header("Note Sprites")]
    [SerializeField] private Sprite[] deviceNoteSprites; // 10種類（デバイスID 0-9用）
    
    private ChartDataV2 currentChart;
    private int currentNoteIndex = 0;
    private bool isSpawning = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // ゲーム開始イベントを購読
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart += StartSpawning;
            GameManagerV2.Instance.OnGameEnd += StopSpawning;
        }
    }
    
    void OnDestroy()
    {
        // イベント購読解除
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= StartSpawning;
            GameManagerV2.Instance.OnGameEnd -= StopSpawning;
        }
    }
    
    void Update()
    {
        if (!isSpawning || currentChart == null) return;
        if (GameManagerV2.Instance == null || !GameManagerV2.Instance.IsGameStarted) return;// TODO GamaManagerV2の状態のやつを参照するように変更
        
        double currentMusicTime = GameManagerV2.Instance.GetMusicTime();
        
        // スポーン判定
        while (currentNoteIndex < currentChart.gameNotes.Count)
        {
            var note = currentChart.gameNotes[currentNoteIndex];
            double spawnTime = note.time - spawnAheadTime;
            
            if (currentMusicTime >= spawnTime)
            {
                Debug.Log($"[NoteSpawnerV2] Spawning note: index={currentNoteIndex}, time={note.time:F6}s, deviceId={note.deviceId}, type={note.noteType}, currentTime={currentMusicTime:F6}s");
                SpawnNote(note);
                currentNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// ノーツを生成
    /// </summary>
    private void SpawnNote(ChartDataV2.GameNote note)
    {
        // centerPointが画面外にある場合は(0,0,0)を使用
        Vector3 centerPos = Vector3.zero;
        if (centerPoint != null)
        {
            centerPos = centerPoint.position;
        }
        
        Vector3 iconPos = GetDeviceIconPosition(note.deviceId);
        
        Debug.Log($"[NoteSpawnerV2] ノート生成: deviceId={note.deviceId}, time={note.time:F6}, type={note.noteType}, centerPos={centerPos}, iconPos={iconPos}");
        
        // LongStartの場合は複数のビジュアルノートを生成
        if (note.noteType == ChartDataV2.NoteType.LongStart)
        {
            SpawnLongNoteVisuals(note, centerPos, iconPos);
        }
        else
        {
            // 単ノート・LongEndを生成
            SpawnSingleNote(note, centerPos, iconPos);
        }
    }
    
    /// <summary>
    /// 単ノートを生成
    /// </summary>
    private void SpawnSingleNote(ChartDataV2.GameNote note, Vector3 centerPos, Vector3 iconPos)
    {
        if (NotePoolV2.Instance == null)
        {
            Debug.LogError("[NoteSpawnerV2] NotePoolV2が見つかりません");
            return;
        }
        
        GameObject noteObj = NotePoolV2.Instance.GetNote();
        if (noteObj == null) return;
        
        NoteObjectV2 noteComponent = noteObj.GetComponent<NoteObjectV2>();
        if (noteComponent == null)
        {
            Debug.LogError("[NoteSpawnerV2] NoteObjectV2コンポーネントが見つかりません");
            return;
        }
        
        // スプライトを取得
        Sprite sprite = GetSpriteForDevice(note.deviceId);
        
        noteComponent.Initialize(
            note.deviceId,
            centerPos,
            iconPos,
            note.time,
            note.noteType,
            sprite
        );
    }
    
    /// <summary>
    /// 長押しノートのビジュアル（複数の通常ノート）を生成
    /// </summary>
    private void SpawnLongNoteVisuals(ChartDataV2.GameNote longStartNote, Vector3 centerPos, Vector3 iconPos)
    {
        const double visualNoteInterval = 0.1; // 0.1秒ごとにノートを生成
        double endTime = longStartNote.time + longStartNote.duration;
        
        double currentTime = longStartNote.time;
        int noteCount = 0;
        
        while (currentTime <= endTime)
        {
            // 各ノートを通常ノートとして生成
            if (NotePoolV2.Instance == null) return;
            
            GameObject noteObj = NotePoolV2.Instance.GetNote();
            if (noteObj == null) break;
            
            NoteObjectV2 noteComponent = noteObj.GetComponent<NoteObjectV2>();
            if (noteComponent == null)
            {
                Debug.LogError("[NoteSpawnerV2] NoteObjectV2コンポーネントが見つかりません");
                break;
            }
            
            Sprite sprite = GetSpriteForDevice(longStartNote.deviceId);
            
            // 各ノートはSingleタイプとして初期化（判定はされない）
            noteComponent.Initialize(
                longStartNote.deviceId,
                centerPos,
                iconPos,
                currentTime,
                ChartDataV2.NoteType.Single,
                sprite
            );
            
            currentTime += visualNoteInterval;
            noteCount++;
        }
        
        Debug.Log($"[NoteSpawnerV2] LongNoteビジュアル生成: deviceId={longStartNote.deviceId}, start={longStartNote.time:F6}, end={endTime:F6}, count={noteCount}");
    }
    
    /// <summary>
    /// デバイスIDに対応するアイコン位置を取得
    /// </summary>
    private Vector3 GetDeviceIconPosition(int deviceId)
    {
        if (deviceIcons != null && deviceId >= 0 && deviceId < deviceIcons.Length && deviceIcons[deviceId] != null)
        {
            return deviceIcons[deviceId].position;
        }
        
        Debug.LogWarning($"[NoteSpawnerV2] デバイス{deviceId}のアイコンが未設定");
        return Vector3.zero;
    }
    
    /// <summary>
    /// デバイスIDに対応するスプライトを取得
    /// </summary>
    private Sprite GetSpriteForDevice(int deviceId)
    {
        if (deviceNoteSprites != null && deviceId >= 0 && deviceId < deviceNoteSprites.Length)
        {
            return deviceNoteSprites[deviceId];
        }
        return null;
    }
    
    /// <summary>
    /// スポーンを開始
    /// </summary>
    public void StartSpawning()
    {
        // 譜面データを取得（ゲーム開始時に取得するように変更）
        if (ChartManagerV2.Instance != null)
        {
            currentChart = ChartManagerV2.Instance.GetChart();
            if (currentChart == null)
            {
                Debug.LogError("[NoteSpawnerV2] 譜面データが取得できません");
                return;
            }
        }
        else
        {
            Debug.LogError("[NoteSpawnerV2] ChartManagerV2が見つかりません");
            return;
        }
        
        isSpawning = true;
        currentNoteIndex = 0;
        Debug.Log($"[NoteSpawnerV2] ノーツ生成開始 - 総ノート数: {currentChart.gameNotes.Count}");
        
        // 初期状態をログ出力
        if (centerPoint == null) Debug.LogError("[NoteSpawnerV2] centerPoint が未設定です！");
        if (deviceIcons == null || deviceIcons.Length == 0) Debug.LogError("[NoteSpawnerV2] deviceIcons が未設定です！");
        if (NotePoolV2.Instance == null) Debug.LogError("[NoteSpawnerV2] NotePoolV2 が見つかりません！");
    }
    
    /// <summary>
    /// スポーンを停止
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("[NoteSpawnerV2] ノーツ生成停止");
    }
    
    /// <summary>
    /// スポーン設定をリセット
    /// </summary>
    public void ResetSpawner()
    {
        currentNoteIndex = 0;
        isSpawning = false;
    }
    
    // Getter
    public double GetSpawnAheadTime() => spawnAheadTime;
}
