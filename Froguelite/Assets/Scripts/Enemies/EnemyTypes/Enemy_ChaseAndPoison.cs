using System.Collections;
using UnityEngine;

public class Enemy_ChaseAndPoison : EnemyBase
{

    // Enemy_ChaseAndPoison chases the player, slows down, warns with a sprite, then releases poison damage.


    #region VARIABLES


    [Header("Chase and Poison Settings")]
    [SerializeField] private EnemyBehavior_Chase chaseBehavior;
    [SerializeField] private SpriteRenderer warningSpriteRenderer;
    [SerializeField] private ParticleSystem poisonParticles;
    [SerializeField] private CircleCollider2D poisonCollider;
    [SerializeField] private Transform spriteTransform;
    
    [Header("Hovering Animation")]
    [SerializeField] private float hoverSpeed = 1f;
    [SerializeField] private float hoverRangeX = 0.15f;
    [SerializeField] private float hoverRangeY = 0.2f;
    
    [Header("Timing")]
    [SerializeField] private float chaseTime = 3f;
    [SerializeField] private float slowdownDuration = 1f;
    [SerializeField] private float warningDuration = 1.5f;
    
    [Header("Speed Settings")]
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private float slowedSpeed = 0.5f;
    
    [Header("Poison Settings")]
    [SerializeField] private float poisonRadius = 2f;
    [SerializeField] private int poisonDamage = 2;
    [SerializeField] private float poisonDelayBeforeDamage = 0.25f;
    [SerializeField] private float poisonActiveDuration = 1f;
    
    private bool isPoisonActive = false;
    private Vector3 originalSpriteLocalPosition;
    private float currentHoverAngle = 0f;


    #endregion


    #region SETUP


    protected override void Awake()
    {
        base.Awake();
        
        // Store original sprite position for hovering animation
        if (spriteTransform != null)
        {
            originalSpriteLocalPosition = spriteTransform.localPosition;
        }
        
        // Make sure warning sprite is hidden at start
        if (warningSpriteRenderer != null)
        {
            warningSpriteRenderer.enabled = false;
        }
        
        // Disable poison collider at start
        if (poisonCollider != null)
        {
            poisonCollider.enabled = false;
        }
    }


    #endregion


    #region ENEMYBASE OVERRIDES


    // On begin player chase, start the chase and poison routine
    protected override void OnEngagePlayer()
    {
        base.OnEngagePlayer();
        
        if (chaseBehavior != null && PlayerMovement.Instance != null)
        {
            chaseBehavior.BeginChase(PlayerMovement.Instance.transform);
            StartCoroutine(ChaseAndPoisonRoutine());
            StartHoveringAnimation();
        }
    }


    public override void Die()
    {
        StopAllCoroutines();
        
        // Stop hovering animation
        if (spriteTransform != null)
        {
            LeanTween.cancel(spriteTransform.gameObject);
        }
        
        // Clean up warning sprite and particles
        if (warningSpriteRenderer != null)
        {
            warningSpriteRenderer.enabled = false;
        }
        
        if (poisonParticles != null && poisonParticles.isPlaying)
        {
            poisonParticles.Stop();
        }
        
        base.Die();
    }


    // Override to restart hovering animation after knockback ends
    public override void DamageEnemy(float damageAmount, float knockbackForce)
    {
        base.DamageEnemy(damageAmount, knockbackForce);
        
        // Restart hovering animation after knockback
        if (!isDead && knockbackForce > 0f)
        {
            StartCoroutine(RestartHoveringAfterKnockback());
        }
    }


    // Waits for knockback to finish, then restarts hovering animation
    private IEnumerator RestartHoveringAfterKnockback()
    {
        // Wait until knockback is finished
        while (isKnockedBack)
        {
            yield return null;
        }
        
        // Restart the hovering animation
        StartHoveringAnimation();
    }


    #endregion


    #region HOVERING ANIMATION


    // Starts the smooth hovering animation for the butterfly sprite
    private void StartHoveringAnimation()
    {
        if (spriteTransform == null)
            return;
        
        // Cancel any existing tweens on the sprite
        LeanTween.cancel(spriteTransform.gameObject);
        
        // Calculate current angle based on sprite's actual position to avoid snapping
        Vector3 currentOffset = spriteTransform.localPosition - originalSpriteLocalPosition;
        
        // If sprite is at or near original position, use current stored angle or random
        if (currentOffset.magnitude < 0.01f)
        {
            // First time or reset - use random or stored angle
            if (currentHoverAngle == 0f)
            {
                currentHoverAngle = Random.Range(0f, Mathf.PI * 2f);
            }
        }
        else
        {
            // Calculate angle from current position to continue smoothly
            // For X component: offsetX = Sin(angle) * hoverRangeX
            // So: angle = arcsin(offsetX / hoverRangeX)
            float normalizedX = Mathf.Clamp(currentOffset.x / hoverRangeX, -1f, 1f);
            currentHoverAngle = Mathf.Asin(normalizedX);
            
            // Determine correct quadrant based on Y offset
            float expectedY = Mathf.Sin(currentHoverAngle * 2f) * hoverRangeY;
            if (Mathf.Abs(currentOffset.y - expectedY) > 0.01f)
            {
                // We're in the other half of the sine wave
                currentHoverAngle = Mathf.PI - currentHoverAngle;
            }
        }
        
        // Create a looping figure-8 or circular hovering motion
        // Using LeanTween with a custom update function for smooth 2D hovering
        LeanTween.value(spriteTransform.gameObject, currentHoverAngle, currentHoverAngle + (Mathf.PI * 2f), hoverSpeed)
            .setOnUpdate((float angle) => {
                if (spriteTransform != null)
                {
                    currentHoverAngle = angle;
                    float offsetX = Mathf.Sin(angle) * hoverRangeX;
                    float offsetY = Mathf.Sin(angle * 2f) * hoverRangeY; // Double frequency for figure-8 pattern
                    spriteTransform.localPosition = originalSpriteLocalPosition + new Vector3(offsetX, offsetY, 0f);
                }
            })
            .setLoopClamp();
    }


    #endregion


    #region CHASE AND POISON ROUTINE


    // Main coroutine that loops the chase and poison behavior
    private IEnumerator ChaseAndPoisonRoutine()
    {
        while (!isDead && engagedWithPlayer)
        {
            // Step 1: Chase at normal speed
            chaseBehavior.Speed = normalSpeed;
            yield return new WaitForSeconds(chaseTime);
            
            if (isDead) yield break;
            
            // Step 2: Slow down gradually
            yield return StartCoroutine(SlowdownCoroutine());
            
            if (isDead) yield break;
            
            // Step 3: Stop and show warning
            chaseBehavior.stopChaseOverride = true;
            navAgent.isStopped = true;
            
            // Flash warning sprite to indicate impending poison attack
            yield return StartCoroutine(FlashWarningSprite());
            
            if (isDead) yield break;
            
            // Step 4: Hide warning and release poison
            if (warningSpriteRenderer != null)
            {
                warningSpriteRenderer.enabled = false;
            }
            
            yield return StartCoroutine(ReleasePoisonBurst());
            
            if (isDead) yield break;
            
            // Step 5: Resume chase
            chaseBehavior.stopChaseOverride = false;
            navAgent.isStopped = false;
        }
    }


    // Gradually slows down the enemy's movement speed
    private IEnumerator SlowdownCoroutine()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < slowdownDuration)
        {
            float t = elapsedTime / slowdownDuration;
            chaseBehavior.Speed = Mathf.Lerp(normalSpeed, slowedSpeed, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end at exactly the slowed speed
        chaseBehavior.Speed = slowedSpeed;
    }


    // Flashes the warning sprite to alert the player of impending poison attack
    private IEnumerator FlashWarningSprite()
    {
        if (warningSpriteRenderer == null)
        {
            yield return new WaitForSeconds(warningDuration);
            yield break;
        }
        
        // Enable the sprite renderer
        warningSpriteRenderer.enabled = true;
        
        // Calculate time per flash cycle (fade in + fade out)
        int flashCount = 3;
        float timePerFlash = warningDuration / flashCount;
        float fadeTime = timePerFlash / 2f; // Half for fade in, half for fade out
        
        Color spriteColor = warningSpriteRenderer.color;
        spriteColor.a = 0f;
        warningSpriteRenderer.color = spriteColor;
        
        // Flash multiple times
        for (int i = 0; i < flashCount; i++)
        {
            // Fade in to 50%
            LeanTween.cancel(warningSpriteRenderer.gameObject);
            Color targetColor = warningSpriteRenderer.color;
            targetColor.a = 0.5f;
            
            LeanTween.value(warningSpriteRenderer.gameObject, 0f, 0.5f, fadeTime)
                .setOnUpdate((float val) => {
                    Color c = warningSpriteRenderer.color;
                    c.a = val;
                    warningSpriteRenderer.color = c;
                });
            
            yield return new WaitForSeconds(fadeTime);
            
            // Fade out to 0%
            LeanTween.cancel(warningSpriteRenderer.gameObject);
            LeanTween.value(warningSpriteRenderer.gameObject, 0.5f, 0f, fadeTime)
                .setOnUpdate((float val) => {
                    Color c = warningSpriteRenderer.color;
                    c.a = val;
                    warningSpriteRenderer.color = c;
                });
            
            yield return new WaitForSeconds(fadeTime);
        }
    }


    // Releases the poison burst, playing particles and enabling damage zone over time
    private IEnumerator ReleasePoisonBurst()
    {
        // Play poison particle effect
        if (poisonParticles != null)
        {
            poisonParticles.Play();
        }
        
        // Wait for initial delay before damage becomes active
        yield return new WaitForSeconds(poisonDelayBeforeDamage);
        
        // Enable poison damage zone
        isPoisonActive = true;
        if (poisonCollider != null)
        {
            poisonCollider.enabled = true;
        }
        
        // Keep damage active for the specified duration
        yield return new WaitForSeconds(poisonActiveDuration);
        
        // Disable poison damage zone
        isPoisonActive = false;
        if (poisonCollider != null)
        {
            poisonCollider.enabled = false;
        }
        
        // Stop poison particles
        if (poisonParticles != null)
        {
            poisonParticles.Stop();
        }
        
        // Wait a moment before resuming chase
        yield return new WaitForSeconds(0.5f);
    }


    #endregion


    #region POISON DAMAGE COLLISION


    // Triggered when player enters the poison zone
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isPoisonActive || isDead)
            return;
        
        if (collision.CompareTag("Player"))
        {
            // Don't damage player if they are dashing
            if (PlayerMovement.Instance.IsDashing)
                return;
            
            StatsManager.Instance.playerHealth.DamagePlayer(poisonDamage);
        }
    }


    // Triggered while player stays in the poison zone
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isPoisonActive || isDead)
            return;
        
        if (collision.CompareTag("Player"))
        {
            // Don't damage player if they are dashing
            if (PlayerMovement.Instance.IsDashing)
                return;
            
            // Damage is handled by PlayerHealth's cooldown system
            StatsManager.Instance.playerHealth.DamagePlayer(poisonDamage);
        }
    }


    #endregion


    #region DEBUG


    // Visualize the poison radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, poisonRadius);
    }


    #endregion


}
