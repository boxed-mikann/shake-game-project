// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// /// <summary>
// /// 結果画面UI - 最終ボルテージ、ランク、メッセージ表示
// /// </summary>
// public class ResultUI : MonoBehaviour
// {
//     [Header("UI Elements")]
//     [SerializeField] private Slider voltageGauge;
//     [SerializeField] private TextMeshProUGUI rankText;
//     [SerializeField] private TextMeshProUGUI messageText;
//     [SerializeField] private Image characterReactionImage;
    
//     [Header("Rank Sprites")]
//     [SerializeField] private Sprite[] rankSprites; // S, A, B, C
    
//     [Header("Rank Messages")]
//     [SerializeField] private string[] rankMessages = new string[] {
//         "完璧！最高の盛り上がり！",
//         "素晴らしい！チームワーク抜群！",
//         "いい感じ！もう少しで完璧！",
//         "まずまず！次はもっと頑張ろう！"
//     };
    
//     void Start()
//     {
//         SubscribeToEvents();
//     }
    
//     void OnEnable()
//     {
//         SubscribeToEvents();
//     }
    
//     void OnDisable()
//     {
//         if (GameManagerV2.Instance != null)
//         {
//             GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
//         }
//     }
    
//     private void SubscribeToEvents()
//     {
//         if (GameManagerV2.Instance != null)
//         {
//             // 重複購読を避けるため、一度解除してから購読
//             GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
//             GameManagerV2.Instance.OnGameEnd += OnGameEnd;
//         }
//     }
    
//     private void OnGameEnd()
//     {
//         if (VoltageManager.Instance == null) return;
        
//         float finalVoltage = VoltageManager.Instance.GetVoltage();
//         float voltageRate = finalVoltage / GameConstantsV2.VOLTAGE_MAX;
        
//         // ランク判定
//         string rank;
//         int rankIndex;
//         if (voltageRate >= 0.8f)
//         {
//             rank = "S";
//             rankIndex = 0;
//         }
//         else if (voltageRate >= 0.6f)
//         {
//             rank = "A";
//             rankIndex = 1;
//         }
//         else if (voltageRate >= 0.4f)
//         {
//             rank = "B";
//             rankIndex = 2;
//         }
//         else
//         {
//             rank = "C";
//             rankIndex = 3;
//         }
        
//         // UI更新
//         if (voltageGauge != null)
//         {
//             voltageGauge.value = voltageRate;
//         }
        
//         if (rankText != null)
//         {
//             rankText.text = rank;
//         }
        
//         if (messageText != null && rankIndex < rankMessages.Length)
//         {
//             messageText.text = rankMessages[rankIndex];
//         }
        
//         if (characterReactionImage != null && rankIndex < rankSprites.Length && rankSprites[rankIndex] != null)
//         {
//             characterReactionImage.sprite = rankSprites[rankIndex];
//         }
//     }
// }
