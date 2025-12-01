using UnityEngine;
using UnityEngine.UI;

public class DeviceIcon : MonoBehaviour, IShakeable
{
    private SpriteRenderer spriteRenderer;
    private Image uiImage;
    private GameObject hitEffectObject; // CFXR3 Hit Misc A（UI版では省略可）
    
    private string deviceId;
    
    void Awake()
    {
        // どちらか存在する方を使う（Canvas上はImage、ワールド空間はSpriteRenderer）
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        
        // エフェクトは子オブジェクトから取得（存在しない場合はnullのままでOK）
        if (transform.childCount > 0)
        {
            hitEffectObject = transform.GetChild(0).gameObject; // CFXR3 Hit Misc A
            //デバッグログ
            Debug.Log("[DeviceIcon] Hit effect object found: " + hitEffectObject.name);
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
        
        // // エフェクトを初期化
        // if (hitEffectObject != null)
        // {
        //     hitEffectObject.SetActive(false);
        // }
    }
    
    public string GetDeviceId() => deviceId;
    
    // ★ ShakeResolverV2から直接呼び出される
    public void OnShakeProcessed()
    {
        PlayHitEffect();
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
            else
            {
                // ParticleSystem がない場合は SetActive で再生
                Debug.Log("[DeviceIcon] No ParticleSystem found, using SetActive for deviceId: " + deviceId);
                // hitEffectObject.SetActive(false);
                // hitEffectObject.SetActive(true);
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
