using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 譜面データ構造 - NoteEditor JSON形式に対応
/// JSON読み込み → ゲーム用データに変換
/// </summary>
[Serializable]
public class ChartDataV2
{
    public string formatVersion; // JSONのformatVersion
    public int beat; // JSONのbeat (BPMのこと？)
    
    public string name;
    public int maxBlock;
    public int BPM;
    public int offset; // サンプル数 (44100Hz想定)
    public List<NoteJson> notes;

    // JSONから読み込んだ生データ
    [Serializable]
    public class NoteJson
    {
        public int LPB;
        public int num;
        public int block;
        public int type; // 1:単ノート, 2:長押し開始
        public List<ChildNoteJson> notes; // 長押しの終点
    }

    [Serializable]
    public class ChildNoteJson
    {
        public int LPB;
        public int num;
        public int block;
        public int type;
        // public List<ChildNoteJson> notes; // 再帰構造を削除してシリアライズエラーを回避
    }

    // ゲーム用に展開したデータ
    [NonSerialized]
    public List<GameNote> gameNotes = new List<GameNote>();

    [Serializable]
    public class GameNote
    {
        public int deviceId; // block
        public double time; // 秒
        public NoteType noteType;
        public double duration; // 長押しの場合の長さ（秒）
        
        public GameNote(int deviceId, double time, NoteType type, double duration = 0)
        {
            this.deviceId = deviceId;
            this.time = time;
            this.noteType = type;
            this.duration = duration;
        }
    }

    public enum NoteType
    {
        Single,    // 単ノート - 通常の判定
        LongStart, // 長押し開始（表示用、判定はしない）
        LongEnd    // 長押し終了（判定する）
    }

    /// <summary>
    /// JSONデータをゲーム用データに変換
    /// </summary>
    public void ConvertToGameNotes()
    {
        gameNotes.Clear();
        if (notes == null) return;
        
        // BPMの補正
        if (BPM == 0 && beat > 0) BPM = beat;
        if (BPM == 0) BPM = 120; // デフォルト

        double beatDuration = 60.0 / BPM; // 1拍の長さ（秒）

        foreach (var note in notes)
        {
            double noteTime = CalculateTime(note, beatDuration);

            if (note.type == 1) // 単ノート
            {
                gameNotes.Add(new GameNote(note.block, noteTime, NoteType.Single));
            }
            else if (note.type == 2 && note.notes != null && note.notes.Count > 0) // 長押し
            {
                var endNote = note.notes[0];
                double endTime = CalculateTime(endNote, beatDuration);
                double duration = endTime - noteTime;

                // 長押し開始（見た目用）
                gameNotes.Add(new GameNote(note.block, noteTime, NoteType.LongStart, duration));
                // 長押し終了（判定用）
                gameNotes.Add(new GameNote(note.block, endTime, NoteType.LongEnd, duration));
                
                Debug.Log($"[ChartDataV2] 長押しノート追加: block={note.block}, start={noteTime:F6}, end={endTime:F6}, duration={duration:F6}");
            }
            else if (note.type == 2)
            {
                Debug.LogWarning($"[ChartDataV2] type=2だがnotesが空: block={note.block}, num={note.num}, notes.Count={note.notes?.Count ?? 0}");
            }
        }

        // 時刻順にソート
        gameNotes.Sort((a, b) => a.time.CompareTo(b.time));
        
        Debug.Log($"[ChartDataV2] 変換完了: {gameNotes.Count}ノーツ（BPM={BPM}, offset={(double)offset/44100.0:F6}秒）");
        if (gameNotes.Count > 0)
        {
            Debug.Log($"[ChartDataV2] 最初のノート: time={gameNotes[0].time:F6}秒, deviceId={gameNotes[0].deviceId}, type={gameNotes[0].noteType}");
        }
    }

    /// <summary>
    /// ノーツの時刻を計算
    /// </summary>
    private double CalculateTime(NoteJson note, double beatDuration)
    {
        // num / LPB = 拍数
        // LPBがない場合は480(標準的な分解能)と仮定
        int lpb = note.LPB > 0 ? note.LPB : 480;
        
        double beats = (double)note.num / lpb;
        // offsetはサンプル数 (44100Hz想定)
        return beats * beatDuration + (double)offset / 44100.0;
    }

    private double CalculateTime(ChildNoteJson note, double beatDuration)
    {
        int lpb = note.LPB > 0 ? note.LPB : 480;
        double beats = (double)note.num / lpb;
        return beats * beatDuration + (double)offset / 44100.0;
    }
}
