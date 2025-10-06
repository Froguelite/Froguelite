using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TongueBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D tongueCollider;      // long BoxCollider2D set as trigger
    [SerializeField] private SpriteRenderer visualSprite;    // optional visual to scale/flip

    [Header("Timing")]
    [SerializeField] private float extendTime = 0.18f;
    [SerializeField] private float retractTime = 0.12f;
    [SerializeField] private float holdDefault = 1.0f;

    [Header("Sizing")]
    [SerializeField] private float islandLength = 12f;       // total length to cover island
    [SerializeField] private float thickness = 0.5f;         // collider thickness

    private void Reset()
    {
        tongueCollider = GetComponent<Collider2D>();
        visualSprite = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (tongueCollider != null) tongueCollider.enabled = false;
    }

    // Extend the tongue aligned to island axis, hold, then retract.
    // horizontal = true -> align along X, else align along Y
    public IEnumerator ExtendAndHold(bool horizontal, float holdOverride = -1f)
    {
        float hold = holdOverride > 0f ? holdOverride : holdDefault;

        // Setup transform orientation and scale so collider covers island length
        if (horizontal)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            transform.localScale = new Vector3(islandLength, thickness, 1f);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            transform.localScale = new Vector3(islandLength, thickness, 1f);
        }

        // Optional visual extend animation (scale from small to full)
        if (visualSprite != null)
        {
            visualSprite.drawMode = SpriteDrawMode.Sliced;
            visualSprite.size = new Vector2(0.01f, thickness);
            float t = 0f;
            while (t < extendTime)
            {
                t += Time.deltaTime;
                float u = t / extendTime;
                visualSprite.size = new Vector2(Mathf.Lerp(0.01f, islandLength, u), thickness);
                yield return null;
            }
            visualSprite.size = new Vector2(islandLength, thickness);
        }
        else
        {
            yield return new WaitForSeconds(extendTime);
        }

        // Enable collider for damage window
        if (tongueCollider != null) tongueCollider.enabled = true;
        yield return new WaitForSeconds(hold);

        // Disable collider and play retraction animation
        if (tongueCollider != null) tongueCollider.enabled = false;

        if (visualSprite != null)
        {
            float t2 = 0f;
            while (t2 < retractTime)
            {
                t2 += Time.deltaTime;
                float u = 1f - (t2 / retractTime);
                visualSprite.size = new Vector2(Mathf.Lerp(0.01f, islandLength, Mathf.Clamp01(u)), thickness);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(retractTime);
        }
    }
}
