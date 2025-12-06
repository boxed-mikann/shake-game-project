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
/// - デバイスIDごとに異なる音色を再生可能
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
    [SerializeField] private AudioClip[] shakeHitSounds = new AudioClip[10];  // デバイスID別の効果音（10種類）
    [SerializeField] private AudioClip registerSound;  // デバイス登録完了時の効果音
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

        // AudioClip配列の検証
        int loadedClips = 0;
        for (int i = 0; i < shakeHitSounds.Length; i++)
        {
            if (shakeHitSounds[i] != null)
            {
                loadedClips++;
                Debug.Log($"[SEManager] Loaded audio clip[{i}]: {shakeHitSounds[i].name}");
            }
        }
        if (loadedClips == 0)
        {
            Debug.LogWarning("[SEManager] No shake hit sounds assigned! Please assign AudioClips in the Inspector.");
        }
        else
        {
            Debug.Log($"[SEManager] Loaded {loadedClips} audio clips");
        }
    }

    /// <summary>
    /// シェイク時の効果音を再生
    /// </summary>
    /// <param name="deviceId">デバイスID (0-9)。未指定または範囲外の場合はID 0を使用。</param>
    public void PlayShakeHit(int deviceId = 0)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[SEManager] AudioSource is not initialized!");
            return;
        }

        // デバイスIDの範囲チェック
        if (deviceId < 0 || deviceId >= shakeHitSounds.Length)
        {
            Debug.LogWarning($"[SEManager] Invalid deviceId: {deviceId}. Using default (0).");
            deviceId = 0;
        }

        AudioClip clip = shakeHitSounds[deviceId];
        if (clip == null)
        {
            Debug.LogWarning($"[SEManager] shakeHitSound[{deviceId}] is null! Cannot play sound.");
            return;
        }

        // PlayOneShotで低遅延再生
        audioSource.PlayOneShot(clip, volume);
        
        Debug.Log($"[SEManager] 🔊 Playing shake hit sound (ID: {deviceId}, Clip: {clip.name})");
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
    /// デバイス登録完了時の効果音を再生
    /// </summary>
    public void PlayRegisterSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[SEManager] AudioSource is not initialized!");
            return;
        }

        if (registerSound == null)
        {
            Debug.LogWarning("[SEManager] registerSound is null! Cannot play sound.");
            return;
        }

        // PlayOneShotで低遅延再生
        audioSource.PlayOneShot(registerSound, volume);
        
        Debug.Log($"[SEManager] 🔊 Playing register sound (Clip: {registerSound.name})");
    }

    /// <summary>
    /// 指定したIDの効果音を設定（デバッグ用）
    /// </summary>
    public void SetShakeHitSound(int deviceId, AudioClip clip)
    {
        if (deviceId >= 0 && deviceId < shakeHitSounds.Length)
        {
            shakeHitSounds[deviceId] = clip;
            string clipName = clip != null ? clip.name : "null";
            Debug.Log($"[SEManager] Shake hit sound[{deviceId}] set to: {clipName}");
        }
        else
        {
            Debug.LogWarning($"[SEManager] Invalid deviceId: {deviceId}");
        }
    }
}
