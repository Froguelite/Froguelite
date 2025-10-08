using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    // PlayerMovement handles moving the player based on input from the InputManager


    #region VARIABLES


    public static PlayerMovement Instance { get; private set; }

    [SerializeField] private Collider2D playerCollider;

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


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
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


}