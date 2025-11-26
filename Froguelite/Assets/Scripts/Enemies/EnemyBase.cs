using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyBase : MonoBehaviour, IEnemy
{

    // EnemyBase is a base class for enemy types, providing common functionality like health, nav, knockback, and visuals.


    #region VARIABLES


    public enum EnemyMovementType
    {
        Walking,
        Flying,
    }

    [SerializeField] private bool chaseOnStart = false;

    [Header("Stats & Navigation")]
    [SerializeField] private string enemyId = "EnemyBase";
    public string GetEnemyId() { return enemyId; }
    [SerializeField] private float maxHealth = 20f;
    private float currentHealth;
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] private EnemyMovementType movementType;
    [SerializeField] private bool isMiniboss = false;

    [Header("Effects")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private bool useKnockback = true;
    [SerializeField] private float knockbackDuration = 0.1f; // How long knockback lasts
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] private FlipbookAnimator defeatSmokeAnimator;
    [SerializeField] private AudioClip deathSound;
    private float flashDuration = 0.2f; // How long the red flash lasts
    private Color flashColor = Color.red; // Color to flash when hit

    public bool isKnockedBack { get; private set; } = false; // Tracks if enemy is currently being knocked back
    private Color originalColor; // Store the original sprite color

    [SerializeField] private ParticleSystem sickParticles; // Particle system that plays while poisoned
    public bool isPoisoned { get; private set; } = false; // Tracks if enemy is currently poisoned
    private Coroutine poisonCoroutine; // Reference to active poison coroutine
    private Color poisonTintMultiplier = new Color(0.6f, 1f, 0.6f, 1f); // Multiplier for poison tint (slightly green)

    public Room parentRoom { get; private set; } = null; // The room this enemy belongs to
    public bool engagedWithPlayer { get; private set; } = false;
    public bool isDead { get { return currentHealth <= 0f; } }
    [HideInInspector]
    public UnityEvent onDeathEvent;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Initializes this enemy with its parent room
    public virtual void InitializeEnemy(Room parentRoom)
    {
        this.parentRoom = parentRoom;
    }


    // Awake
    protected virtual void Awake()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;

        // Initialize health
        currentHealth = maxHealth;

        rb.bodyType = RigidbodyType2D.Kinematic;
        originalColor = spriteRenderer.color;
    }


    // Start
    protected virtual void Start()
    {
        if (chaseOnStart)
        {
            BeginPlayerChase();
        }
    }


    // Called when the chase with the player begins (i.e. enter room or similar)
    public void BeginPlayerChase()
    {
        GetComponent<EnemyAlertAnimator>()?.TriggerAlert();

        StartCoroutine(WaitThenEngagePlayer());
    }


    private IEnumerator WaitThenEngagePlayer()
    {
        yield return new WaitForSeconds(1f); // Wait for alert animation to finish

        engagedWithPlayer = true;
        OnEngagePlayer();
    }


    protected virtual void OnEngagePlayer()
    {
        navAgent.enabled = true;
    }


    // Called when the chase with the player ends (i.e. death or similar)
    public virtual void StopPlayerChase()
    {
        engagedWithPlayer = false;
        navAgent.enabled = false;
    }


    #endregion


    #region DAMAGE AND DEATH


    // Damages this enemy
    public virtual void DamageEnemy(float damageAmount, float knockbackForce)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Flash red when hit
        FlashSprite();

        // Apply knockback if force is greater than 0
        if (knockbackForce > 0f && useKnockback && engagedWithPlayer)
        {
            ApplyKnockback(knockbackForce);
        }
    }


    // Kills this enemy
    public virtual void Die()
    {
        if (parentRoom != null)
            parentRoom.OnEnemyDefeated(this);
        onDeathEvent?.Invoke();

        // Clean up poison if active
        if (isPoisoned)
        {
            RemovePoison();
        }

        spriteRenderer.enabled = false;
        StopPlayerChase();
        defeatSmokeAnimator.Play(true);
        AudioManager.Instance.PlaySound(deathSound);

        if (isMiniboss)
        {
            if (parentRoom == null)
                InventoryManager.Instance.SpewGoldenFlies(transform.position, 1);
            else if (parentRoom.enemies.Count == 0)
                InventoryManager.Instance.SpewGoldenFlies(transform.position, 1);
        }
            
        Destroy(gameObject, 1f);
    }


    #endregion


    #region KNOCKBACK AND EFFECTS


    // Applies knockback force away from the player
    public void ApplyKnockback(float knockbackForce)
    {
        if (rb == null || PlayerMovement.Instance == null) return;

        // Calculate direction away from player
        Vector3 playerPosition = PlayerMovement.Instance.transform.position;
        Vector2 knockbackDirection = ((Vector2)transform.position - (Vector2)playerPosition).normalized;

        // Start knockback coroutine
        StartCoroutine(KnockbackCoroutine(knockbackDirection * knockbackForce));
    }


    // Coroutine to handle knockback physics and timing
    private System.Collections.IEnumerator KnockbackCoroutine(Vector2 knockbackVelocity)
    {
        // Disable NavMeshAgent and enable physics
        isKnockedBack = true;
        navAgent.enabled = false;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Apply the knockback force
        rb.linearVelocity = knockbackVelocity;

        // Wait for knockback duration
        yield return new WaitForSeconds(knockbackDuration);

        // Re-enable navigation and disable physics
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        navAgent.enabled = true;
        isKnockedBack = false;
    }


    // Flash the sprite red when taking damage
    private void FlashSprite()
    {
        LeanTween.cancel(spriteRenderer.gameObject);
        Color targetFlashColor = flashColor;
        Color targetOriginalColor = originalColor;
        
        // If poisoned, blend the flash and original colors with poison tint
        if (isPoisoned)
        {
            targetFlashColor = MultiplyColors(flashColor, poisonTintMultiplier);
            targetOriginalColor = MultiplyColors(originalColor, poisonTintMultiplier);
        }
        
        spriteRenderer.color = targetFlashColor;
        LeanTween.value(spriteRenderer.gameObject, targetFlashColor, targetOriginalColor, flashDuration).setOnUpdate((Color newColor) =>
        {
            spriteRenderer.color = newColor;
        });
    }
    
    // Multiplies two colors component-wise for blending effects
    private Color MultiplyColors(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }


    #endregion


    #region POISON


    // Applies poison to this enemy
    public void ApplyPoison(float duration, float damagePerSecond)
    {
        // If already poisoned, stop existing poison and restart with new duration
        if (isPoisoned)
        {
            RemovePoison();
        }

        isPoisoned = true;
        
        // Apply green tint multiplier to current color
        spriteRenderer.color = MultiplyColors(spriteRenderer.color, poisonTintMultiplier);
        
        // Start poison particles if assigned
        if (sickParticles != null)
        {
            sickParticles.Play();
        }

        // Start poison coroutine
        poisonCoroutine = StartCoroutine(PoisonCoroutine(duration, damagePerSecond));

    }


    // Coroutine to handle poison damage over time and visuals
    private IEnumerator PoisonCoroutine(float duration, float damagePerSecond)
    {
        float elapsedTime = 0f;
        float damageInterval = 1f; // Damage every second
        float nextDamageTime = 0f;

        while (elapsedTime < duration && !isDead)
        {
            elapsedTime += Time.deltaTime;

            // Apply damage every second
            if (elapsedTime >= nextDamageTime)
            {
                DamageEnemy(damagePerSecond, 0f); // Apply poison damage without knockback
                nextDamageTime += damageInterval;
            }

            yield return null;
        }

        // Poison duration ended, remove it
        RemovePoison();
    }


    // Removes poison effect and restores original color
    private void RemovePoison()
    {
        if (!isPoisoned) return;

        isPoisoned = false;

        // Stop poison coroutine if running
        if (poisonCoroutine != null)
        {
            StopCoroutine(poisonCoroutine);
            poisonCoroutine = null;
        }

        // Restore original sprite color
        spriteRenderer.color = originalColor;
        
        // Stop poison particles if assigned
        if (sickParticles != null)
        {
            sickParticles.Stop();
        }
    }


    #endregion


}
