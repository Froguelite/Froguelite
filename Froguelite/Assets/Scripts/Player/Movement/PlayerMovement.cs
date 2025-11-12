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
    }

    private void Update()
    {
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
        if (canMove && !isManualMoving)
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
        if (canMove && !isAttackingOverride)
            InputManager.Instance.PushAnyPendingMovement();
        else
            rb.linearVelocity = Vector2.zero;
    }


    // Sets whether the player cannot move due to attacking
    public void SetAttackingOverride(bool value)
    {
        isAttackingOverride = value;
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
        }
        else
        {
            // Restore original damping
            rb.linearDamping = originalDrag;
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


    #endregion


}