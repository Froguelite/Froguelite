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
    }


    // Enables all actions and subscribes to their events
    private void EnableActions()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCanceled;
        attackAction.started += OnAttack;
    }


    // Disables all actions and unsubscribes from their events
    private void DisableActions()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        attackAction.started -= OnAttack;
    }


    #endregion


    #region MOVE AND ATTACK HANDLERS


    // Called when the move action is performed
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        PlayerMovement.Instance.SetMoveInputAxes(moveInput);
    }


    // Called when the move action stops
    public void OnMoveCanceled(InputAction.CallbackContext context)
    {
        PlayerMovement.Instance.SetMoveInputAxes(Vector2.zero);
    }


    // Called when the attack action is performed
    public void OnAttack(InputAction.CallbackContext context)
    {
        PlayerAttack.Instance.OnInitiateAttack();
    }


    #endregion


}
