using UnityEngine;
using TMPro;

/// <summary>
/// 判定ポップアップ - Perfect/Good/Bad判定表示
/// Animator + Trigger方式。表示後lifetimeで自動非表示。
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(TMPro.TextMeshPro))]
public class JudgePopup : MonoBehaviour
{
    //ちゃんと取得できるか、念のためアタッチできるかで確認
    //[SerializeField] private TMPro.TextMeshPro judgeText;
    private TMPro.TextMeshPro judgeText;
    private Animator animator; // Animator Controllerに"Show"トリガーを定義
    [SerializeField] private float lifetime = 1.0f;
    
    [Header("Colors")]
    [SerializeField] private Color perfectColor = Color.yellow;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color badColor = Color.red;
    [SerializeField] private Color missColor = Color.white;
    
    void Awake()
    {
        if (judgeText == null)
        {
            judgeText = GetComponent<TMPro.TextMeshPro>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // 初期状態は非アクティブにする
        gameObject.SetActive(false);
    }
    
    public void Show(string judgement)
    {
        Debug.Log($"[JudgePopup] Showing judgement: {judgement}");
        
        // キャンセルして再度非表示タイマーをリセット
        CancelInvoke(nameof(HidePopup));
        
        // テキスト設定
        if (judgeText != null)
        {
            judgeText.text = judgement;
            judgeText.color = GetJudgementColor(judgement);
        }
        
        // ゲームオブジェクトをアクティブにしてから、Animatorトリガーをセット
        gameObject.SetActive(true);
        
        if (animator != null)
        {
            // Animator の状態を完全にリセット
            animator.Rebind();
            animator.Update(0f);
            
            // アニメーション再生
            animator.SetTrigger("Show");
        }
        
        // 自動非表示タイマーをセット
        Invoke(nameof(HidePopup), lifetime);
    }
    
    private void HidePopup()
    {
        Debug.Log("[JudgePopup] Hiding popup");
        gameObject.SetActive(false);
    }
    
    private Color GetJudgementColor(string judgement)
    {
        return judgement.ToUpper() switch
        {
            "PERFECT" => perfectColor,
            "GOOD" => goodColor,
            "BAD" => badColor,
            "MISS" => missColor,
            _ => Color.white
        };
    }
}
