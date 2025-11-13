 using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{

    public static PlayerAnimationController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FlipbookAnimator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Idle Animation")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private float idleFrameDuration = 0.15f;
    
    [Header("Moving Animation")]
    [SerializeField] private Sprite[] movingSprites;
    [SerializeField] private float movingFrameDuration = 0.1f;
    
    [Header("Attacking Animation")]
    [SerializeField] private Sprite[] attackingSprites;
    [SerializeField] private float attackingFrameDuration = 0.08f;
    
    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.1f;
    
    private Rigidbody2D rb;
    private Vector2 lastAimDirection = Vector2.right;
    private bool isAttacking = false;
    private AnimationState currentState = AnimationState.Idle;

    public bool overrideAnimations = false;

    private enum AnimationState
    {
        Idle,
        Moving,
        Attacking
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Auto-find components if not assigned
        if (animator == null) animator = GetComponent<FlipbookAnimator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Start with idle animation
        PlayAnimation(AnimationState.Idle);
    }
    
    void Update()
    {
        if (!isAttacking && !overrideAnimations)
        {
            UpdateMovementAnimation();
            UpdateFacingDirection();
        }
    }
    
    void UpdateMovementAnimation()
    {
        // Check if player is moving based on velocity
        bool isMoving = rb != null && rb.linearVelocity.magnitude > movementThreshold;
        
        AnimationState newState = isMoving ? AnimationState.Moving : AnimationState.Idle;
        
        if (newState != currentState)
        {
            PlayAnimation(newState);
        }
    }
    
    void UpdateFacingDirection()
    {
        // Base facing on movement direction when not attacking
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            // Only care about horizontal movement for facing
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                lastAimDirection.x = rb.linearVelocity.x;
            }
        }
        
        // Flip sprite based on horizontal direction
        bool shouldFlipX = lastAimDirection.x < 0;
        
        if (animator != null)
        {
            animator.SetFlipX(shouldFlipX);
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.flipX = shouldFlipX;
        }
    }
    
    void PlayAnimation(AnimationState state)
    {
        currentState = state;
        
        if (animator == null) return;
        
        switch (state)
        {
            case AnimationState.Idle:
                if (idleSprites != null && idleSprites.Length > 0)
                {
                    animator.SetSprites(idleSprites, idleFrameDuration, FlipbookLoopMethod.Loop);
                    animator.Play();
                }
                break;
                
            case AnimationState.Moving:
                if (movingSprites != null && movingSprites.Length > 0)
                {
                    animator.SetSprites(movingSprites, movingFrameDuration, FlipbookLoopMethod.Loop);
                    animator.Play();
                }
                break;
                
            case AnimationState.Attacking:
                if (attackingSprites != null && attackingSprites.Length > 0)
                {
                    float effectiveFrameDuration = attackingFrameDuration / StatsManager.Instance.playerRate.GetValueAsMultiplier();
                    animator.SetSprites(attackingSprites, effectiveFrameDuration, FlipbookLoopMethod.Once);
                    animator.Play();
                }
                break;
        }
    }
    
    /// Call this when the player starts attacking
    public void PlayAttackAnimation(Vector2 attackDirection)
    {
        isAttacking = true;
        
        // Override facing direction based on attack direction
        if (Mathf.Abs(attackDirection.x) > 0.1f)
        {
            lastAimDirection = attackDirection;
            bool shouldFlipX = attackDirection.x < 0;
            
            if (animator != null)
            {
                animator.SetFlipX(shouldFlipX);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = shouldFlipX;
            }
        }
        
        PlayAnimation(AnimationState.Attacking);
        
        // Get attack animation duration
        float attackDuration = GetAnimationDuration();
        
        // Return to movement animation after attack finishes
        Invoke(nameof(EndAttack), attackDuration);
    }
    
    void EndAttack()
    {
        isAttacking = false;
    }
    
    float GetAnimationDuration()
    {
        // Calculate attack animation duration based on sprites and frame duration
        if (attackingSprites != null && attackingSprites.Length > 0)
        {
            float effectiveFrameDuration = attackingFrameDuration / StatsManager.Instance.playerRate.GetValueAsMultiplier();
            return attackingSprites.Length * effectiveFrameDuration;
        }
        
        return 0.5f; // Default fallback
    }
    
    /// Public method to manually set facing direction (useful for aiming)
    public void SetFacingDirection(Vector2 direction)
    {
        if (!isAttacking && Mathf.Abs(direction.x) > 0.1f)
        {
            lastAimDirection = direction;
        }
    }
}

