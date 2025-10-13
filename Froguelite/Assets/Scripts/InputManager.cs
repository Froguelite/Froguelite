using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    // InputManager is the central manager of player input
    // ALL input comes through the input manager, and distributes to other scripts as needed


    #region VARIABLES


    public static InputManager Instance { get; private set; }

    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction mapAction;

    private bool pendingAttack = false;
    private Vector2 pendingMoveInput = Vector2.zero;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        SetupActions();
    }


    private void OnEnable()
    {
        EnableActions();
    }


    private void OnDisable()
    {
        DisableActions();
    }


    #endregion


    #region ENABLE / DISABLE ACTIONS


    // Sets up the input actions
    private void SetupActions()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];
        mapAction = playerInput.actions["Map"];
    }


    // Enables all actions and subscribes to their events
    private void EnableActions()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCanceled;
        attackAction.started += OnAttackStart;
        attackAction.canceled += OnAttackEnd;

        mapAction.started += OnOpenMap;
        mapAction.canceled += OnCloseMap;
    }


    // Disables all actions and unsubscribes from their events
    private void DisableActions()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        attackAction.started -= OnAttackStart;
        attackAction.canceled -= OnAttackEnd;

        mapAction.started -= OnOpenMap;
        mapAction.canceled -= OnCloseMap;
    }


    #endregion


    #region MOVE


    // Called when the move action is performed
    public void OnMove(InputAction.CallbackContext context)
    {
        pendingMoveInput = context.ReadValue<Vector2>();
        PlayerMovement.Instance.SetMoveInputAxes(pendingMoveInput);
    }


    // Called when the move action stops
    public void OnMoveCanceled(InputAction.CallbackContext context)
    {
        pendingMoveInput = Vector2.zero;
        PlayerMovement.Instance.SetMoveInputAxes(pendingMoveInput);
    }


    // Pushes any pending movement inputs again to the movement script
    public void PushAnyPendingMovement()
    {
        PlayerMovement.Instance.SetMoveInputAxes(pendingMoveInput);
    }


    #endregion


    #region ATTACK


    // Called when the attack action is started
    public void OnAttackStart(InputAction.CallbackContext context)
    {
        pendingAttack = true;
        PlayerAttack.Instance.OnInitiateAttack();
    }


    // Called when the attack action is stopped
    public void OnAttackEnd(InputAction.CallbackContext context)
    {
        pendingAttack = false;
    }


    // Returns whether an attack is pending (button held down)
    public bool IsPendingAttack()
    {
        return pendingAttack;
    }


    #endregion


    #region MAP


    // Called when the map action is started
    public void OnOpenMap(InputAction.CallbackContext context)
    {
        MinimapManager.Instance.ToggleFullMap(true);
    }


    // Called when the map action is stopped
    public void OnCloseMap(InputAction.CallbackContext context)
    {
        MinimapManager.Instance.ToggleFullMap(false);
    }


    #endregion


}
