using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SerialPortConfigUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject configPanel;
    [SerializeField] private TMP_Dropdown portDropdown;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button openConfigButton;
    [SerializeField] private Button closeConfigButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        // ボタンのリスナー登録
        if (openConfigButton != null)
            openConfigButton.onClick.AddListener(OpenConfig);
        
        if (closeConfigButton != null)
            closeConfigButton.onClick.AddListener(CloseConfig);
            
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectButtonClicked);

        // 初期状態は非表示
        if (configPanel != null)
            configPanel.SetActive(false);
            
        UpdateStatusText();
    }

    private void Update()
    {
        // 接続状態の監視（簡易的）
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText != null && SerialPortManagerV2.Instance != null)
        {
            bool isConnected = SerialPortManagerV2.Instance.IsConnected;
            string port = SerialPortManagerV2.Instance.CurrentPortName;
            statusText.text = isConnected ? $"Connected: {port}" : "Disconnected";
            statusText.color = isConnected ? Color.green : Color.red;
        }
    }

    public void OpenConfig()
    {
        if (configPanel != null)
        {
            configPanel.SetActive(true);
            RefreshPortList();
        }
    }

    public void CloseConfig()
    {
        if (configPanel != null)
            configPanel.SetActive(false);
    }

    private void RefreshPortList()
    {
        if (portDropdown == null || SerialPortManagerV2.Instance == null) return;

        portDropdown.ClearOptions();

        string[] ports = SerialPortManagerV2.Instance.GetAvailablePorts();
        List<string> options = new List<string>(ports);
        
        // ポートが見つからない場合のメッセージ
        if (options.Count == 0)
        {
            options.Add("No Ports Found");
            portDropdown.interactable = false;
        }
        else
        {
            portDropdown.interactable = true;
        }

        portDropdown.AddOptions(options);

        // 現在のポートを選択状態にする
        string currentPort = SerialPortManagerV2.Instance.CurrentPortName;
        int index = options.FindIndex(p => p == currentPort);
        if (index >= 0)
        {
            portDropdown.value = index;
        }
    }

    private void OnConnectButtonClicked()
    {
        if (portDropdown == null || SerialPortManagerV2.Instance == null) return;
        
        if (portDropdown.options.Count == 0) return;

        string selectedPort = portDropdown.options[portDropdown.value].text;
        
        // "No Ports Found" などのメッセージを選択している場合は無視
        if (selectedPort.Contains(" ")) return; 

        SerialPortManagerV2.Instance.Connect(selectedPort);
    }
}
