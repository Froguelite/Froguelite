using UnityEngine;
using System.Collections;

public class PowerFly : MonoBehaviour, ICollectable
{

    // PowerFly handles a single power fly's display, collection, and effect applying


    #region VARIABLES


    public PowerFlyData powerFlyData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer shadowSpriteRenderer;
    [SerializeField] private Transform buzzOffset;
    [SerializeField] private bool setupOnStart = false;
    
    [Header("Buzzing Behavior")]
    [SerializeField] private float buzzRadius = 0.5f;
    [SerializeField] private float directionChangeInterval = 0.3f;
    
    [Header("LeanTween Animation")]
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobDuration = 1.5f;
    [SerializeField] private float rotationAmount = 15f;
    [SerializeField] private float rotationDuration = 2f;
    [SerializeField] private ItemDefinition itemDef;
    
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    
    // Pre-collection callback for totem choice system
    public System.Action<PowerFly> onPreCollect;
    private Coroutine buzzCoroutine;
    
    private LTDescr bobTween;
    private LTDescr rotationTween;

    private bool canCollect = true;
    private bool hasBeenCollected = false;

    private bool isCapsuleFly = false;
    
    private Coroutine manualMoveCoroutine;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Start()
    {
        if (setupOnStart && powerFlyData != null)
        {
            SetupFly();
        }
    }


    // Sets up this power fly based on given power fly data
    public void SetupFly(PowerFlyData powerFlyData, bool isCapsuleFly = false)
    {
        this.powerFlyData = powerFlyData;
        SetupFly(isCapsuleFly);
    }


    // Sets up this power fly based on already assigned power fly data
    public void SetupFly(bool isCapsuleFly = false)
    {
        spriteRenderer.sprite = powerFlyData.displayImg;
        originalPosition = transform.position;

        rotationDuration *= Random.Range(0.8f, 1.2f);
        bobDuration *= Random.Range(0.8f, 1.2f);

        if (isCapsuleFly)
        {
            this.isCapsuleFly = true;
            buzzRadius *= 2.75f;
            directionChangeInterval *= 2.75f;
            shadowSpriteRenderer.enabled = false;
        }

        StartBuzzing();
        StartBobbingAndRotation();
    }


    void OnDestroy()
    {
        StopBuzzing();
        StopBobbingAndRotation();
    }
    

    public void SetCanCollect(bool value)
    {
        canCollect = value;
    }


    #endregion


    #region BUZZING BEHAVIOR


    // Starts the buzzing coroutine to make the fly move around randomly
    public void StartBuzzing()
    {
        if (buzzCoroutine == null)
        {
            buzzCoroutine = StartCoroutine(BuzzAround());
        }
    }
    
    // Stops the buzzing coroutine
    public void StopBuzzing()
    {
        if (buzzCoroutine != null)
        {
            StopCoroutine(buzzCoroutine);
            buzzCoroutine = null;
        }
    }
    
    // Coroutine that continuously moves the fly around in a buzzing pattern
    private IEnumerator BuzzAround()
    {
        while (true)
        {
            // Generate a random position within the buzz radius
            Vector2 randomDirection = Random.insideUnitCircle * buzzRadius;
            targetPosition = originalPosition + new Vector3(randomDirection.x, randomDirection.y, 0);
            
            // Move towards the target position
            float elapsedTime = 0f;
            Vector3 startPosition = buzzOffset.position;
            
            while (elapsedTime < directionChangeInterval)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / directionChangeInterval;
                
                // Use smoothstep for more natural fly-like movement
                t = t * t * (3f - 2f * t);
                
                buzzOffset.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            
            // Small random pause to make it feel more organic
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }


    #endregion


    #region MANUAL MOVEMENT


    /// <summary>
    /// Manually moves the fly to a target position over a specified duration, passing through a midpoint.
    /// Stops buzzing movement but keeps bobbing and rotation active.
    /// Uses a quadratic Bezier curve for smooth movement.
    /// </summary>
    /// <param name="endPosition">The final destination position</param>
    /// <param name="midpoint">A point the fly must pass through during movement</param>
    /// <param name="duration">Time in seconds for the complete movement</param>
    public void ManualMoveToPosition(Vector3 endPosition, Vector3 midpoint, float duration)
    {
        // Stop any existing manual movement
        if (manualMoveCoroutine != null)
        {
            StopCoroutine(manualMoveCoroutine);
            manualMoveCoroutine = null;
        }
        
        // Stop buzzing movement
        StopBuzzing();
        
        // Start the manual movement coroutine
        manualMoveCoroutine = StartCoroutine(ManualMoveCoroutine(endPosition, midpoint, duration));
    }
    
    /// <summary>
    /// Coroutine that handles the smooth curved movement through the midpoint to the end position.
    /// Uses quadratic Bezier curve interpolation for smooth transitions.
    /// </summary>
    private IEnumerator ManualMoveCoroutine(Vector3 endPosition, Vector3 midpoint, float duration)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Apply ease-in-out for smoother acceleration/deceleration
            float smoothT = t * t * (3f - 2f * t);
            
            // Calculate position using quadratic Bezier curve
            // B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
            // where P₀ = start, P₁ = midpoint, P₂ = end
            Vector3 newPosition = QuadraticBezier(startPosition, midpoint, endPosition, smoothT);
            
            transform.position = newPosition;
            
            yield return null;
        }
        
        // Ensure we end exactly at the target position
        transform.position = endPosition;
        
        // Update original position so if buzzing restarts, it buzzes around the new position
        originalPosition = endPosition;
        
        manualMoveCoroutine = null;
    }
    
    /// <summary>
    /// Calculates a point on a quadratic Bezier curve.
    /// </summary>
    /// <param name="p0">Start point</param>
    /// <param name="p1">Control point (midpoint)</param>
    /// <param name="p2">End point</param>
    /// <param name="t">Interpolation value between 0 and 1</param>
    /// <returns>Position on the curve at parameter t</returns>
    private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector3 point = uu * p0; // (1-t)² * P₀
        point += 2 * u * t * p1; // 2(1-t)t * P₁
        point += tt * p2;        // t² * P₂
        
        return point;
    }


    #endregion


    #region LEANTWEEN ANIMATIONS


    // Starts the bobbing and rotation animations using LeanTween
    public void StartBobbingAndRotation()
    {
        // Start bobbing animation (up and down movement)
        bobTween = LeanTween.moveLocalY(spriteRenderer.gameObject, spriteRenderer.transform.localPosition.y + bobHeight, bobDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
        
        // Start rotation animation (gentle swaying)
        rotationTween = LeanTween.rotateZ(spriteRenderer.gameObject, rotationAmount, rotationDuration)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();
    }
    
    // Stops the bobbing and rotation animations
    public void StopBobbingAndRotation()
    {
        if (bobTween != null)
        {
            LeanTween.cancel(bobTween.uniqueId);
            bobTween = null;
        }
        
        if (rotationTween != null)
        {
            LeanTween.cancel(rotationTween.uniqueId);
            rotationTween = null;
        }
    }


    #endregion


    #region COLLECTION


    // On collect, apply the effect and destroy the game object
    public void OnCollect()
    {
        Debug.Log("TRING TO COLLECT, canCollect=" + canCollect);
        if (!canCollect) return;
        
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;
        
        // Invoke pre-collection callback (for totem choice system)
        onPreCollect?.Invoke(this);
        
        // Immediately disable collider and hide all visuals
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        // Disable all sprite renderers (main sprite + shadow)
        SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in allSprites)
        {
            sprite.enabled = false;
        }

        CollectionOverlayHandler.Instance.ShowPowerFlyCollected(powerFlyData, isCapsuleFly);
        
        StopBuzzing();
        StopBobbingAndRotation();

        if (isCapsuleFly)
        {
            AudioManager.Instance.PlaySound(FlySlotsSound.FlySlotsCollect);
        }
        else
        {
            AudioManager.Instance.PlaySound(CollectibleSound.PowerFlyCollect);
        }

        if (!isCapsuleFly)
        {
            InventoryManager.Instance.AddPowerFly(powerFlyData);
            PowerFlyFactory.Instance.MarkPowerFlyAsCollected(powerFlyData);

            if (itemDef != null)
            {
                InventoryManager.Instance.AddItem(itemDef, 1);
            }
            
            foreach (PowerFlyEffect effect in powerFlyData.effects)
            {
                effect.ApplyEffect();
            }
        }

        Destroy(gameObject);
    }


    #endregion


}
