using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    // PlayerAttack handles the player's tongue attack mechanics


    #region VARIABLES


    public static PlayerAttack Instance { get; private set; }


    [Header("Tongue Settings")]
    [SerializeField] Transform tongue;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private bool stopMovementOnAttack = true;
    [SerializeField] float tongueDistance = 3f;
    [SerializeField] float tongueExtendSpeed = 10f;
    [SerializeField] float tongueRetractSpeed = 25f;
    [SerializeField] float tongueCooldown = 0.5f;
    private Vector3 tongueStartOffset;

    [Header("Tongue Visual Settings")]
    [SerializeField] private GameObject tongueVisual;
    [SerializeField] private SpriteRenderer tongueSprite;
    [SerializeField] private Transform playerMouth;
    [SerializeField] private float tongueWidth = 0.2f;

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
        tongueStartOffset = tongue.localPosition;

        // Initialize tongue visual as hidden
        if (tongueVisual != null)
        {
            tongueSprite.enabled = false;
            // Also enable the GameObject if it was disabled in editor
            tongueVisual.gameObject.SetActive(true);
            tongueSprite.gameObject.SetActive(true);
        }
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
        Vector3 direction = (mousePosition - tongue.position).normalized;
        targetLocalPosition = direction * GetStatModifiedRange() + tongueStartOffset;

        // Play attack animation
        if (animationController != null)
        {
            animationController.PlayAttackAnimation(direction);
        }

        isExtending = true;
    }


    // Handles the tongue attack, extending or retracting as required
    public void HandleTongueAttack()
    {
        if (isExtending)
        {
            tongue.localPosition = Vector3.MoveTowards(tongue.localPosition, targetLocalPosition, GetStatModifiedExtendSpeed() * Time.fixedDeltaTime);
            if (Vector3.Distance(tongue.localPosition, targetLocalPosition) < 0.01f)
            {
                StopTongueExtension();
            }
        }
        else if (isRetracting)
        {
            tongue.localPosition = Vector3.MoveTowards(tongue.localPosition, tongueStartOffset, GetStatModifiedRetractSpeed() * Time.fixedDeltaTime);

            if (Vector3.Distance(tongue.localPosition, tongueStartOffset) < 0.01f)
            {
                StopTongueRetraction();
            }
        }

        // Update the tongue visual to stretch/retract
        UpdateTongueVisual();
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
        tongue.localPosition = tongueStartOffset;
        StartCoroutine(TongueCooldownCoroutine());
    }

    // Handles the cooldown after a tongue attack
    private IEnumerator TongueCooldownCoroutine()
    {
        inCooldown = true;
        yield return new WaitForSeconds(GetStatModifiedCooldown());
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


    #region TONGUE VISUAL


    // Updates the tongue visual to stretch between the player's mouth and tongue tip
    private void UpdateTongueVisual()
    {
        // Skip if visual is not assigned
        if (tongueVisual == null) return;

        // Show visual only when extending or retracting
        bool shouldShow = isExtending || isRetracting;
        tongueSprite.enabled = shouldShow;

        if (!shouldShow) return;

        // Use player mouth position as start, or tongue's parent position if mouth not assigned
        Vector3 mouthPos = playerMouth != null ? playerMouth.position : tongue.parent.position;
        Vector3 tipPos = tongue.position;

        // Calculate distance and direction
        float distance = Vector3.Distance(mouthPos, tipPos);
        Vector3 direction = tipPos - mouthPos;

        // Position visual at midpoint between mouth and tip
        tongueVisual.transform.position = (mouthPos + tipPos) / 2f;
        tongueVisual.transform.position = new Vector3(
            tongueVisual.transform.position.x,
            tongueVisual.transform.position.y,
            0f  // Ensure Z position is 0 for 2D
        );

        // Scale the visual to match the distance
        // Y-axis is the length, X-axis is the width
        tongueVisual.transform.localScale = new Vector3(tongueWidth, distance, 1f);

        // Rotate to point in the correct direction
        // Subtract 90 degrees because sprites typically face "up" by default
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        tongueVisual.transform.rotation = Quaternion.Euler(0, 0, angle);
    }


    #endregion


    #region STAT MODIFIERS


    // Gets the tongue extend speed modified by player rate stat
    private float GetStatModifiedExtendSpeed()
    {
        return tongueExtendSpeed * StatsManager.Instance.playerRate.GetValueAsMultiplier();
    }


    // Gets the tongue retract speed modified by player rate stat
    private float GetStatModifiedRetractSpeed()
    {
        return tongueRetractSpeed * StatsManager.Instance.playerRate.GetValueAsMultiplier();
    }


    // Gets the tongue cooldown modified by player rate stat
    private float GetStatModifiedCooldown()
    {
        return tongueCooldown / StatsManager.Instance.playerRate.GetValueAsMultiplier();
    }


    // Gets the tongue range modified by player range stat
    private float GetStatModifiedRange()
    {
        return tongueDistance * StatsManager.Instance.playerRange.GetValueAsMultiplier();
    }


    #endregion


}