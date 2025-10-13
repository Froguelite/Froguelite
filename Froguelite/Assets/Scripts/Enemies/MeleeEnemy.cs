using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemy : MonoBehaviour, IEnemy
{

    // MeleeEnemy is a basic enemy type that uses close-range attacks


    #region VARIABLES


    public enum EnemyMovementType
    {
        GetNear,
        FullChase,
        Circle
    }

    [Header("Stats")]
    [SerializeField] private float maxHealth = 20f;
    private float currentHealth;

    [Header("Navigation")]
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private EnemyMovementType movementType;
    [SerializeField] private float stoppingDistance = 2f; // Distance to stop from player for GetNear movement
    [SerializeField] private float circleRadius = 3f; // Radius for circle movement
    [SerializeField] private float circleSpeed = 2f; // Speed multiplier for circle movement

    [Header("Knockback")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float knockbackDuration = 0.1f; // How long knockback lasts

    [Header("Visual Effects")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> possibleSprites; // List of possible sprites for random selection
    [SerializeField] private float flashDuration = 0.2f; // How long the red flash lasts
    [SerializeField] private Color flashColor = Color.red; // Color to flash when hit

    private Transform navTarget;
    private float circleAngle = 0f; // Current angle for circle movement
    private bool isKnockedBack = false; // Tracks if enemy is currently being knocked back
    private Color originalColor; // Store the original sprite color

    private Room parentRoom;
    private bool engagedWithPlayer = false;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Initializes this enemy with its parent room
    public void InitializeEnemy(Room parentRoom)
    {
        this.parentRoom = parentRoom;
    }


    // Awake
    private void Awake()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;

        // Initialize health
        currentHealth = maxHealth;

        rb.bodyType = RigidbodyType2D.Kinematic;
        originalColor = spriteRenderer.color;

        // Assign a random sprite from the list if available
        if (possibleSprites != null && possibleSprites.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleSprites.Count);
            spriteRenderer.sprite = possibleSprites[randomIndex];
        }
    }


    // Update
    private void Update()
    {
        if (navAgent == null || navTarget == null) return;
        if (isKnockedBack) return;
        if (!engagedWithPlayer) return;

        switch (movementType)
        {
            case EnemyMovementType.GetNear:
                NavNearPlayer();
                break;

            case EnemyMovementType.FullChase:
                NavFullChase();
                break;

            case EnemyMovementType.Circle:
                NavCirclePlayer();
                break;
        }

        // Update sprite flip based on direction to target
        UpdateSpriteFlip();
    }


    #endregion


    #region PLAYER CHASE


    public void BeginPlayerChase()
    {
        // Implementation for beginning player chase
        if (PlayerMovement.Instance != null)
        {
            navTarget = PlayerMovement.Instance.transform;
            engagedWithPlayer = true;
        }
    }


    #endregion


    #region NAVIGATION METHODS


    // Moves towards the player but stops at a certain distance
    private void NavNearPlayer()
    {
        if (PlayerMovement.Instance == null) return;

        Vector3 playerPosition = PlayerMovement.Instance.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);

        // If we're too far, move closer
        if (distanceToPlayer > stoppingDistance)
        {
            navAgent.SetDestination(playerPosition);
        }
        else
        {
            // Stop moving if we're close enough
            navAgent.SetDestination(transform.position);
        }
    }


    // Continuously chases the player directly
    private void NavFullChase()
    {
        if (PlayerMovement.Instance == null) return;

        Vector3 playerPosition = PlayerMovement.Instance.transform.position;
        navAgent.SetDestination(playerPosition);
    }


    // Circles around the player at a set radius
    private void NavCirclePlayer()
    {
        if (PlayerMovement.Instance == null) return;

        Vector3 playerPosition = PlayerMovement.Instance.transform.position;

        // Increment the circle angle based on time and speed
        circleAngle += circleSpeed * Time.deltaTime;

        // Calculate the circle position around the player
        Vector3 circleOffset = new Vector3(
            Mathf.Cos(circleAngle) * circleRadius,
            Mathf.Sin(circleAngle) * circleRadius,
            0f
        );

        Vector3 targetPosition = playerPosition + circleOffset;
        navAgent.SetDestination(targetPosition);
    }


    #endregion


    #region DAMAGE AND DEATH


    // Damages this enemy
    public void DamageEnemy(float damageAmount, float knockbackForce)
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
            
        Destroy(gameObject);
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


    // Updates sprite flip based on horizontal direction to target
    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null || navTarget == null) return;

        // Get the direction to the target
        Vector3 directionToTarget = navTarget.position - transform.position;
        
        // Flip sprite based on horizontal direction
        if (directionToTarget.x < 0)
        {
            // Target is to the left, face left
            spriteRenderer.flipX = false;
        }
        else if (directionToTarget.x > 0)
        {
            // Target is to the right, face right
            spriteRenderer.flipX = true;
        }
        // If directionToTarget.x == 0, keep current facing direction
    }


    #endregion


}
