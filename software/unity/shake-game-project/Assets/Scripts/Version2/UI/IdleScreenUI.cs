using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 待機画面UI - タイトル、ガイド、スタートボタン
/// </summary>
public class IdleScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private Button startButton;
    
    [Header("Settings")]
    [SerializeField] private string titleMessage = "みんなでシェイク！";
    [SerializeField] private string guideMessage = "シェイクでデバイス登録！\n全員でシェイクしてスタート！";
    
    void Start()
    {
        if (titleText != null)
        {
            titleText.text = titleMessage;
        }
        
        if (guideText != null)
        {
            guideText.text = guideMessage;
        }
        
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }
    
    void Update()
    {
        // エンターキーでゲーム開始
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TryStartGame();
        }
        
        // 同時シェイクで自動開始
        if (DeviceManager.Instance != null && DeviceManager.Instance.CheckSimultaneousShakeToStart())
        {
            TryStartGame();
        }
    }
    
    void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }
    
    private bool isGameStarting = false;
    
    private void OnStartButtonClicked()
    {
        TryStartGame();
    }
    
    private void TryStartGame()
    {
        if (isGameStarting) return;
        
        // デバイスが登録されているか確認
        if (DeviceManager.Instance == null || DeviceManager.Instance.GetDeviceCount() == 0)
        {
            Debug.Log("[IdleScreenUI] デバイスが登録されていません");
            return;
        }
        
        isGameStarting = true;
        
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.StartGame();
        }
        
        if (GameConstantsV2.DEBUG_MODE)
        {
            Debug.Log($"[IdleScreenUI] ゲーム開始: {DeviceManager.Instance.GetDeviceCount()}台登録済み");
        }
    }
}
