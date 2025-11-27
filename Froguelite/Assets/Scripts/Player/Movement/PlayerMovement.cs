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
    [SerializeField] private ParticleSystem sickParticles; // Particle system for sick fly effect
    [SerializeField] private ParticleSystem iceParticles; // Particle system for ice/reduced friction effect
    [SerializeField] private GameObject afterimagePrefab; // Prefab for dash afterimages

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right; // Track last movement direction, default to right
    private Vector2 currentVel;
    
    private Color originalSpriteColor; // Store the original sprite color
    private bool isSickFlyActive = false; // Track if sick fly effect is active
    private Color sickFlyTintMultiplier = new Color(0.7f, 1f, 0.7f, 1f); // Green tint multiplier for sick fly
    private bool isIceTintActive = false; // Track if ice/reduced friction effect is active
    private Color iceTintMultiplier = new Color(0.7f, 0.7f, 1f, 1f); // Light blue tint multiplier for ice

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
    
    // Afterimage settings
    private float afterimageSpawnInterval = 0.05f; // Time between spawning afterimages
    private float afterimageFadeDuration = 0.3f; // How long afterimages take to fade out
    private float afterimageTimer = 0f; // Timer for spawning afterimages

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

    public Transform lilypadCenterPoint;

    private float originalDrag;
    private bool useReducedFriction = false;


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
        originalDrag = rb.linearDamping;
        originalSpriteColor = playerSpriteRenderer.color;
        
        // Subscribe to reset event
        GameManager.ResetPlayerState += ResetSpriteColor;
    }

    private void OnDestroy()
    {
        // Unsubscribe from reset event
        GameManager.ResetPlayerState -= ResetSpriteColor;
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
            
            // Spawn afterimages during dash
            afterimageTimer -= Time.deltaTime;
            if (afterimageTimer <= 0f)
            {
                SpawnAfterimage();
                afterimageTimer = afterimageSpawnInterval;
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
        
        // Track last movement direction when there's valid input
        if (newMoveInput.magnitude > 0.1f)
        {
            lastMoveDirection = newMoveInput.normalized;
        }
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

            // If reduced friction is active, use momentum-based movement (ice sliding)
            if (useReducedFriction)
            {
                float speedMultiplier = StatsManager.Instance.playerSpeed.GetValueAsMultiplier() * Time.fixedDeltaTime * 200f;
                Vector2 targetVelocity = finalMoveInput * speedMultiplier;
                
                // If there's input, accelerate towards target velocity
                if (finalMoveInput.magnitude > 0.01f)
                {
                    // Smoothly accelerate towards target velocity
                    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
                }
                // If no input, let velocity decay naturally (low damping will make it slide)
                // Velocity will naturally decay due to low linearDamping
            }
            else
            {
                // Normal movement: directly set velocity
                rb.linearVelocity = finalMoveInput * StatsManager.Instance.playerSpeed.GetValueAsMultiplier() * Time.fixedDeltaTime * 200f;
            }
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
        // Check if player can dash (not on cooldown, can move, not manually moving, already dashing)
        if (dashCooldownTimer > 0f || !canMove || isManualMoving || isDashing)
            return;

        // If attacking, immediately retract tongue and stop attack
        if (isAttackingOverride && PlayerAttack.Instance.IsAttacking())
        {
            PlayerAttack.Instance.ImmediateRetract();
        }

        // Determine dash direction based on current movement input
        Vector2 direction = moveInput;
        
        // If no movement input, try velocity direction
        if (direction.magnitude < 0.1f)
        {
            direction = rb.linearVelocity.normalized;
        }
        
        // If still no direction, use last stored movement direction
        if (direction.magnitude < 0.1f)
        {
            direction = lastMoveDirection;
        }
        
        // Final fallback: dash to the right (should never reach here due to initialization)
        if (direction.magnitude < 0.1f)
        {
            direction = Vector2.right;
        }

        // Start the dash
        isDashing = true;
        dashDirection = direction.normalized;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Play dash animation
        //PlayerAnimationController.Instance.PlayDashAnimation(dashDirection);

        AudioManager.Instance.PlaySound(PlayerSound.Dodge, 0.3f);
    }


    // Ends the dash
    private void EndDash()
    {
        isDashing = false;
        dashDirection = Vector2.zero;
        
        // End dash animation
        //PlayerAnimationController.Instance.EndDashAnimation();
        
        // Re-apply current movement input if player can move
        if (canMove && !isAttackingOverride)
            InputManager.Instance.PushAnyPendingMovement();
    }


    // Spawns an afterimage at the player's current position
    private void SpawnAfterimage()
    {
        if (afterimagePrefab == null || playerSpriteRenderer == null)
            return;

        // Create the afterimage GameObject
        GameObject afterimage = Instantiate(afterimagePrefab, transform.position, Quaternion.identity);
        
        // Get the SpriteRenderer component from the afterimage
        SpriteRenderer afterimageSR = afterimage.GetComponent<SpriteRenderer>();
        if (afterimageSR != null)
        {
            // Copy the current sprite and visual properties from the player
            afterimageSR.sprite = playerSpriteRenderer.sprite;
            Color afterimageColor = playerSpriteRenderer.color;
            afterimageColor.a *= 0.6f; // Start at 60% opacity
            afterimageSR.color = afterimageColor;
            afterimageSR.flipX = playerSpriteRenderer.flipX;
            afterimageSR.flipY = playerSpriteRenderer.flipY;
            afterimageSR.sortingLayerName = playerSpriteRenderer.sortingLayerName;
            afterimageSR.sortingOrder = playerSpriteRenderer.sortingOrder - 1; // Render behind player
            
            // Fade out the afterimage using LeanTween
            LeanTween.alpha(afterimage, 0f, afterimageFadeDuration).setOnComplete(() =>
            {
                Destroy(afterimage);
            });
        }
        else
        {
            // If no SpriteRenderer found, just destroy the afterimage after the fade duration
            Destroy(afterimage, afterimageFadeDuration);
        }
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


    #region REDUCED FRICTION


    // Sets whether to use reduced friction (slippery movement) or not
    public void SetReducedFriction(bool value)
    {
        useReducedFriction = value;
        
        if (value)
        {
            // Set very low damping for ice-like sliding
            rb.linearDamping = 0.1f;
            ApplyIceTint();
        }
        else
        {
            // Restore original damping
            rb.linearDamping = originalDrag;
            RemoveIceTint();
            // Stop any sliding momentum when disabled
            if (moveInput.magnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
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


    // Applies sick fly green tint to the player sprite
    public void ApplySickFlyTint()
    {
        if (isSickFlyActive) return; // Already applied
        
        isSickFlyActive = true;
        playerSpriteRenderer.color = MultiplyColors(playerSpriteRenderer.color, sickFlyTintMultiplier);
        
        // Play sick particles if assigned
        if (sickParticles != null)
        {
            sickParticles.Play();
        }
    }


    // Removes sick fly green tint from the player sprite
    public void RemoveSickFlyTint()
    {
        if (!isSickFlyActive) return; // Not applied
        
        isSickFlyActive = false;
        // Divide current color by the tint multiplier to reverse the effect
        Color currentColor = playerSpriteRenderer.color;
        playerSpriteRenderer.color = new Color(
            currentColor.r / sickFlyTintMultiplier.r,
            currentColor.g / sickFlyTintMultiplier.g,
            currentColor.b / sickFlyTintMultiplier.b,
            currentColor.a / sickFlyTintMultiplier.a
        );
        
        // Stop sick particles if assigned
        if (sickParticles != null)
        {
            sickParticles.Stop();
        }
    }


    // Applies ice/reduced friction blue tint to the player sprite
    private void ApplyIceTint()
    {
        if (isIceTintActive) return; // Already applied
        
        isIceTintActive = true;
        playerSpriteRenderer.color = MultiplyColors(playerSpriteRenderer.color, iceTintMultiplier);
        
        // Play ice particles if assigned
        if (iceParticles != null)
        {
            iceParticles.Play();
        }
    }


    // Removes ice/reduced friction blue tint from the player sprite
    private void RemoveIceTint()
    {
        if (!isIceTintActive) return; // Not applied
        
        isIceTintActive = false;
        // Divide current color by the tint multiplier to reverse the effect
        Color currentColor = playerSpriteRenderer.color;
        playerSpriteRenderer.color = new Color(
            currentColor.r / iceTintMultiplier.r,
            currentColor.g / iceTintMultiplier.g,
            currentColor.b / iceTintMultiplier.b,
            currentColor.a / iceTintMultiplier.a
        );
        
        // Stop ice particles if assigned
        if (iceParticles != null)
        {
            iceParticles.Stop();
        }
    }


    // Multiplies two colors component-wise for blending effects
    private Color MultiplyColors(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }


    // Resets the player sprite color to its original value, removing all tints
    private void ResetSpriteColor()
    {
        // Remove all active tints
        if (isSickFlyActive)
        {
            isSickFlyActive = false;
            // Stop sick particles if assigned
            if (sickParticles != null)
            {
                sickParticles.Stop();
            }
        }
        
        if (isIceTintActive)
        {
            isIceTintActive = false;
            // Stop ice particles if assigned
            if (iceParticles != null)
            {
                iceParticles.Stop();
            }
        }

        // Clear friction effect
        if (useReducedFriction)
        {
            SetReducedFriction(false);
        }

        // Clear jitter effect
        if (useJitterAnimation)
        {
            useJitterAnimation = false;
            jitterOffset = Vector3.zero;
            jitterOffsetTarget = Vector3.zero;
            jitterChangeTimer = 0f;
        }
        
        // Clear drunk effect
        if (useDrunkMovement)
        {
            useDrunkMovement = false;
            drunkOffset = Vector2.zero;
            drunkOffsetTarget = Vector2.zero;
            drunkOffsetChangeTimer = 0f;
        }
        
        // Reset to original color
        playerSpriteRenderer.color = originalSpriteColor;
    }


    #endregion


}