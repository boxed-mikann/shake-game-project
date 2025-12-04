using UnityEngine;

/// <summary>
/// ========================================
/// SEManager（Version2）
/// ========================================
/// 
/// 責務：効果音の管理・再生
/// - AudioClipをインスペクターでアタッチして事前キャッシュ
/// - AudioSource.PlayOneShot()で低遅延再生
/// - シングルトンパターンで全体からアクセス可能
/// 
/// 最適化：
/// - 事前にAudioSourceを生成してGC削減
/// - キャッシュ済みのAudioClipを使用
/// 
/// 参照元：Version1のAudioManager
/// ========================================
/// </summary>
public class SEManager : MonoBehaviour
{
    public static SEManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shakeHitSound;  // シェイク時の効果音
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;  // 音量

    private AudioSource audioSource;

    private void Awake()
    {
        // シングルトン設定
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        // AudioSourceの初期化
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // AudioSourceの設定
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        Debug.Log("[SEManager] Initialized");

        // AudioClipの検証
        if (shakeHitSound == null)
        {
            Debug.LogWarning("[SEManager] shakeHitSound is not assigned! Please assign an AudioClip in the Inspector.");
        }
        else
        {
            Debug.Log($"[SEManager] Loaded audio clip: {shakeHitSound.name}");
        }
    }

    /// <summary>
    /// シェイク時の効果音を再生
    /// </summary>
    public void PlayShakeHit()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[SEManager] AudioSource is not initialized!");
            return;
        }

        if (shakeHitSound == null)
        {
            Debug.LogWarning("[SEManager] shakeHitSound is null! Cannot play sound.");
            return;
        }

        // PlayOneShotで低遅延再生
        audioSource.PlayOneShot(shakeHitSound, volume);
        
        Debug.Log($"[SEManager] 🔊 Playing shake hit sound");
    }

    /// <summary>
    /// 音量を設定
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
        Debug.Log($"[SEManager] Volume set to: {volume}");
    }

    /// <summary>
    /// 効果音をインスペクターから設定（デバッグ用）
    /// </summary>
    public void SetShakeHitSound(AudioClip clip)
    {
        shakeHitSound = clip;
        Debug.Log($"[SEManager] Shake hit sound set to: {clip?.name ?? "null"}");
    }
}
