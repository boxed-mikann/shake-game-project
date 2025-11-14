using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// マルチプレイヤー管理
/// </summary>
public class PlayerManager : MonoBehaviour
{
    // ===== Singleton =====
    private static PlayerManager _instance;
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerManager");
                    _instance = go.AddComponent<PlayerManager>();
                }
            }
            return _instance;
        }
    }

    // ===== プレイヤー管理 =====
    private Dictionary<int, PlayerUIController> _playerUIControllers = new Dictionary<int, PlayerUIController>();
    
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
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PlayerManager] Initialize completed");
        }
    }
    
    /// <summary>
    /// プレイヤーUI コントローラーを登録
    /// </summary>
    public void RegisterPlayerUI(int childId, PlayerUIController uiController)
    {
        if (!_playerUIControllers.ContainsKey(childId))
        {
            _playerUIControllers[childId] = uiController;
            
            if (GameConstants.DEBUG_MODE)
            {
                Debug.Log($"[PlayerManager] Registered player UI: {childId}");
            }
        }
    }
    
    /// <summary>
    /// プレイヤーデータからUIを更新
    /// </summary>
    public void UpdatePlayerUI(PlayerData playerData)
    {
        if (_playerUIControllers.TryGetValue(playerData.childId, out var uiController))
        {
            uiController.UpdateUI(playerData);
        }
    }
    
    /// <summary>
    /// すべてのプレイヤーUIを更新
    /// </summary>
    public void UpdateAllPlayerUI()
    {
        var players = GameManager.Instance.Players;
        
        foreach (var playerEntry in players)
        {
            UpdatePlayerUI(playerEntry.Value);
        }
    }
    
    /// <summary>
    /// プレイヤーマネージャーをリセット
    /// </summary>
    public void Reset()
    {
        _playerUIControllers.Clear();
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[PlayerManager] Reset");
        }
    }
    
    // ===== Getter =====
    public Dictionary<int, PlayerUIController> PlayerUIControllers => _playerUIControllers;
    public int PlayerCount => _playerUIControllers.Count;
}

/// <summary>
/// プレイヤーのUI表示を管理するコンポーネント
/// 各プレイヤースロット（UI）に付与される
/// </summary>
public class PlayerUIController : MonoBehaviour
{
    [SerializeField] private int _childId;
    [SerializeField] private UnityEngine.UI.Image _energyBar;
    [SerializeField] private UnityEngine.UI.Text _playerNameText;
    
    private PlayerData _currentPlayerData;
    
    public void Initialize(int childId)
    {
        _childId = childId;
        PlayerManager.Instance.RegisterPlayerUI(childId, this);
    }
    
    public void UpdateUI(PlayerData playerData)
    {
        _currentPlayerData = playerData;
        
        // エネルギーバー更新
        if (_energyBar != null)
        {
            float energyRatio = playerData.energyLevel / 100f;
            _energyBar.fillAmount = energyRatio;
        }
        
        // プレイヤー名表示
        if (_playerNameText != null)
        {
            _playerNameText.text = $"Player {_childId}";
        }
    }
    
    public int ChildId => _childId;
}
