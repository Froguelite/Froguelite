using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    // PlayerMovement handles moving the player based on input from the InputManager


    #region VARIABLES


    public static PlayerMovement Instance { get; private set; }

    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private Collider2D damageCollider;
    [SerializeField] public SpriteRenderer playerSpriteRenderer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVel;

    private bool isManualMoving = false;
    private Vector3 manualMoveTarget;
    private float manualMoveSpeed;
    [HideInInspector] public UnityEvent onReachManualMoveTarget;

    public bool canMove { get; private set; } = true;
    public bool isAttackingOverride { get; private set; } = false; // If true, player cannot move due to attacking
    public bool IsDashing => isDashing; // Public getter to check if player is currently dashing

    private bool isDashing = false;
    private float dashDuration = 0.2f; // How long the dash lasts
    private float dashSpeed = 15f; // Speed multiplier during dash
    private float dashCooldown = 0.5f; // Cooldown between dashes
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.zero;

    private bool useDrunkMovement = false;
    private Vector2 drunkOffset = Vector2.zero;
    private Vector2 drunkOffsetTarget = Vector2.zero;
    private float drunkOffsetChangeTimer = 0f;
    private float drunkOffsetChangeInterval = 0.3f; // How often the drunk offset changes
    private float drunkOffsetMagnitude = .5f; // How strong the drunk effect is

    [SerializeField] private bool useJitterAnimation = false;
    private Vector3 jitterOffset = Vector3.zero;
    private Vector3 jitterOffsetTarget = Vector3.zero;
    private float jitterChangeTimer = 0f;
    private float jitterChangeInterval = 0.05f; // How fast the jitter changes (faster = more jittery)
    private float jitterMagnitude = 0.05f; // How far the sprite jitters (in world units)


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            TeleportInstanceToThis();
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Update dash cooldown
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Update dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }

        if (useJitterAnimation)
        {
            UpdateJitterAnimation();
        }
        else if (jitterOffset != Vector3.zero)
        {
            // Reset jitter offset when disabled
            playerSpriteRenderer.transform.localPosition = Vector3.zero;
            jitterOffset = Vector3.zero;
            jitterOffsetTarget = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }


    #endregion


    #region MOVEMENT


    // Sets the input axes of the movement
    public void SetMoveInputAxes(Vector2 newMoveInput)
    {
        moveInput = newMoveInput;
    }


    // Applies the movement to the player
    public void ApplyMovement()
    {
        if (isDashing)
        {
            // During dash, move in the dash direction at dash speed
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        else if (canMove && !isManualMoving)
        {
            Vector2 finalMoveInput = Vector2.zero;
            if (!isAttackingOverride)
                finalMoveInput = moveInput;

            // Apply drunk movement if enabled
            if (useDrunkMovement)
            {
                UpdateDrunkOffset();
                finalMoveInput += drunkOffset;
            }

            rb.linearVelocity = finalMoveInput * StatsManager.Instance.playerSpeed.GetValueAsMultiplier() * Time.fixedDeltaTime * 200f;
        }
        else if (isManualMoving)
            HandleManualMove();
    }


    // Handles manually moving player towards a target position
    private void HandleManualMove()
    {
        transform.position = Vector3.MoveTowards(transform.position, manualMoveTarget, manualMoveSpeed * Time.fixedDeltaTime);
        if (Vector3.Distance(transform.position, manualMoveTarget) < 0.01f)
        {
            EndManualMove();
        }
    }


    // Sets whether the player can move or not
    // Stops any active movement if set to false, pushes any pending movement if set to true
    public void SetCanMove(bool value)
    {
        canMove = value;
        
        // Cancel active dash if movement is disabled
        if (!canMove && isDashing)
        {
            EndDash();
        }
        
        if (canMove && !isAttackingOverride)
            InputManager.Instance.PushAnyPendingMovement();
        else
            rb.linearVelocity = Vector2.zero;
    }


    // Sets whether the player cannot move due to attacking
    public void SetAttackingOverride(bool value)
    {
        isAttackingOverride = value;
        
        // Cancel active dash if attacking starts
        if (isAttackingOverride && isDashing)
        {
            EndDash();
        }
        
        if (!isAttackingOverride && canMove)
            InputManager.Instance.PushAnyPendingMovement();
        else
            rb.linearVelocity = Vector2.zero;
    }


    #endregion


    #region MANUAL MOVING


    // Manually moves the player to a specific world position
    public void ManualMoveToPosition(Vector3 targetPosition, float moveSpeed)
    {
        isManualMoving = true;
        manualMoveTarget = targetPosition;
        manualMoveSpeed = moveSpeed;
    }


    // Ends manual movement
    public void EndManualMove()
    {
        isManualMoving = false;
        onReachManualMoveTarget?.Invoke();
        onReachManualMoveTarget.RemoveAllListeners();
    }


    private void TeleportInstanceToThis()
    {
        // Set the player position
        Instance.transform.position = transform.position;

        // Find and update all active CinemachineCamera components
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera cam in cameras)
        {
            if (cam.isActiveAndEnabled)
            {
                // Force the camera to update its position immediately
                cam.ForceCameraPosition(PlayerMovement.Instance.transform.position, Quaternion.identity);

                // Manually update the camera's internal state
                cam.UpdateCameraState(Vector3.up, Time.deltaTime);
            }
        }
    }


    #endregion


    #region COLLISION


    // Enables collision for the player movement
    public void EnableCollision()
    {
        playerCollider.enabled = true;
    }


    // Disables collision for the player movement
    public void DisableCollision()
    {
        playerCollider.enabled = false;
    }


    // Gets the center of the player
    public Vector3 GetPlayerCenter()
    {
        return damageCollider.bounds.center;
    }


    #endregion


    #region DASH


    // Initiates a dash in the current movement direction
    public void InitiateDash()
    {
        // Check if player can dash (not on cooldown, can move, not manually moving, not attacking)
        if (dashCooldownTimer > 0f || !canMove || isManualMoving || isDashing || isAttackingOverride)
            return;

        // Determine dash direction based on current movement input
        Vector2 direction = moveInput;
        
        // If no movement input, dash in the direction the player is currently moving
        if (direction.magnitude < 0.1f)
        {
            // If not moving at all, dash forward (could use last facing direction if you track it)
            direction = rb.linearVelocity.normalized;
            
            // If still no direction, don't dash
            if (direction.magnitude < 0.1f)
                return;
        }

        // Start the dash
        isDashing = true;
        dashDirection = direction.normalized;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Play dash animation
        PlayerAnimationController.Instance.PlayDashAnimation(dashDirection);
    }


    // Ends the dash
    private void EndDash()
    {
        isDashing = false;
        dashDirection = Vector2.zero;
        
        // End dash animation
        PlayerAnimationController.Instance.EndDashAnimation();
        
        // Re-apply current movement input if player can move
        if (canMove && !isAttackingOverride)
            InputManager.Instance.PushAnyPendingMovement();
    }


    #endregion


    #region DRUNK MOVEMENT


    // Sets whether to use drunk movement (random jitter) or not
    public void SetUseDrunkMovement(bool value)
    {
        useDrunkMovement = value;

        // Reset drunk offset when disabled
        if (!value)
        {
            drunkOffset = Vector2.zero;
            drunkOffsetTarget = Vector2.zero;
            drunkOffsetChangeTimer = 0f;
        }
    }


    // Updates the drunk movement offset over time
    private void UpdateDrunkOffset()
    {
        drunkOffsetChangeTimer -= Time.fixedDeltaTime;

        // Generate new random target offset when timer expires
        if (drunkOffsetChangeTimer <= 0f)
        {
            drunkOffsetChangeTimer = drunkOffsetChangeInterval;
            drunkOffsetTarget = new Vector2(
                Random.Range(-drunkOffsetMagnitude, drunkOffsetMagnitude),
                Random.Range(-drunkOffsetMagnitude, drunkOffsetMagnitude)
            );
        }

        // Smoothly interpolate towards the target offset
        drunkOffset = Vector2.Lerp(drunkOffset, drunkOffsetTarget, Time.fixedDeltaTime * 3f);
    }


    #endregion


    #region JITTER ANIMATION


    public void SetUseJitterAnimation(bool value)
    {
        useJitterAnimation = value;
    }


    // Updates the jitter animation (visual only, doesn't affect movement)
    private void UpdateJitterAnimation()
    {
        jitterChangeTimer -= Time.deltaTime;

        // Generate new random target offset when timer expires
        if (jitterChangeTimer <= 0f)
        {
            jitterChangeTimer = jitterChangeInterval;
            jitterOffsetTarget = new Vector3(
                Random.Range(-jitterMagnitude, jitterMagnitude),
                Random.Range(-jitterMagnitude, jitterMagnitude),
                0f
            );
        }

        // Smoothly interpolate towards the target offset
        jitterOffset = Vector3.Lerp(jitterOffset, jitterOffsetTarget, Time.deltaTime * 20f);
        
        // Apply the offset to the sprite's local position (visual only)
        playerSpriteRenderer.transform.localPosition = jitterOffset;
    }


    #endregion


    #region OTHER


    public void SetRenderAbove(bool value)
    {
        if (value)
            playerSpriteRenderer.sortingOrder = 10;
        else
            playerSpriteRenderer.sortingOrder = 0;
    }


    public void SetSpriteHidden(bool value)
    {
        playerSpriteRenderer.enabled = !value;
    }


    #endregion


}