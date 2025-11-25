using System.Collections;
using System.Collections.Generic;
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
    private Color originalTongueColor; // Store original tongue color

    private Vector3 targetLocalPosition;
    private bool isExtending = false;
    private bool isRetracting = false;
    private bool inCooldown = false;

    public bool canAttack { get; private set; } = true;

    [Header("Backwards Tongue Settings")]
    private HashSet<string> activeTongueTags = new HashSet<string>();
    private Transform backwardsTongue;
    private GameObject backwardsTongueVisual;
    private SpriteRenderer backwardsTongueSprite;
    private Vector3 backwardsTargetLocalPosition;
    private bool backwardsIsExtending = false;
    private bool backwardsIsRetracting = false;


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
            
            // Store original tongue color
            if (tongueSprite != null)
            {
                originalTongueColor = tongueSprite.color;
            }
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

        // Spawn backwards tongue if tag exists
        if (activeTongueTags.Contains("backwardsTongue"))
        {
            StartBackwardsTongueAttack(direction);
        }
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

        // Handle backwards tongue if it exists
        if (backwardsTongue != null)
        {
            HandleBackwardsTongueAttack();
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

        // Also stop backwards tongue extension
        if (backwardsTongue != null && backwardsIsExtending)
        {
            StopBackwardsTongueExtension();
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
        return isExtending || isRetracting || backwardsIsExtending || backwardsIsRetracting;
    }


    #endregion


    #region TONGUE TAGS


    // Adds a tongue tag if it doesn't already exist
    public void AddTongueTag(string tag)
    {
        if (!activeTongueTags.Contains(tag))
        {
            activeTongueTags.Add(tag);
            Debug.Log($"[PlayerAttack] Added tongue tag: {tag}");

            // Initialize backwards tongue if this is the backwardsTongue tag
            if (tag == "backwardsTongue")
            {
                InitializeBackwardsTongue();
            }
            
            // Apply sick fly tint if this is the sickFly tag
            if (tag == "sickFly")
            {
                PlayerMovement.Instance.ApplySickFlyTint();
            }
        }
    }


    // Checks if a tongue tag is active
    public bool HasTongueTag(string tag)
    {
        return activeTongueTags.Contains(tag);
    }


    #endregion


    #region BACKWARDS TONGUE


    // Initializes the backwards tongue GameObject and components
    private void InitializeBackwardsTongue()
    {
        if (backwardsTongue != null) return; // Already initialized

        // Create backwards tongue tip (collider)
        GameObject backwardsTongueTip = new GameObject("BackwardsTongueTip");
        backwardsTongueTip.transform.SetParent(tongue.parent);
        backwardsTongueTip.transform.localPosition = tongueStartOffset;
        backwardsTongueTip.transform.localScale = tongue.localScale;
        backwardsTongue = backwardsTongueTip.transform;

        // Add collider and AttackOverlapHandler (copy from main tongue)
        Collider2D mainCollider = tongue.GetComponent<Collider2D>();
        if (mainCollider != null)
        {
            // Copy collider properties based on type
            if (mainCollider is CircleCollider2D circleCollider)
            {
                CircleCollider2D backwardsCollider = backwardsTongueTip.AddComponent<CircleCollider2D>();
                backwardsCollider.radius = circleCollider.radius;
                backwardsCollider.offset = circleCollider.offset;
                backwardsCollider.isTrigger = true;
            }
            else if (mainCollider is BoxCollider2D boxCollider)
            {
                BoxCollider2D backwardsCollider = backwardsTongueTip.AddComponent<BoxCollider2D>();
                backwardsCollider.size = boxCollider.size;
                backwardsCollider.offset = boxCollider.offset;
                backwardsCollider.isTrigger = true;
            }
            else
            {
                // Fallback: add a basic circle collider
                CircleCollider2D backwardsCollider = backwardsTongueTip.AddComponent<CircleCollider2D>();
                backwardsCollider.radius = 0.1f;
                backwardsCollider.isTrigger = true;
            }
        }

        AttackOverlapHandler mainHandler = tongue.GetComponent<AttackOverlapHandler>();
        if (mainHandler != null)
        {
            backwardsTongueTip.AddComponent<AttackOverlapHandler>();
        }

        // Create backwards tongue visual (matching the structure of main tongue visual)
        backwardsTongueVisual = new GameObject("BackwardsTongueVisual");
        backwardsTongueVisual.transform.SetParent(tongue.parent);
        backwardsTongueVisual.transform.localPosition = Vector3.zero; // Will be positioned by UpdateBackwardsTongueVisual
        backwardsTongueVisual.transform.localScale = new Vector3(0.2f, 0.2f, 1f); // Match main tongue visual's initial scale

        // Create child sprite object (matching main tongue structure)
        GameObject backwardsTongueSpriteObj = new GameObject("Sprite");
        backwardsTongueSpriteObj.transform.SetParent(backwardsTongueVisual.transform);
        backwardsTongueSpriteObj.transform.localPosition = Vector3.zero;
        backwardsTongueSpriteObj.transform.localRotation = Quaternion.Euler(0, 180, -90); // Match main tongue sprite rotation
        backwardsTongueSpriteObj.transform.localScale = new Vector3(0.2f, 1f, 1f); // Match main tongue sprite scale

        // Add sprite renderer to child
        backwardsTongueSprite = backwardsTongueSpriteObj.AddComponent<SpriteRenderer>();
        backwardsTongueSprite.sprite = tongueSprite.sprite;
        backwardsTongueSprite.sortingLayerID = tongueSprite.sortingLayerID;
        backwardsTongueSprite.sortingOrder = tongueSprite.sortingOrder - 1; // Render behind main tongue
        backwardsTongueSprite.enabled = false;

        Debug.Log("[PlayerAttack] Initialized backwards tongue");
    }


    // Starts the backwards tongue attack in the opposite direction
    private void StartBackwardsTongueAttack(Vector3 mainDirection)
    {
        if (backwardsTongue == null) return;

        // Calculate opposite direction
        Vector3 backwardsDirection = -mainDirection;
        backwardsTargetLocalPosition = backwardsDirection * GetStatModifiedRange() + tongueStartOffset;
        backwardsIsExtending = true;
        backwardsIsRetracting = false;
    }


    // Handles the backwards tongue attack movement
    private void HandleBackwardsTongueAttack()
    {
        if (backwardsIsExtending)
        {
            backwardsTongue.localPosition = Vector3.MoveTowards(backwardsTongue.localPosition, backwardsTargetLocalPosition, GetStatModifiedExtendSpeed() * Time.fixedDeltaTime);
            if (Vector3.Distance(backwardsTongue.localPosition, backwardsTargetLocalPosition) < 0.01f)
            {
                StopBackwardsTongueExtension();
            }
        }
        else if (backwardsIsRetracting)
        {
            backwardsTongue.localPosition = Vector3.MoveTowards(backwardsTongue.localPosition, tongueStartOffset, GetStatModifiedRetractSpeed() * Time.fixedDeltaTime);

            if (Vector3.Distance(backwardsTongue.localPosition, tongueStartOffset) < 0.01f)
            {
                StopBackwardsTongueRetraction();
            }
        }

        // Update the backwards tongue visual
        UpdateBackwardsTongueVisual();
    }


    // Stops the backwards tongue extension and starts retraction
    private void StopBackwardsTongueExtension()
    {
        backwardsIsExtending = false;
        backwardsIsRetracting = true;
    }


    // Stops the backwards tongue retraction
    private void StopBackwardsTongueRetraction()
    {
        backwardsIsRetracting = false;
        backwardsTongue.localPosition = tongueStartOffset;
    }


    // Updates the backwards tongue visual to stretch between the player's mouth and tongue tip
    private void UpdateBackwardsTongueVisual()
    {
        if (backwardsTongueVisual == null || backwardsTongueSprite == null) return;

        // Show visual only when extending or retracting
        bool shouldShow = backwardsIsExtending || backwardsIsRetracting;
        backwardsTongueSprite.enabled = shouldShow;

        if (!shouldShow) return;

        // Use player mouth position as start
        Vector3 mouthPos = playerMouth != null ? playerMouth.position : backwardsTongue.parent.position;
        Vector3 tipPos = backwardsTongue.position;

        // Calculate distance and direction
        float distance = Vector3.Distance(mouthPos, tipPos);
        Vector3 direction = tipPos - mouthPos;

        // Position visual at midpoint between mouth and tip
        backwardsTongueVisual.transform.position = (mouthPos + tipPos) / 2f;
        backwardsTongueVisual.transform.position = new Vector3(
            backwardsTongueVisual.transform.position.x,
            backwardsTongueVisual.transform.position.y,
            0f
        );

        // Scale the visual to match the distance
        // The main tongue visual uses the same calculation, so this should match
        backwardsTongueVisual.transform.localScale = new Vector3(tongueWidth, distance, 1f);

        // Rotate to point in the correct direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        backwardsTongueVisual.transform.rotation = Quaternion.Euler(0, 0, angle);
    }


    // Immediately retracts the tongue without animation (used when dash interrupts attack)
    public void ImmediateRetract()
    {
        // Stop any active attack states
        isExtending = false;
        isRetracting = false;

        // Reset tongue position immediately
        tongue.localPosition = tongueStartOffset;

        // Re-enable player movement
        PlayerMovement.Instance.SetAttackingOverride(false);

        // Hide tongue visual
        if (tongueSprite != null)
        {
            tongueSprite.enabled = false;
        }

        // Start cooldown
        if (!inCooldown)
        {
            StartCoroutine(TongueCooldownCoroutine());
        }
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

        // Set tongue color based on Sick Fly status
        if (HasTongueTag("sickFly"))
        {
            tongueSprite.color = Color.green;
        }
        else
        {
            tongueSprite.color = originalTongueColor;
        }
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