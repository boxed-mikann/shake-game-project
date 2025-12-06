using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// ========================================
/// ShakeResolverV2 - ハンドラー戦略パターン版
/// ========================================
/// 
/// 責務：シェイク入力のルーティングと状態別処理の切り替え
/// - 入力キューからシェイクを取得
/// - 現在の GameState に応じて処理ハンドラーを切り替え
/// - イベント駆動でハンドラーを更新
/// 
/// 処理フロー：
/// 1. ShakeHandlerBase 継承クラスを保持（初期値: IdleRegisterHandler）
/// 2. GameManagerV2 のイベント購読で状態遷移時にハンドラーを切り替え
/// 3. Update() で入力キューを処理し、現在のハンドラーに委譲
/// 
/// ========================================
/// </summary>
public class ShakeResolverV2 : MonoBehaviour
{
    private static ConcurrentQueue<(string deviceId, double timestamp)> _sharedInputQueue 
        = new ConcurrentQueue<(string deviceId, double timestamp)>();
    
    [SerializeField] private IdleRegisterHandler idleRegisterHandler;
    [SerializeField] private IdleStartingHandler idleStartingHandler;
    [SerializeField] private GamePlayHandler gamePlayHandler;
    
    private ShakeHandlerBase _currentHandler;

    private void Awake()
    {
        // 初期ハンドラーは IdleRegisterHandler
        _currentHandler = idleRegisterHandler;
    }

    private System.Action onResisterStartAction;
    private System.Action onIdleStartAction;
    private System.Action onGameStartAction;
    private System.Action onGameEndAction;
    
    private void Start()
    {
        SubscribeToEvents();
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        if (GameManagerV2.Instance != null && onResisterStartAction != null)
        {
            GameManagerV2.Instance.OnResisterStart -= onResisterStartAction;
            GameManagerV2.Instance.OnIdleStart -= onIdleStartAction;
            GameManagerV2.Instance.OnGameStart -= onGameStartAction;
            GameManagerV2.Instance.OnGameEnd -= onGameEndAction;
        }
    }
    
    private void SubscribeToEvents()
    {
        if (GameManagerV2.Instance != null)
        {
            // ラムダ式をフィールドに保存して重複購読を防ぐ
            if (onResisterStartAction == null)
            {
                onResisterStartAction = () =>
                {
                    Debug.Log("[ShakeResolverV2] State changed to IdleRegister");
                    _currentHandler = idleRegisterHandler;
                };
            }
            if (onIdleStartAction == null)
            {
                onIdleStartAction = () =>
                {
                    Debug.Log("[ShakeResolverV2] State changed to IdleStarting");
                    _currentHandler = idleStartingHandler;
                };
            }
            if (onGameStartAction == null)
            {
                onGameStartAction = () =>
                {
                    Debug.Log("[ShakeResolverV2] State changed to Game");
                    _currentHandler = gamePlayHandler;
                };
            }
            if (onGameEndAction == null)
            {
                onGameEndAction = () =>
                {
                    Debug.Log("[ShakeResolverV2] State changed to Result (no handler)");
                    // Result状態では特に処理しない
                    _currentHandler = null;
                };
            }
            
            // 重複購読を避けるため、一度解除してから購読
            GameManagerV2.Instance.OnResisterStart -= onResisterStartAction;
            GameManagerV2.Instance.OnResisterStart += onResisterStartAction;
            GameManagerV2.Instance.OnIdleStart -= onIdleStartAction;
            GameManagerV2.Instance.OnIdleStart += onIdleStartAction;
            GameManagerV2.Instance.OnGameStart -= onGameStartAction;
            GameManagerV2.Instance.OnGameStart += onGameStartAction;
            GameManagerV2.Instance.OnGameEnd -= onGameEndAction;
            GameManagerV2.Instance.OnGameEnd += onGameEndAction;
        }
        else
        {
            Debug.LogWarning("[ShakeResolverV2] GameManagerV2.Instance is null!");
        }
    }

    public static void EnqueueInput(string deviceId, double timestamp)
    {
        _sharedInputQueue.Enqueue((deviceId, timestamp));
    }
    
    void Update()
    {
        while (_sharedInputQueue.TryDequeue(out var input))
        {
            Debug.Log($"[ShakeResolverV2] Dequeued shake: DeviceID={input.deviceId} at {input.timestamp}s");
            ProcessShake(input.deviceId, input.timestamp);
        }
    }
    
    private void ProcessShake(string deviceId, double timestamp)
    {
        if (_currentHandler == null)
        {
            Debug.LogWarning($"[ShakeResolverV2] No handler assigned for shake input");
            return;
        }

        _currentHandler.HandleShake(deviceId, timestamp);
    }
}
