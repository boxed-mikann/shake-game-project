using UnityEngine;

[CreateAssetMenu(fileName = "NewSongData", menuName = "ShakeGame/SongData")]
public class SongData : ScriptableObject
{
    public string title;
    public string artist;
    public Sprite jacketImage;
    
    [Tooltip("譜面JSONファイル")]
    public TextAsset chartJson;
    
    [Tooltip("動画ファイルのパス (StreamingAssetsからの相対パス)")]
    public string videoPath;
    
    [Tooltip("プレビュー用オーディオ（あれば）")]
    public AudioClip previewAudio;
    
    [Tooltip("難易度表示など")]
    public int difficultyLevel = 1;
}
