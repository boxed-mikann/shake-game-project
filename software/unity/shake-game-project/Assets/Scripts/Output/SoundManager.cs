using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource shakeCountAudioSource;
    [SerializeField] private AudioClip shakeCountSound;
    [SerializeField] private float soundVolume = 0.5f;
    
    void Start()
    {
        // AudioSource が未設定の場合は自動作成
        if (shakeCountAudioSource == null)
        {
            shakeCountAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// シェイク検知時に効果音を再生
    /// </summary>
    public void PlayShakeSound()
    {
        if (shakeCountSound != null && shakeCountAudioSource != null)
        {
            shakeCountAudioSource.PlayOneShot(shakeCountSound, soundVolume);
            Debug.Log("🔊 Shake sound played!");
        }
        else
        {
            Debug.LogWarning("⚠️ Shake sound or AudioSource not assigned!");
        }
    }
}