using UnityEngine;
using TMPro;

/// <summary>
/// 判定ポップアップ - Perfect/Good/Bad判定表示
/// Animator + Trigger方式。表示後lifetimeでプール返却。
/// </summary>
[RequireComponent(typeof(Animator))]
public class JudgePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI judgeText;
    [SerializeField] private Animator animator; // Animator Controllerに"Show"トリガーを定義
    [SerializeField] private float lifetime = 1.0f;
    
    [Header("Colors")]
    [SerializeField] private Color perfectColor = Color.yellow;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color badColor = Color.red;
    
    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
    
    public void Show(string judgement)
    {
        if (judgeText != null)
        {
            judgeText.text = judgement;
            switch (judgement.ToUpper())
            {
                case "PERFECT": judgeText.color = perfectColor; break;
                case "GOOD":    judgeText.color = goodColor;    break;
                case "BAD":     judgeText.color = badColor;     break;
                default:         judgeText.color = badColor;     break;
            }
        }
        
        if (animator != null)
        {
            animator.ResetTrigger("Show");
            animator.SetTrigger("Show");
        }
        
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), lifetime);
    }
    
    private void ReturnToPool()
    {
        if (PopupPool.Instance != null)
        {
            PopupPool.Instance.ReturnJudgePopup(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
