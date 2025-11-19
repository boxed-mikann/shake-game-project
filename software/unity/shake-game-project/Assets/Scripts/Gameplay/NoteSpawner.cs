using UnityEngine;
using System.Collections;

/// <summary>
/// ========================================
/// NoteSpawner（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：フェーズに応じた時間ベース音符生成
/// - PhaseManager.OnPhaseChanged を購読
/// - 各フェーズの spawnFrequency に基づいて定期生成
/// - LastSprintPhase では生成速度が既に調整済み（PhaseManager で計算）
/// 
/// Coroutine による定期スポーン：
/// - yield return new WaitForSeconds(frequency) で定期生成
/// - フェーズ変更時に前の Coroutine を停止
/// 
/// 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の UpdateNoteSpawning() + SpawnNote()
/// 
/// ========================================
/// </summary>
public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnContainer;         // 音符の親オブジェクト
    [SerializeField] private Vector2 spawnRangeX = new Vector2(-6f, 6f);    // X座標の範囲
    [SerializeField] private Vector2 spawnRangeY = new Vector2(-4f, 4f);    // Y座標の範囲
    
    private Coroutine _spawnCoroutine = null;
    
    // シングルトンインスタンス
    private static NoteSpawner _instance;
    public static NoteSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NoteSpawner>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // spawnContainer の確認
        if (spawnContainer == null)
        {
            GameObject container = new GameObject("NotesContainer");
            spawnContainer = container.transform;
            spawnContainer.SetParent(transform);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NoteSpawner] Initialized");
    }
    
    private void OnEnable()
    {
        // PhaseManager.OnPhaseChanged を購読
        PhaseManager.OnPhaseChanged.AddListener(OnPhaseChanged);
        GameManager.OnShowTitle.AddListener(StopSpawning);
    }
    
    private void OnDisable()
    {
        // イベント購読解除
        PhaseManager.OnPhaseChanged.RemoveListener(OnPhaseChanged);
        GameManager.OnShowTitle.RemoveListener(StopSpawning);
    }
    
    /// <summary>
    /// スポーンを停止（タイトル復帰時）
    /// </summary>
    private void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NoteSpawner] Spawning stopped");
    }
    
    /// <summary>
    /// フェーズ変更イベントハンドラ
    /// </summary>
    private void OnPhaseChanged(PhaseChangeData phaseData)
    {
        // 前のフェーズの Coroutine を停止
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteSpawner] Phase changed: {phaseData.phaseType}, Frequency: {phaseData.spawnFrequency}s");
        
        // 新しいフェーズのスポーンループ開始
        _spawnCoroutine = StartCoroutine(SpawnLoop(phaseData.spawnFrequency, phaseData.duration));
    }
    
    /// <summary>
    /// スポーンループ（Coroutine）
    /// </summary>
    private IEnumerator SpawnLoop(float frequency, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // 生成数上限チェック
            if (NoteManager.Instance != null && 
                NoteManager.Instance.GetActiveNoteCount() >= GameConstants.MAX_NOTE_COUNT)
            {
                if (GameConstants.DEBUG_MODE)
                    Debug.Log("[NoteSpawner] Max note count reached, skipping spawn");
                
                yield return new WaitForSeconds(frequency);
                elapsed += frequency;
                continue;
            }
            
            // 音符を1個生成
            SpawnOneNote();
            
            // 次のスポーンまで待機
            yield return new WaitForSeconds(frequency);
            elapsed += frequency;
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[NoteSpawner] Spawn loop completed");
    }
    
    /// <summary>
    /// 音符を1個生成
    /// </summary>
    private void SpawnOneNote()
    {
        // NotePool から取得
        if (NotePool.Instance == null)
        {
            Debug.LogError("[NoteSpawner] NotePool instance not found!");
            return;
        }
        
        Note note = NotePool.Instance.GetNote();
        
        if (note == null)
        {
            Debug.LogError("[NoteSpawner] Failed to get note from pool!");
            return;
        }
        
        // 親オブジェクトを設定
        note.transform.SetParent(spawnContainer);
        
        // ランダムな位置に配置
        Vector3 randomPos = new Vector3(
            Random.Range(spawnRangeX.x, spawnRangeX.y),
            Random.Range(spawnRangeY.x, spawnRangeY.y),
            0f
        );
        note.transform.position = randomPos;
        
        // ランダムな回転（±30度）
        float randomRotation = Random.Range(-GameConstants.NOTE_ROTATION_MAX, GameConstants.NOTE_ROTATION_MAX);
        note.transform.rotation = Quaternion.Euler(0f, 0f, randomRotation);
        
        // ランダムカラー設定
        SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = GetRandomColor();
        }
        
        // NoteManager に登録
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.AddNote(note);
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[NoteSpawner] 🎵 Note spawned at {randomPos}, rotation: {randomRotation}°");
    }
    
    /// <summary>
    /// ランダムカラーを取得
    /// </summary>
    private Color GetRandomColor()
    {
        Color[] colors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f),     // Orange
            new Color(0.5f, 0f, 0.5f)    // Purple
        };
        
        return colors[Random.Range(0, colors.Length)];
    }
}