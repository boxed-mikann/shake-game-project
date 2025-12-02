using UnityEngine;
using UnityEngine.UI;

public class DeviceIcon : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private GameObject hitEffectObject; // CFXR3 Hit Misc A（UI版では省略可）
    private JudgePopup judgePopup; // 子オブジェクトの判定ポップアップ
    
    private string deviceId;
    
    void Awake()
    {
        // どちらか存在する方を使う（Canvas上はImage、ワールド空間はSpriteRenderer）
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        // 子オブジェクトから CFXR エフェクトと JudgePopup を取得
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                
                // エフェクトを探す（ParticleSystem）
                if (hitEffectObject == null && child.GetComponent<ParticleSystem>() != null)
                {
                    hitEffectObject = child;
                    Debug.Log("[DeviceIcon] Hit effect object found: " + hitEffectObject.name);
                }
                
                // JudgePopup を探す
                if (judgePopup == null && child.GetComponent<JudgePopup>() != null)
                {
                    judgePopup = child.GetComponent<JudgePopup>();
                    Debug.Log("[DeviceIcon] JudgePopup found: " + judgePopup.gameObject.name);
                }
            }
        }
    }
    
    public void Initialize(string id, Sprite sprite)
    {
        deviceId = id;
        if (uiImage != null)
        {
            uiImage.sprite = sprite;
            uiImage.enabled = true;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
        }
    }
    
    public string GetDeviceId() => deviceId;
    
    /// <summary>
    /// シェイク入力時に呼び出される - エフェクトと判定表示を実行
    /// </summary>
    /// <param name="judgement">判定タイプ</param>
    public void OnShakeProcessed(JudgeManagerV2.JudgementType judgement = JudgeManagerV2.JudgementType.Good)
    {
        PlayHitEffect();
        ShowJudgement(judgement);
    }
    
    private void PlayHitEffect()
    {
        if (hitEffectObject != null)
        {
            // ParticleSystem で再生制御
            var particleSystem = hitEffectObject.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                Debug.Log("[DeviceIcon] Playing CFXR particle effect for deviceId: " + deviceId);
                particleSystem.Stop();
                particleSystem.Play();
            }
            return;
        }
        
        // UI版など、エフェクトが無い場合は軽いフィードバック（スケールパンチ）
        var t = transform;
        if (t is RectTransform)
        {
            Debug.Log("[DeviceIcon] Playing UIScalePunch for deviceId: " + deviceId);
            // 簡易スケールアニメーション
            StopAllCoroutines();
            StartCoroutine(UIScalePunch((RectTransform)t));
        }
    }

    private void ShowJudgement(JudgeManagerV2.JudgementType judgement)
    {
        if (judgePopup != null)
        {
            Debug.Log($"[DeviceIcon] Showing judgement popup: {judgement}");
            judgePopup.Show(JudgeManagerV2.GetJudgementText(judgement));
        }
        else
        {
            Debug.LogWarning("[DeviceIcon] JudgePopup not found");
        }
    }

    private System.Collections.IEnumerator UIScalePunch(RectTransform rt)
    {
        Vector3 baseScale = rt.localScale;
        Vector3 up = baseScale * 1.1f;
        float t = 0f;
        while (t < 0.05f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.Lerp(baseScale, up, t / 0.05f);
            yield return null;
        }
        t = 0f;
        while (t < 0.08f)
        {
            t += Time.unscaledDeltaTime;
            rt.localScale = Vector3.Lerp(up, baseScale, t / 0.08f);
            yield return null;
        }
        rt.localScale = baseScale;
    }
}
