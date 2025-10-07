using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    // PlayerAttack handles the player's tongue attack mechanics


    #region VARIABLES


    public static PlayerAttack Instance { get; private set; }


    [Header("Tongue Settings")]
    [SerializeField] Transform tongue;
    [SerializeField] private bool stopMovementOnAttack = true;
    [SerializeField] float tongueDistance = 3f;
    [SerializeField] float tongueExtendSpeed = 10f;
    [SerializeField] float tongueRetractSpeed = 25f;
    [SerializeField] float tongueCooldown = 0.5f;

    private Vector3 targetLocalPosition;
    private bool isExtending = false;
    private bool isRetracting = false;
    private bool inCooldown = false;

    public bool canAttack { get; private set; } = true;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    void FixedUpdate()
    {
        HandleTongueAttack();
    }


    #endregion


    #region TONGUE ATTACK


    // If possible, initiate attack
    public void OnInitiateAttack()
    {
        if (!isExtending && !isRetracting && !inCooldown && canAttack)
        {
            StartTongueAttack();
        }
    }


    // Starts the tonuge attack
    void StartTongueAttack()
    {
        // Stop player movement if setting is set so
        if (stopMovementOnAttack)
        {
            PlayerMovement.Instance.SetAttackingOverride(true);
        }

        // Get mouse position in world space
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Get target position for tongue extension
        Vector3 direction = (mousePosition - transform.position).normalized;
        targetLocalPosition = direction * tongueDistance;

        isExtending = true;
    }


    // Handles the tongue attack, extending or retracting as required
    public void HandleTongueAttack()
    {
        if (isExtending)
        {
            tongue.localPosition = Vector3.MoveTowards(tongue.localPosition, targetLocalPosition, tongueExtendSpeed * Time.fixedDeltaTime);
            if (Vector3.Distance(tongue.localPosition, targetLocalPosition) < 0.01f)
            {
                StopTongueExtension();
            }
        }
        else if (isRetracting)
        {
            tongue.localPosition = Vector3.MoveTowards(tongue.localPosition, Vector3.zero, tongueRetractSpeed * Time.fixedDeltaTime);

            if (Vector3.Distance(tongue.localPosition, Vector3.zero) < 0.01f)
            {
                StopTongueRetraction();
            }
        }
    }

    // Stops the tongue extension and starts retraction
    public void StopTongueExtension(bool movePlayerWithRetract = false)
    {
        isExtending = false;
        isRetracting = true;

        if (movePlayerWithRetract)
        {
            PlayerMovement.Instance.ManualMoveToPosition(tongue.position, tongueRetractSpeed);
        }
    }

    // Stops the tongue retraction and starts cooldown
    public void StopTongueRetraction()
    {
        PlayerMovement.Instance.SetAttackingOverride(false);
        isRetracting = false;
        tongue.localPosition = Vector3.zero;
        StartCoroutine(TongueCooldownCoroutine());
    }

    // Handles the cooldown after a tongue attack
    private IEnumerator TongueCooldownCoroutine()
    {
        inCooldown = true;
        yield return new WaitForSeconds(tongueCooldown);
        inCooldown = false;
        if (InputManager.Instance.IsPendingAttack())
        {
            OnInitiateAttack();
        }
    }


    // Sets whether the player can attack or not
    // If setting to true, will push any pending attack input
    public void SetCanAttack(bool value)
    {
        canAttack = value;
        if (canAttack && InputManager.Instance.IsPendingAttack())
            OnInitiateAttack();
    }
    

    // Returns whether the player is currently attacking (extending or retracting)
    public bool IsAttacking()
    {
        return isExtending || isRetracting;
    }


    #endregion


}