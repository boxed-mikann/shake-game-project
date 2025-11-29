using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ノーツ生成システム - 譜面データに基づいてノーツを生成
/// </summary>
public class NoteSpawnerV2 : MonoBehaviour
{
    public static NoteSpawnerV2 Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnAheadTime = 2f; // ノーツを何秒前に生成するか
    [SerializeField] private Vector3 spawnStartPosition = new Vector3(0, 5, 0);
    [SerializeField] private Vector3 judgeLinePosition = new Vector3(0, -3, 0);
    
    [Header("Note Sprites")]
    [SerializeField] private Sprite[] deviceNoteSprites; // 10種類（デバイスID 0-9用）
    
    private ChartData currentChart;
    private int currentNoteIndex = 0;
    private bool isSpawning = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    
    void OnEnable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart += OnGameStart;
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
        }
    }
    
    void OnDisable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    
    private void OnGameStart()
    {
        // 譜面を読み込み
        if (ChartLoader.Instance != null)
        {
            currentChart = ChartLoader.Instance.GetCurrentChart();
            if (currentChart == null)
            {
                Debug.LogWarning("[NoteSpawnerV2] 譜面が読み込まれていません");
                return;
            }
        }
        
        currentNoteIndex = 0;
        isSpawning = true;
    }
    
    private void OnGameEnd()
    {
        isSpawning = false;
        currentNoteIndex = 0;
        
        // 画面上のノーツをクリア
        if (NotePoolV2.Instance != null)
        {
            NotePoolV2.Instance.ClearAllNotes();
        }
    }
    
    void Update()
    {
        if (!isSpawning || currentChart == null) return;
        if (!GameManagerV2.Instance.IsGameStarted) return;
        
        float currentMusicTime = GameManagerV2.Instance.GetMusicTime();
        
        // 生成タイミングに達したノーツをスポーン
        while (currentNoteIndex < currentChart.notes.Count)
        {
            var noteData = currentChart.notes[currentNoteIndex];
            float spawnTime = noteData.time - spawnAheadTime;
            
            if (currentMusicTime >= spawnTime)
            {
                SpawnNote(noteData);
                currentNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }
    
    private void SpawnNote(ChartData.NoteData noteData)
    {
        if (NotePoolV2.Instance == null) return;
        
        GameObject noteObj = NotePoolV2.Instance.GetNote();
        if (noteObj == null) return;
        
        NoteV2 note = noteObj.GetComponent<NoteV2>();
        if (note == null) return;
        
        // デバイスIDを文字列に変換
        string deviceId = noteData.block.ToString();
        
        // ノーツスプライトを取得
        Sprite noteSprite = null;
        if (deviceNoteSprites != null && noteData.block >= 0 && noteData.block < deviceNoteSprites.Length)
        {
            noteSprite = deviceNoteSprites[noteData.block];
        }
        
        // ターゲット位置を計算（各デバイスアイコンの位置に流下）
        Vector3 targetPos = CalculateTargetPosition(deviceId);
        
        note.Initialize(deviceId, noteData.time, spawnStartPosition, targetPos, noteSprite);
    }
    
    private Vector3 CalculateTargetPosition(string deviceId)
    {
        // TODO: DeviceIconManagerから各アイコンの位置を取得
        // 現在は固定位置を返す
        return judgeLinePosition;
    }
}
