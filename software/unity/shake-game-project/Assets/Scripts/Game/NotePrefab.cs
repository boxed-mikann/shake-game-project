using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音符・休符オブジェクト
/// 責務：自身の見た目管理、はじけ判定、フェーズ反映
/// </summary>
public class NotePrefab : MonoBehaviour
{
    private Phase _currentPhase = Phase.NotePhase;
    private Image _image;
    private Button _button;
    
    // 色設定
    private Color _noteColor = Color.white;      // 音符（白）
    private Color _restColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // 休符（グレー）
    
    private void Awake()
    {
        _image = GetComponent<Image>();
        _button = GetComponent<Button>();
        
        if (_button != null)
        {
            _button.onClick.AddListener(OnNoteClicked);
        }
    }
    
    private void Start()
    {
        // 初期フェーズを反映
        SetPhase(PhaseController.Instance.GetCurrentPhase());
    }
    
    /// <summary>
    /// フェーズを設定し、見た目を更新
    /// </summary>
    public void SetPhase(Phase phase)
    {
        _currentPhase = phase;
        
        if (_image != null)
        {
            if (phase == Phase.NotePhase)
            {
                _image.color = _noteColor;  // 白
            }
            else
            {
                _image.color = _restColor;  // グレー
            }
        }
    }
    
    /// <summary>
    /// クリック時の処理
    /// </summary>
    private void OnNoteClicked()
    {
        if (_currentPhase == Phase.NotePhase)
        {
            // 音符をはじけた → スコア加算
            ScoreManager.Instance.AddNoteScore(1);
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[NotePrefab] ✨ Note hit!");
        }
        else if (_currentPhase == Phase.RestPhase)
        {
            // 休符をはじけた → ペナルティ + フリーズ
            ScoreManager.Instance.SubtractRestPenalty(1);
            GameManager.Instance.TriggerFreeze();
            
            if (GameConstants.DEBUG_MODE)
                Debug.Log("[NotePrefab] ❌ Rest hit (penalty!)");
        }
        
        // オブジェクト削除
        Destroy(gameObject);
    }
}