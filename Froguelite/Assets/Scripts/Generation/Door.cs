using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{

    // Door represents a single door within a room, managing its state and interactions


    #region VARIABLES


    public enum DoorDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public DoorData doorData { get; private set; }

    private bool isTravelling = false;
    private bool invertSpriteDirection = false;

    [SerializeField] private SpriteRenderer frogRenderer;
    [SerializeField] private Transform bobbingTransform;
    [SerializeField] private SpriteMask frogMask;
    [SerializeField] private bool useWaterSway = true;
    [SerializeField] private bool useBobbingMove = false;
    [SerializeField] private bool useRipples = true;
    [SerializeField] private ParticleSystem rippleParticles;
    [SerializeField] private bool useLeafTrail = false;
    [SerializeField] private ParticleSystem[] leafTrailParticles;

    [SerializeField] private Transform attachTransform;

    [SerializeField] private Sprite unlockedSpriteUp;
    [SerializeField] private Sprite lockedSpriteUp;
    [SerializeField] private Sprite unlockedSpriteDown;
    [SerializeField] private Sprite lockedSpriteDown;
    [SerializeField] private Sprite unlockedSpriteLeft;
    [SerializeField] private Sprite lockedSpriteLeft;
    [SerializeField] private Sprite unlockedSpriteRight;
    [SerializeField] private Sprite lockedSpriteRight;

    [SerializeField] private Sign signPrefab;
    public Sprite dangerousSignSprite;
    public Sprite woodpeckerSignSprite;
    private bool spawnedSign = false;

    // Floating animation variables
    private Vector3 frogRendShownPos;
    private Vector3 frogRendHiddenPos;
    private float floatSpeed = 1f;
    private float floatRange = 0.3f;
    private float timeOffset;
    private bool inShownPosition = true;

    // Bobbing animation variables
    private bool isBobbingDuringTravel = false;
    private Coroutine bobbingCoroutine;
    private Vector3 bobbingTransformOriginalPos;
    private bool spawnedDangerousSign = false;


    #endregion


    #region MONOBEHAVIOUR

    void Awake()
    {
        if (!useRipples)
        {
            rippleParticles.Stop();
        }

        StopLeafTrailParticles();
    }

    private void Update()
    {
        if (frogMask != null && useWaterSway)
        {
            FloatFrog();
        }
    }


    #endregion


    #region DATA MANAGEMENT


    // Initializes door with given data
    public void InitializeDoorData(DoorData data)
    {
        doorData = data;

        frogRendShownPos = frogRenderer.transform.localPosition;
        frogRendHiddenPos = frogRendShownPos - new Vector3(0, 1, 0);
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // Random starting phase for variety
        bobbingTransformOriginalPos = bobbingTransform.transform.localPosition;

        DoorManager.Instance.RegisterDoor(this);

        UpdateDoorVisuals();
    }

    // Updates door's data with given new data
    public void UpdateDoorData(DoorData newData)
    {
        doorData = newData;

        UpdateDoorVisuals();
    }


    #endregion


    #region SPAWNING AND VISUALS


    // Updates the door's visual representation based on its current state (open, closed, locked, etc.)
    public void UpdateDoorVisuals(bool animate = true)
    {
        if (doorData.isImpassable)
        {
            frogRenderer.enabled = false;
            return;
        }

        // Set sprites to correct direction and locked / unlocked
        if (doorData.isLocked)
        {
            switch (doorData.direction)
            {
                case DoorDirection.Up:
                    frogRenderer.sprite = invertSpriteDirection ? lockedSpriteUp : lockedSpriteDown;
                    break;
                case DoorDirection.Down:
                    frogRenderer.sprite = invertSpriteDirection ? lockedSpriteDown : lockedSpriteUp;
                    break;
                case DoorDirection.Left:
                    frogRenderer.sprite = invertSpriteDirection ? lockedSpriteLeft : lockedSpriteRight;
                    break;
                case DoorDirection.Right:
                    frogRenderer.sprite = invertSpriteDirection ? lockedSpriteRight : lockedSpriteLeft;
                    break;
            }
            SpawnSign(woodpeckerSignSprite);
        }
        else
        {
            switch (doorData.direction)
            {
                case DoorDirection.Up:
                    frogRenderer.sprite = invertSpriteDirection ? unlockedSpriteUp : unlockedSpriteDown;
                    break;
                case DoorDirection.Down:
                    frogRenderer.sprite = invertSpriteDirection ? unlockedSpriteDown : unlockedSpriteUp;
                    break;
                case DoorDirection.Left:
                    frogRenderer.sprite = invertSpriteDirection ? unlockedSpriteLeft : unlockedSpriteRight;
                    break;
                case DoorDirection.Right:
                    frogRenderer.sprite = invertSpriteDirection ? unlockedSpriteRight : unlockedSpriteLeft;
                    break;
            }
        }

        // If we are travelling, don't need to worry about showing / hiding
        if (isTravelling) return;

        // Show or hide the frog accordingly
        if (doorData.isOpen)
        {
            if (inShownPosition) return;

            inShownPosition = true;
            LeanTween.cancel(frogRenderer.gameObject);

            frogRenderer.enabled = true;

            if (useRipples)
                rippleParticles.Play();
                
            if (animate)
            {
                if (useLeafTrail)
                    StartLeafTrailParticles();

                frogRenderer.transform.LeanMoveLocal(frogRendShownPos, 0.5f).setEaseOutSine().setOnComplete(() =>
                {
                    if (useLeafTrail)
                        StopLeafTrailParticles();
                });
            }
            else
            {
                frogRenderer.transform.localPosition = frogRendShownPos;
            }
        }
        else
        {
            if (!inShownPosition) return;

            inShownPosition = false;
            LeanTween.cancel(frogRenderer.gameObject);
            if (useRipples)
                rippleParticles.Stop();

            if (animate)
            {
                if (useLeafTrail)
                    StartLeafTrailParticles();

                frogRenderer.transform.LeanMoveLocal(frogRendHiddenPos, 0.5f).setEaseInSine().setOnComplete(() =>
                {
                    frogRenderer.enabled = false;
                    if (useLeafTrail)
                        StopLeafTrailParticles();
                });
            }
            else
            {
                frogRenderer.transform.localPosition = frogRendHiddenPos;
                frogRenderer.enabled = false;
            }
        }
    }

    // Makes the frog renderer float around its original position in the water
    private void FloatFrog()
    {
        // Don't apply floating if we're bobbing during travel
        if (isBobbingDuringTravel) return;

        // Create smooth floating motion using sine waves
        float x = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatRange;
        float y = Mathf.Sin((Time.time + timeOffset) * floatSpeed * 1.3f) * floatRange * 0.7f;

        // Apply the floating offset as local position
        frogMask.transform.localPosition = new Vector3(x, y, 0);
    }


    private void StartLeafTrailParticles()
    {
        if (leafTrailParticles == null) return;

        foreach (var ps in leafTrailParticles)
        {
            if (ps != null && !ps.isPlaying)
            {
                ps.Play();
            }
        }
    }

    private void StopLeafTrailParticles()
    {
        if (leafTrailParticles == null) return;

        foreach (var ps in leafTrailParticles)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop();
            }
        }
    }

    public void SpawnSign(Sprite signSprite)
    {
        if (spawnedSign) return;
        spawnedSign = true;

        if (signPrefab != null)
        {
            Vector3 offset = Vector3.zero;
            
            switch (doorData.direction)
            {
                case DoorDirection.Up:
                    offset = new Vector3(-2f, 0, 0); // Left of door
                    break;
                case DoorDirection.Down:
                    offset = new Vector3(-2f, 0, 0); // Left of door
                    break;
                case DoorDirection.Left:
                    offset = new Vector3(0, 2f, 0); // Above door
                    break;
                case DoorDirection.Right:
                    offset = new Vector3(0, 2f, 0); // Above door
                    break;
            }
            
            Sign signInstance = Instantiate(signPrefab, transform.position + offset, Quaternion.identity);
            signInstance.SetupSign(signSprite, true);
        }
    }


    #endregion


    #region OPENING AND LOCKING


    // Opens the door if it's not impassable
    public void OpenDoor()
    {
        if (!doorData.isImpassable)
        {
            doorData.isOpen = true;

            if (!doorData.isLocked)
            {
                UpdateDoorVisuals();
            }
        }
    }

    // Closes the door if it's not impassable
    public void CloseDoor()
    {
        if (!doorData.isImpassable)
        {
            doorData.isOpen = false;

            if (!doorData.isLocked)
            {
                UpdateDoorVisuals();
            }
        }
    }

    // Unlocks the door if it's locked
    public void UnlockDoor()
    {
        if (doorData.isLocked)
        {
            doorData.isLocked = false;

            if (!doorData.isImpassable)
            {
                UpdateDoorVisuals();
                
                // Notify MinimapManager about the unlock
                if (MinimapManager.Instance != null && RoomManager.Instance != null)
                {
                    Room room = RoomManager.Instance.GetRoomAtWorldPosition(doorData.launchPosition);
                    if (room != null)
                    {
                        MinimapManager.Instance.OnDoorUnlocked(room, doorData.direction);
                    }
                }
            }
        }
    }

    // Locks the door if it's not already locked
    public void LockDoor()
    {
        if (!doorData.isLocked)
        {
            doorData.isLocked = true;

            if (!doorData.isImpassable)
            {
                UpdateDoorVisuals();
            }
        }
    }


    #endregion


    #region TRAVEL


    // Initiates the travel process through the door if it's open and not locked
    public void OnInteract()
    {
        if (GameManager.Instance.currentPlayerState == GameManager.PlayerState.Exploring &&
            doorData.isOpen &&
            !doorData.isImpassable)
        {
            if (doorData.isLocked)
            {
                if (InventoryManager.Instance.woodpeckers <= 0) 
                {
                    PlayerAttack.Instance.StopTongueExtension();
                    return;
                }

                InventoryManager.Instance.RemoveWoodpeckers(1);
                UnlockDoor();

                PlayerAttack.Instance.StopTongueExtension(false);
                return;
            }

            GameManager.Instance.SetPlayerState(GameManager.PlayerState.InBubble);
            isTravelling = true;
            DoorManager.Instance.OnTravelStarted();

            MinimapManager.Instance.OnPlayerTravelThroughDoor(RoomManager.Instance.GetRoomAtWorldPosition(doorData.launchPosition), doorData.direction);

            // Make it so the player cannot move or attack, and suck the player into the bubble
            PlayerAttack.Instance.StopTongueExtension(true);
            PlayerAttack.Instance.SetCanAttack(false);
            PlayerMovement.Instance.SetCanMove(false);
            PlayerMovement.Instance.DisableCollision();

            PlayerMovement.Instance.onReachManualMoveTarget.AddListener(() =>
            {
                // Make sure the player is exactly inside the frog, and parent them to it so they move with it
                PlayerMovement.Instance.transform.position = attachTransform.transform.position;
                PlayerMovement.Instance.transform.SetParent(attachTransform.transform);

                invertSpriteDirection = true;
                UpdateDoorVisuals();

                // Start bobbing animation if enabled
                if (useBobbingMove)
                {
                    if (bobbingCoroutine != null) StopCoroutine(bobbingCoroutine);
                    bobbingCoroutine = StartCoroutine(BobbingDuringTravel(2f));
                }

                // Enable leaf trail particles during travel
                if (useLeafTrail)
                    StartLeafTrailParticles();

                // Tween the frog from current position (launch position) to other island (landing position)
                transform.LeanMove(doorData.otherRoomLaunchPosition, 2f).setEaseInOutSine().setOnComplete(() =>
                {
                    // Unparent the player, and move them to the landing position
                    PlayerMovement.Instance.transform.SetParent(null);
                    DontDestroyOnLoad(PlayerMovement.Instance.gameObject);
                    StartCoroutine(LaunchPlayerToLanding());

                    invertSpriteDirection = false;
                    UpdateDoorVisuals();

                    transform.LeanMove(doorData.launchPosition, 2f).setEaseInOutSine().setOnComplete(() =>
                    {
                        // Stop bobbing animation
                        if (bobbingCoroutine != null)
                        {
                            StopCoroutine(bobbingCoroutine);
                            bobbingCoroutine = null;
                            isBobbingDuringTravel = false;
                            LeanTween.cancel(bobbingTransform.gameObject);
                            bobbingTransform.transform.LeanMoveLocal(bobbingTransformOriginalPos, 0.3f).setEaseOutSine();
                        }

                        // Stop leaf trail particles after travel
                        if (useLeafTrail)
                            StopLeafTrailParticles();

                        isTravelling = false;
                        UpdateDoorVisuals();
                        DoorManager.Instance.OnTravelEnded();
                    });

                    GameManager.Instance.SetPlayerState(GameManager.PlayerState.Exploring);
                });
            });
        }
    }


    private IEnumerator LaunchPlayerToLanding(float duration = .5f)
    {
        Vector3 startPosition = PlayerMovement.Instance.transform.position;
        Vector3 endPosition = doorData.landingPosition;

        // Calculate arc height based on distance for a natural arc
        float distance = Vector3.Distance(startPosition, endPosition);
        float arcHeight = distance * 0.5f; // Arc height is half the distance for a nice curve

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Use easing for smooth acceleration/deceleration
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // Calculate the horizontal position (linear interpolation)
            Vector3 horizontalPos = Vector3.Lerp(startPosition, endPosition, easedT);

            // Calculate the vertical arc using a parabola
            // Peak of arc occurs at t = 0.5
            float arcOffset = 4f * arcHeight * t * (1f - t);

            // Combine horizontal movement with vertical arc
            Vector3 arcPosition = horizontalPos + Vector3.up * arcOffset;

            PlayerMovement.Instance.transform.position = arcPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end exactly at the landing position
        PlayerMovement.Instance.transform.position = endPosition;

        // Re-enable player collision and movement after landing
        PlayerMovement.Instance.EnableCollision();
        PlayerMovement.Instance.SetCanMove(true);
        PlayerAttack.Instance.SetCanAttack(true);

        // Notify that the launch phase is complete
        DoorManager.Instance.OnTravelLaunchComplete();
    }


    // Creates a bobbing animation during travel that matches the movement speed
    private IEnumerator BobbingDuringTravel(float travelDuration)
    {
        isBobbingDuringTravel = true;
        float elapsedTime = 0f;
        float bobbingFrequency = 4f; // Number of bobs during the travel
        float bobbingHeight = .10f; // Maximum height of each bob

        while (elapsedTime < travelDuration)
        {
            float t = elapsedTime / travelDuration;

            // Apply bobbing motion with speed-based frequency
            float bobbingOffset = Mathf.Sin(elapsedTime * bobbingFrequency * 2f * Mathf.PI / travelDuration) * bobbingHeight;
            bobbingTransform.transform.localPosition = new Vector3(0, bobbingOffset, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset position at the end
        LeanTween.cancel(bobbingTransform.gameObject);
        bobbingTransform.transform.LeanMoveLocal(bobbingTransformOriginalPos, 0.3f).setEaseOutSine();
        isBobbingDuringTravel = false;
    }


    #endregion


    #region HELPERS


    public static DoorDirection GetOppositeDirection(DoorDirection direction)
    {
        switch (direction)
        {
            case DoorDirection.Up:
                return DoorDirection.Down;
            case DoorDirection.Down:
                return DoorDirection.Up;
            case DoorDirection.Left:
                return DoorDirection.Right;
            case DoorDirection.Right:
                return DoorDirection.Left;
            default:
                return direction;
        }
    }


    #endregion


}

[System.Serializable]
public class DoorData
{

    // DoorData contains all relevant information about a door's state and properties


    #region VARIABLES


    public bool isImpassable; // E.g., solid wall (no door)
    public bool isLocked; // E.g., locked door, requires woodpecker
    public bool isOpen; // E.g., open door, can pass through; if false, it's closed and impassable until opened
    public Door.DoorDirection direction; // Direction the door is facing

    public Vector3 launchPosition; // World position where the door hovers, and will "launch" from
    public Vector3 landingPosition; // World position where the door will land on the other island
    public Vector3 otherRoomLaunchPosition; // World position in the other room where the door launches from


    #endregion


    #region CONSTRUCTORS


    public DoorData(
        Vector3 launchPosition = default,
        Vector3 landingPosition = default,
        Vector3 otherRoomLaunchPosition = default,
        Door.DoorDirection direction = Door.DoorDirection.Up,
        bool isImpassable = true,
        bool isLocked = false,
        bool isOpen = true
    )
    {
        this.isImpassable = isImpassable;
        this.isLocked = isLocked;
        this.isOpen = isOpen;
        this.direction = direction;
        this.launchPosition = launchPosition;
        this.landingPosition = landingPosition;
        this.otherRoomLaunchPosition = otherRoomLaunchPosition;
    }


    public Door.DoorDirection GetOppositeDirection(Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up:
                return Door.DoorDirection.Down;
            case Door.DoorDirection.Down:
                return Door.DoorDirection.Up;
            case Door.DoorDirection.Left:
                return Door.DoorDirection.Right;
            case Door.DoorDirection.Right:
                return Door.DoorDirection.Left;
            default:
                return direction;
        }
    }


    #endregion

}
