using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// メッセージ吹き出しUI - キャラクター画像 + テキストメッセージ
/// PhaseManagerV2と連携して表示内容を変更（Phase 4で完全実装）
/// </summary>
public class MessageBubbleUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI messageText;
    
    [Header("Animation")]
    [SerializeField] private Animation bubbleAnimation;
    
    void Start()
    {
        // 初期状態では非表示
        if (gameObject.activeSelf && bubbleAnimation == null)
        {
            gameObject.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        // Phase 4で実装: PhaseManagerV2.OnPhaseChanged購読
    }
    
    void OnDisable()
    {
        // Phase 4で実装: PhaseManagerV2.OnPhaseChanged購読解除
    }
    
    public void ShowMessage(string message, Sprite characterSprite = null)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        if (characterImage != null && characterSprite != null)
        {
            characterImage.sprite = characterSprite;
        }
        
        gameObject.SetActive(true);
        
        if (bubbleAnimation != null)
        {
            bubbleAnimation.Play();
        }
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
