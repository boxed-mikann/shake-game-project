using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer gameplayVideoPlayer;
    [SerializeField] private VideoPlayer victoryVideoPlayer;
    [SerializeField] private RawImage gameplayRawImage;
    [SerializeField] private RawImage victoryRawImage;
    
    [SerializeField] private VideoClip gameplayVideo;
    [SerializeField] private VideoClip victoryVideo;
    
    void Start()
    {
        SetupVideoPlayers();
        PlayGameplayVideo();
    }
    
    /// <summary>
    /// VideoPlayer と RawImage を接続
    /// </summary>
    private void SetupVideoPlayers()
    {
        if (gameplayVideoPlayer != null && gameplayRawImage != null)
        {
            gameplayVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            RenderTexture gameplayRT = new RenderTexture(1920, 1080, 24);
            gameplayVideoPlayer.targetTexture = gameplayRT;
            gameplayRawImage.texture = gameplayRT;
        }

        if (victoryVideoPlayer != null && victoryRawImage != null)
        {
            victoryVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            RenderTexture victoryRT = new RenderTexture(1920, 1080, 24);
            victoryVideoPlayer.targetTexture = victoryRT;
            victoryRawImage.texture = victoryRT;
        }
        // Hide victory UI and ensure the victory player is not playing at start
        if (victoryRawImage != null)
        {
            victoryRawImage.gameObject.SetActive(false);
        }

        if (victoryVideoPlayer != null)
        {
            victoryVideoPlayer.Stop();
        }
    }
    
    public void PlayGameplayVideo()
    {
        if (gameplayVideoPlayer != null && gameplayVideo != null)
        {
            gameplayVideoPlayer.clip = gameplayVideo;
            gameplayVideoPlayer.Play();
            gameplayVideoPlayer.isLooping = true;
            Debug.Log("▶️ Gameplay video started (looping)");
        }
    }
    
    public void PlayVictoryVideo()
    {
        victoryRawImage.gameObject.SetActive(true);   
        if (victoryVideoPlayer != null && victoryVideo != null)
        {
            victoryVideoPlayer.clip = victoryVideo;
            victoryVideoPlayer.Play();
            victoryVideoPlayer.isLooping = false;
            Debug.Log("🏆 Victory video started");
        }
    }
    
    public void StopVictoryVideo()
    {
        if (victoryVideoPlayer != null)
        {
            victoryVideoPlayer.Stop();
        }
        PlayGameplayVideo();
    }
}