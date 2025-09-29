using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    // PlayerMovement handles moving the player based on input from the InputManager


    #region VARIABLES


    public static PlayerMovement Instance { get; private set; }

    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVel;

    private bool isManualMoving = false;
    private Vector3 manualMoveTarget;
    private float manualMoveSpeed;
    [HideInInspector] public UnityEvent onReachManualMoveTarget;

    public bool canMove { get; private set; } = true;


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
            rb.linearVelocity = moveInput * moveSpeed * Time.fixedDeltaTime * 100f;
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
        if (canMove)
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


}