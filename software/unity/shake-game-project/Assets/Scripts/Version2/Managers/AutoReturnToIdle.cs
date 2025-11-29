using UnityEngine;

/// <summary>
/// 結果画面から待機画面への自動遷移
/// 5秒後に自動でIdle画面に戻る（スペースキーでスキップ可能）
/// </summary>
public class AutoReturnToIdle : MonoBehaviour
{
    [SerializeField] private float returnDelay = 5f;
    [SerializeField] private bool allowSpaceSkip = true;
    
    private float timer = 0f;
    private bool isActive = false;
    
    void OnEnable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameEnd += OnGameEnd;
        }
    }
    
    void OnDisable()
    {
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.OnGameEnd -= OnGameEnd;
        }
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // スペースキーでスキップ
        if (allowSpaceSkip && Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToIdle();
            return;
        }
        
        // タイマー更新
        timer += Time.deltaTime;
        if (timer >= returnDelay)
        {
            ReturnToIdle();
        }
    }
    
    private void OnGameEnd()
    {
        timer = 0f;
        isActive = true;
    }
    
    private void ReturnToIdle()
    {
        isActive = false;
        timer = 0f;
        
        if (GameManagerV2.Instance != null)
        {
            GameManagerV2.Instance.ShowIdle();
        }
    }
}
