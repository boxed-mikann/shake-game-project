using UnityEngine;

/// <summary>
/// ========================================
/// KeyboardInputReader（統一キュー方式）
/// ========================================
/// 
/// 責務：キーボード入力（デバッグ用）
/// 主機能：
/// - Input.GetKeyDown(KeyCode.Space) 等でシェイク検出
/// - ShakeResolver.EnqueueInput()で統一キューに追加
/// 
/// ========================================
/// </summary>
public class KeyboardInputReader : MonoBehaviour
{
    [SerializeField] private KeyCode _shakeKey = KeyCode.Space;
    
    void Update()
    {
        if (Input.GetKeyDown(_shakeKey))
        {
            double timestamp = AudioSettings.dspTime;
            ShakeResolver.EnqueueInput("shake", timestamp);  // ★ 統一キューに追加
        }
    }
}