using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ========================================
/// AudioManager（新アーキテクチャ版）
/// ========================================
/// 
/// 責務：効果音の管理・再生
/// - ゲーム開始時に AudioClip をすべてキャッシュ（Dictionary<string, AudioClip>）
/// - Resources フォルダから AudioClip をロード・キャッシング
/// - Phase*ShakeHandler から PlaySFX("hit") で呼ばれる
/// 
/// 最適化：
/// - 初回ロード時にキャッシング（GC 削減）
/// - AudioSource.PlayOneShot() で再生
/// 
/// 参照元：Assets/Scripts/FormerCodes/Core/GameManager.cs の PlayBurstSound() ロジック
/// 
/// ========================================
/// </summary>
public class AudioManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
            }
            return _instance;
        }
    }
    
    // AudioClip キャッシュ
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
    
    // AudioSource（事前生成で遅延回避）
    private AudioSource _audioSource;
    
    private void Awake()
    {
        // シングルトン設定
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // AudioSource の初期化
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log("[AudioManager] Initialized");
        
        // AudioClip のプリロード
        PreloadAudioClips();
    }
    
    /// <summary>
    /// AudioClip のプリロード（Resources/Audio/ から読み込み）
    /// </summary>
    private void PreloadAudioClips()
    {
        // Resources/Audio/ フォルダから AudioClip を読み込み
        // 例：Resources/Audio/hit.wav → "hit"
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        
        foreach (var clip in clips)
        {
            if (clip != null)
            {
                _audioClips[clip.name] = clip;
                
                if (GameConstants.DEBUG_MODE)
                    Debug.Log($"[AudioManager] Loaded audio clip: {clip.name}");
            }
        }
        
        if (GameConstants.DEBUG_MODE)
            Debug.Log($"[AudioManager] Preloaded {_audioClips.Count} audio clips");
    }
    
    /// <summary>
    /// 効果音を再生
    /// </summary>
    public void PlaySFX(string clipName)
    {
        if (_audioSource == null)
        {
            Debug.LogWarning("[AudioManager] AudioSource is not initialized!");
            return;
        }
        
        // キャッシュから AudioClip を取得
        if (_audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            _audioSource.PlayOneShot(clip, 0.7f);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log($"[AudioManager] 🔊 Playing SFX: {clipName}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] AudioClip not found: {clipName}");
        }
    }
    
    /// <summary>
    /// AudioClip を取得（キャッシュから）
    /// </summary>
    public AudioClip GetClip(string clipName)
    {
        if (_audioClips.TryGetValue(clipName, out AudioClip clip))
        {
            return clip;
        }
        
        Debug.LogWarning($"[AudioManager] AudioClip not found: {clipName}");
        return null;
    }
    
    /// <summary>
    /// 音量設定（オプション）
    /// </summary>
    public void SetVolume(float volume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = Mathf.Clamp01(volume);
        }
    }
}