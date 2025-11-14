using UnityEngine;

/// <summary>
/// オーディオ管理 - BGM・SE再生
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ===== Singleton =====
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    // ===== AudioSource =====
    private AudioSource _bgmAudioSource;
    private AudioSource _sfxAudioSource;
    
    // ===== オーディオクリップ =====
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private AudioClip _shakeDetectedSFX;
    [SerializeField] private AudioClip _damageHitSFX;
    [SerializeField] private AudioClip _syncSuccessSFX;
    [SerializeField] private AudioClip _bossAttackSFX;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        // AudioSource を自動作成
        _bgmAudioSource = GetComponent<AudioSource>();
        if (_bgmAudioSource == null)
        {
            _bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }
        _bgmAudioSource.loop = true;
        _bgmAudioSource.volume = 0.7f;
        
        // SFX用AudioSource
        _sfxAudioSource = gameObject.AddComponent<AudioSource>();
        _sfxAudioSource.loop = false;
        _sfxAudioSource.volume = 0.8f;
        
        // オーディオクリップをResources から読み込み（未設定の場合）
        if (_bgmClip == null)
        {
            _bgmClip = Resources.Load<AudioClip>("Audio/BGM/boss_theme_120bpm_30s");
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[AudioManager] Initialize completed");
        }
    }
    
    /// <summary>
    /// BGM を再生
    /// </summary>
    public void PlayBGM()
    {
        if (_bgmAudioSource != null && _bgmClip != null)
        {
            _bgmAudioSource.clip = _bgmClip;
            _bgmAudioSource.Play();
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log("[AudioManager] BGM started");
            }
        }
    }
    
    /// <summary>
    /// BGM を停止
    /// </summary>
    public void StopBGM()
    {
        if (_bgmAudioSource != null)
        {
            _bgmAudioSource.Stop();
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log("[AudioManager] BGM stopped");
            }
        }
    }
    
    /// <summary>
    /// SE を再生
    /// </summary>
    public void PlaySFX(SFXType sfxType)
    {
        if (_sfxAudioSource == null)
            return;
        
        AudioClip clip = sfxType switch
        {
            SFXType.ShakeDetected => _shakeDetectedSFX,
            SFXType.DamageHit => _damageHitSFX,
            SFXType.SyncSuccess => _syncSuccessSFX,
            SFXType.BossAttack => _bossAttackSFX,
            _ => null
        };
        
        if (clip != null)
        {
            _sfxAudioSource.PlayOneShot(clip);
        }
    }
    
    // ===== Getter/Setter =====
    public AudioClip BGMClip => _bgmClip;
    public void SetBGMClip(AudioClip clip) => _bgmClip = clip;
}

/// <summary>
/// SE の種類
/// </summary>
public enum SFXType
{
    ShakeDetected,
    DamageHit,
    SyncSuccess,
    BossAttack
}
