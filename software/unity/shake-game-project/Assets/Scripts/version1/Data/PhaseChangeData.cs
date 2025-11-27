using UnityEngine;

/// <summary>
/// フェーズ変更時に発行されるイベントの引数
/// PhaseManagerからOnPhaseChangedイベントと共に発行される
/// </summary>
[System.Serializable]
public struct PhaseChangeData
{
    /// <summary>現在のフェーズタイプ（NotePhase, RestPhase, LastSprintPhase）</summary>
    public Phase phaseType;
    
    /// <summary>このフェーズの継続時間（秒）</summary>
    public float duration;
    
    /// <summary>音符湧き出し頻度（秒間隔）</summary>
    public float spawnFrequency;
    
    /// <summary>フェーズ番号（0, 1, 2...）</summary>
    public int phaseIndex;
    
    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"Phase {phaseIndex}: {phaseType} (Duration: {duration}s, SpawnFreq: {spawnFrequency}s)";
    }
}

/// <summary>
/// フェーズ種別
/// </summary>
public enum Phase
{
    NotePhase,
    RestPhase,
    LastSprintPhase
}