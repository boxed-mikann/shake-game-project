using UnityEngine;

/// <summary>
/// ボスの状態・ダメージ・アニメーション管理
/// </summary>
public class BossController : MonoBehaviour
{
    // ===== Singleton =====
    private static BossController _instance;
    public static BossController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BossController>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("BossController");
                    _instance = go.AddComponent<BossController>();
                }
            }
            return _instance;
        }
    }

    // ===== ボス状態 =====
    private BossState _bossState = BossState.Idle;
    private float _currentHP;
    private float _maxHP = GameConstants.BOSS_MAX_HP;
    
    // ===== ボスビジュアル =====
    private Transform _bossTransform;
    private Animator _bossAnimator;
    private SpriteRenderer _bossSpriteRenderer;
    
    // ===== ダメージフィードバック =====
    private float _damageFlashDuration = 0.2f;
    private float _damageFlashTimer = 0f;
    private bool _isFlashing = false;
    
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
        // ボスのゲームオブジェクト取得
        _bossTransform = GetComponent<Transform>();
        _bossAnimator = GetComponent<Animator>();
        _bossSpriteRenderer = GetComponent<SpriteRenderer>();
        
        _currentHP = _maxHP;
        _bossState = BossState.Idle;
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[BossController] Initialize completed");
        }
    }
    
    private void Update()
    {
        // ダメージフラッシュアニメーション更新
        if (_isFlashing)
        {
            UpdateDamageFlash();
        }
    }
    
    /// <summary>
    /// ボスにダメージを与える
    /// </summary>
    public void TakeDamage(float damage, DamageType damageType)
    {
        if (_bossState == BossState.Dead)
            return;
        
        _currentHP -= damage;
        _currentHP = Mathf.Max(0, _currentHP);
        
        _bossState = BossState.TakingDamage;
        
        // ダメージフラッシュ開始
        PlayDamageFlash();
        
        // ダメージポップアップ表示
        ShowDamagePopup(damage, damageType);
        
        // ボス倒された判定
        if (_currentHP <= 0)
        {
            _bossState = BossState.Dead;
            PlayDefeatAnimation();
        }
        else
        {
            _bossState = BossState.Idle;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log($"[BossController] Boss took {damage} damage ({damageType}). HP: {_currentHP}/{_maxHP}");
        }
    }
    
    /// <summary>
    /// ダメージフラッシュを再生
    /// </summary>
    private void PlayDamageFlash()
    {
        if (_bossSpriteRenderer == null)
            return;
        
        _isFlashing = true;
        _damageFlashTimer = _damageFlashDuration;
    }
    
    /// <summary>
    /// ダメージフラッシュアニメーション更新
    /// </summary>
    private void UpdateDamageFlash()
    {
        _damageFlashTimer -= Time.deltaTime;
        
        if (_bossSpriteRenderer != null)
        {
            // 点滅させる
            float alpha = (_damageFlashTimer % (_damageFlashDuration / 2f)) / (_damageFlashDuration / 2f);
            Color color = _bossSpriteRenderer.color;
            color.a = Mathf.Lerp(0.5f, 1f, alpha);
            _bossSpriteRenderer.color = color;
        }
        
        if (_damageFlashTimer <= 0)
        {
            _isFlashing = false;
            if (_bossSpriteRenderer != null)
            {
                Color color = _bossSpriteRenderer.color;
                color.a = 1f;
                _bossSpriteRenderer.color = color;
            }
        }
    }
    
    /// <summary>
    /// ダメージポップアップを表示
    /// </summary>
    private void ShowDamagePopup(float damage, DamageType damageType)
    {
        // UIManager経由でダメージ表示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDamagePopup(damage, damageType);
        }
    }
    
    /// <summary>
    /// ボス倒時のアニメーション
    /// </summary>
    private void PlayDefeatAnimation()
    {
        if (_bossAnimator != null)
        {
            _bossAnimator.SetTrigger("Defeat");
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[BossController] Boss defeated!");
        }
    }
    
    /// <summary>
    /// 攻撃アニメーションを再生
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (_bossAnimator != null)
        {
            _bossAnimator.SetTrigger("Attack");
        }
    }
    
    /// <summary>
    /// ボスをリセット
    /// </summary>
    public void Reset()
    {
        _currentHP = _maxHP;
        _bossState = BossState.Idle;
        _isFlashing = false;
        _damageFlashTimer = 0f;
        
        if (_bossSpriteRenderer != null)
        {
            Color color = _bossSpriteRenderer.color;
            color.a = 1f;
            _bossSpriteRenderer.color = color;
        }
        
        if (GameConstants.DEBUG_MODE)
        {
            Debug.Log("[BossController] Reset");
        }
    }
    
    // ===== Getter =====
    public float CurrentHP => _currentHP;
    public float MaxHP => _maxHP;
    public float HPPercentage => _currentHP / _maxHP;
    public BossState State => _bossState;
}
