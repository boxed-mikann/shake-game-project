using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeviceIconManager : MonoBehaviour
{
    [SerializeField] private GameObject deviceIconPrefab;
    [SerializeField] private Transform iconContainer; // UI時の親、またはWorld時の仮親
    [SerializeField] private Sprite[] deviceSprites; // Inspector設定（10種類、0-9）
    [SerializeField] private bool isGamePlayCanvas = false; // GamePlayCanvas用かどうか
    
    [Header("World Render Settings")]
    [SerializeField] private bool spawnInWorldSpace = false; // trueでデフォルト層（SpriteRenderer）として配置
    [SerializeField] private Transform worldContainer; // 未指定ならルート直下
    [SerializeField] private Vector2 screenMargin = new Vector2(120, 120); // 画面左下からの余白(px)
    [SerializeField] private Vector2 cellSizePx = new Vector2(120, 120); // 1アイコンのピクセル幅高さ
    [SerializeField] private int iconsPerRow = 6; // 1行あたり
    [SerializeField] private float worldZDepth = 0f; // カメラからのZ
    [SerializeField] private int spriteSortingOrder = 50; // Canvasより下または上に調整
    
    private Dictionary<string, DeviceIcon> activeIcons = new Dictionary<string, DeviceIcon>();
    private bool subscribedDeviceMgr = false;
    private bool subscribedGameMgr = false;
    
    void OnEnable()
    {
        // 自動判定: Canvas_Game 配下ならゲーム用として扱う
        if (transform.root != null && transform.root.name.Contains("Canvas_Game"))
        {
            isGamePlayCanvas = true;
        }

        // インスタンス生成順の影響を受けないように遅延購読
        StartCoroutine(EnsureSubscriptions());
    }
    
    void OnDisable()
    {
        if (subscribedDeviceMgr && DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDeviceRegistered -= OnDeviceRegistered;
        }
        subscribedDeviceMgr = false;
        
        if (subscribedGameMgr && GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameStart -= OnGameStart;
        }
        subscribedGameMgr = false;
    }

    private System.Collections.IEnumerator EnsureSubscriptions()
    {
        float timeout = 3f; // safety
        float t = 0f;
        // DeviceManager購読
        while (!subscribedDeviceMgr && t < timeout)
        {
            if (DeviceManager.Instance != null)
            {
                DeviceManager.Instance.OnDeviceRegistered += OnDeviceRegistered;
                subscribedDeviceMgr = true;
                Debug.Log("[DeviceIconManager] Subscribed to DeviceManager.OnDeviceRegistered");
                // 既存登録を反映
                RegenerateAllIcons();
                break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // GameManager購読
        t = 0f;
        while (!subscribedGameMgr && t < timeout)
        {
            if (GameManagerV2.Instance != null)
            {
                GameManagerV2.Instance.OnGameStart += OnGameStart;
                subscribedGameMgr = true;
                Debug.Log("[DeviceIconManager] Subscribed to GameManagerV2.OnGameStart");
                break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
    
    private void OnGameStart()
    {
        // GamePlayCanvas用の場合、ゲーム開始時に登録済みデバイスのアイコンを生成
        if (isGamePlayCanvas)
        {
            RegenerateAllIcons();
        }
    }
    
    private void RegenerateAllIcons()
    {
        // 既存のアイコンをクリア
        foreach (var icon in activeIcons.Values)
        {
            if (icon != null && icon.gameObject != null)
            {
                Destroy(icon.gameObject);
            }
        }
        activeIcons.Clear();
        
        // 登録済みデバイスのアイコンを再生成
        if (DeviceManager.Instance != null)
        {
            foreach (var device in DeviceManager.Instance.GetRegisteredDevices())
            {
                OnDeviceRegistered(device.deviceId);
            }
        }
    }
    
    private void OnDeviceRegistered(string deviceId)
    {
        Debug.Log($"[DeviceIconManager] OnDeviceRegistered: DeviceID={deviceId}, isGamePlayCanvas={isGamePlayCanvas}");
        GameObject iconObj;
        DeviceIcon icon;

        if (spawnInWorldSpace)
        {
            // ワールド空間（SpriteRenderer）で生成
            iconObj = Instantiate(deviceIconPrefab);
            if (worldContainer != null)
            {
                iconObj.transform.SetParent(worldContainer, false);
            }
            Debug.Log($"[DeviceIconManager] Spawned world-space icon for: {deviceId}");

            // 座標配置（画面左下からグリッド）
            var cam = Camera.main;
            if (cam != null)
            {
                int index = activeIcons.Count; // 追加前の数
                int col = iconsPerRow > 0 ? index % iconsPerRow : index;
                int row = iconsPerRow > 0 ? index / iconsPerRow : 0;
                float px = screenMargin.x + col * cellSizePx.x;
                float py = screenMargin.y + row * cellSizePx.y;
                var sp = new Vector3(px, py, cam.nearClipPlane + 1f + worldZDepth);
                var wp = cam.ScreenToWorldPoint(sp);
                iconObj.transform.position = wp;
            }

            // ソーティング調整
            var sr = iconObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = spriteSortingOrder;
            }

            icon = iconObj.GetComponent<DeviceIcon>();
            if (icon == null) icon = iconObj.AddComponent<DeviceIcon>();
        }
        else
        {
            // UI（Image）で生成
            iconObj = Instantiate(deviceIconPrefab, iconContainer);
            Debug.Log($"[DeviceIconManager] Instantiated icon prefab for: {deviceId}");

            // RectTransformが無ければUI要素で作り直す
            if (iconObj.GetComponent<RectTransform>() == null)
            {
                Debug.Log("[DeviceIconManager] Prefab has no RectTransform; creating UI Image instead.");
                Destroy(iconObj);
                var uiObj = new GameObject($"DeviceIconUI_{deviceId}", typeof(RectTransform), typeof(Image), typeof(DeviceIcon));
                var rt = uiObj.GetComponent<RectTransform>();
                rt.SetParent(iconContainer, false);
                rt.sizeDelta = cellSizePx;
                iconObj = uiObj;
            }

            icon = iconObj.GetComponent<DeviceIcon>();
            if (icon == null) icon = iconObj.AddComponent<DeviceIcon>();
        }

        // デバイスID（0-9）に応じた画像設定
        int id = int.Parse(deviceId);
        if (id >= 0 && id < deviceSprites.Length)
        {
            icon.Initialize(deviceId, deviceSprites[id]);
            Debug.Log($"[DeviceIconManager] Initialized icon with sprite[{id}]");
        }
        else
        {
            Debug.LogWarning($"[DeviceIconManager] Invalid device ID: {deviceId}");
        }

        activeIcons[deviceId] = icon;
    }
    
    // ★ ShakeResolverV2から呼び出される
    public DeviceIcon GetDeviceIcon(string deviceId)
    {
        return activeIcons.TryGetValue(deviceId, out var icon) ? icon : null;
    }
    
    // GamePlayCanvas用に再生成（Phase 1-3で使用）
    public void RegenerateForGamePlay(Transform gamePlayContainer)
    {
        foreach (var kvp in activeIcons)
        {
            GameObject iconObj = Instantiate(deviceIconPrefab, gamePlayContainer);
            DeviceIcon icon = iconObj.GetComponent<DeviceIcon>();
            
            int id = int.Parse(kvp.Key);
            if (id >= 0 && id < deviceSprites.Length)
            {
                icon.Initialize(kvp.Key, deviceSprites[id]);
            }
        }
    }
}
