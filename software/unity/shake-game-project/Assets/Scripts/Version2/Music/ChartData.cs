using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 譜面データ構造 - NoteEditor JSON形式に対応
/// </summary>
[Serializable]
public class ChartData
{
    public string name;
    public int BPM;
    public int offset;
    public List<NoteData> notes;
    
    [Serializable]
    public class NoteData
    {
        public int block;       // レーン番号（デバイスID: 0-9、または特殊レーン）
        public int num;         // ノーツ番号
        public int LPB;         // Lines Per Beat（1小節あたりの分割数）
        public int type;        // ノーツタイプ（0: 通常、1: 長押し開始、2: 長押し終了など）
        
        // 計算された時刻（秒）
        [NonSerialized]
        public float time;
    }
    
    /// <summary>
    /// ノーツ時刻を計算
    /// </summary>
    public void CalculateNoteTimes()
    {
        if (notes == null) return;
        
        float beatDuration = 60f / BPM; // 1拍の長さ（秒）
        
        foreach (var note in notes)
        {
            // num と LPB から小節数と拍数を計算
            int measuresFromStart = note.num / note.LPB;
            int beatsInMeasure = note.num % note.LPB;
            
            // 時刻計算（オフセット含む）
            float noteTime = (measuresFromStart * 4 + (float)beatsInMeasure / note.LPB * 4) * beatDuration;
            note.time = noteTime + offset / 1000f; // offsetはミリ秒
        }
        
        // 時刻順にソート
        notes.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
