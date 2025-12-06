// using UnityEngine;
// using TMPro;

// /// <summary>
// /// シンクロポップアップ - シンクロ率に応じたメッセージ表示
// /// Animator + Trigger方式（モダン）。表示後lifetimeでプール返却。
// /// </summary>
// [RequireComponent(typeof(Animator))]
// public class SyncPopup : MonoBehaviour
// {
//     [SerializeField] private TextMeshProUGUI messageText;
//     [SerializeField] private Animator animator; // Animator Controllerに"Show"トリガーを定義
//     [SerializeField] private float lifetime = 1.0f; // 表示後返却までの秒数（Clip長と揃える）
    
//     [Header("Messages")]
//     [SerializeField] private string perfectMessage = "完璧！";
//     [SerializeField] private string syncMessage = "シンクロ！";
//     [SerializeField] private string goodMessage = "いいね！";
    
//     void Awake()
//     {
//         if (animator == null)
//         {
//             animator = GetComponent<Animator>();
//         }
//     }
    
//     public void Show(float syncRate)
//     {
//         string message = syncRate >= 0.9f ? perfectMessage : (syncRate >= 0.7f ? syncMessage : goodMessage);
//         if (messageText != null) messageText.text = message;
        
//         if (animator != null)
//         {
//             animator.ResetTrigger("Show");
//             animator.SetTrigger("Show");
//         }
        
//         CancelInvoke(nameof(ReturnToPool));
//         Invoke(nameof(ReturnToPool), lifetime);
//     }
    
//     private void ReturnToPool()
//     {
//         if (PopupPool.Instance != null)
//         {
//             PopupPool.Instance.ReturnSyncPopup(gameObject);
//         }
//         else
//         {
//             gameObject.SetActive(false);
//         }
//     }
// }
