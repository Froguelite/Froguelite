using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float maxHealth = 20f;
    private float currentHealth;
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private EnemyMovementType movementType;

    [Header("Effects")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float knockbackDuration = 0.1f; // How long knockback lasts
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FlipbookAnimator defeatSmokeAnimator;
    private float flashDuration = 0.2f; // How long the red flash lasts
    private Color flashColor = Color.red; // Color to flash when hit

    public bool isKnockedBack { get; private set; } = false; // Tracks if enemy is currently being knocked back
    private Color originalColor; // Store the original sprite color

    private Room parentRoom;
    public bool engagedWithPlayer { get; private set; } = false;


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
    public virtual void BeginPlayerChase()
    {
        engagedWithPlayer = true;
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
        currentHealth -= damageAmount;
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Flash red when hit
        FlashSprite();

        // Apply knockback if force is greater than 0
        if (knockbackForce > 0f)
        {
            ApplyKnockback(knockbackForce);
        }
    }


    // Kills this enemy
    public void Die()
    {
        if (parentRoom != null)
            parentRoom.OnEnemyDefeated(this);

        spriteRenderer.enabled = false;
        StopPlayerChase();
        defeatSmokeAnimator.Play(true);
            
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
        spriteRenderer.color = flashColor;
        LeanTween.value(spriteRenderer.gameObject, flashColor, originalColor, flashDuration).setOnUpdate((Color newColor) =>
        {
            spriteRenderer.color = newColor;
        });
    }


    #endregion


}
