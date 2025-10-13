using System.Collections;
using UnityEngine;

public class AOECircleFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private float fadeTime;
    private float holdTime;

    public void Initialize(float fadeDuration, float holdDuration)
    {
        sr = GetComponent<SpriteRenderer>();
        fadeTime = fadeDuration;
        holdTime = holdDuration;
        StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        float t = 0f;

        // Fade in
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeTime);
            sr.color = new Color(1f, 0f, 0f, alpha * 0.6f);
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(t / fadeTime);
            sr.color = new Color(1f, 0f, 0f, alpha * 0.6f);
            yield return null;
        }

        Destroy(gameObject);
    }
}
