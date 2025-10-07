using UnityEngine;
using System.Collections;

public class PowerFly : MonoBehaviour, ICollectable
{

    // PowerFly handles a single power fly's display, collection, and effect applying


    #region VARIABLES


    public PowerFlyData powerFlyData;
    [SerializeField] private SpriteRenderer spriteRenderer;
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
    private Coroutine buzzCoroutine;
    
    private LTDescr bobTween;
    private LTDescr rotationTween;


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
    public void SetupFly(PowerFlyData powerFlyData)
    {
        this.powerFlyData = powerFlyData;
        SetupFly();
    }


    // Sets up this power fly based on already assigned power fly data
    public void SetupFly()
    {
        spriteRenderer.sprite = powerFlyData.displayImg;
        originalPosition = transform.position;
        StartBuzzing();
        StartBobbingAndRotation();
    }
    

    void OnDestroy()
    {
        StopBuzzing();
        StopBobbingAndRotation();
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
        if (itemDef != null)
        {
            InventoryManager.Instance.AddItem(itemDef, 1);
        }

        InventoryManager.Instance.AddPowerFly(powerFlyData);
        PowerFlyFactory.Instance.MarkPowerFlyAsCollected(powerFlyData);
        StopBuzzing();
        StopBobbingAndRotation();

        foreach (PowerFlyEffect effect in powerFlyData.effects)
        {
            effect.ApplyEffect();
        }

        CollectionOverlayHandler.Instance.ShowPowerFlyCollected(powerFlyData);
        Destroy(gameObject);
    }


    #endregion


}
