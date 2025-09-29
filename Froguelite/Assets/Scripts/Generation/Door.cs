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

    [SerializeField] private SpriteRenderer bubbleRenderer;
    [SerializeField] private SpriteRenderer shadowRenderer;

    // Floating animation variables
    private Vector3 bubbleOriginalPosition;
    private float floatSpeed = 1f;
    private float floatRange = 0.3f;
    private float shadowDistance = 1f;
    private float timeOffset;


    #endregion


    #region MONOBEHAVIOUR


    private void Start()
    {
        if (bubbleRenderer != null)
        {
            bubbleOriginalPosition = bubbleRenderer.transform.position;
            timeOffset = Random.Range(0f, 2f * Mathf.PI); // Random starting phase for variety
        }
    }

    private void Update()
    {
        if (bubbleRenderer != null)
        {
            FloatBubble();
        }
    }


    #endregion


    #region DATA MANAGEMENT


    // Initializes door with given data
    public void InitializeDoorData(DoorData data)
    {
        doorData = data;
    }

    // Updates door's data with given new data
    public void UpdateDoorData(DoorData newData)
    {
        doorData = newData;
    }


    #endregion


    #region SPAWNING AND VISUALS


    // Spawns the door at a specific X, Y position in the room, based on doorData
    public void SpawnDoor()
    {
        // TODO
    }

    // Updates the door's visual representation based on its current state (open, closed, locked, etc.)
    private void UpdateDoorVisuals()
    {
        // TODO
    }

    // Makes the bubble renderer float around its original position
    private void FloatBubble()
    {
        // Create smooth floating motion using sine waves
        float x = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatRange;
        float y = Mathf.Sin((Time.time + timeOffset) * floatSpeed * 1.3f) * floatRange * 0.7f;

        // Apply the floating offset as local position
        bubbleRenderer.transform.localPosition = new Vector3(x, y, 0);
        shadowRenderer.transform.localPosition = new Vector3(x, -shadowDistance, 0);
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
            !doorData.isLocked &&
            !doorData.isImpassable)
        {
            GameManager.Instance.SetPlayerState(GameManager.PlayerState.InBubble);

            // Make it so the player cannot move or attack, and suck the player into the bubble
            PlayerAttack.Instance.StopTongueExtension(true);
            PlayerAttack.Instance.SetCanAttack(false);
            PlayerMovement.Instance.SetCanMove(false);
            PlayerMovement.Instance.DisableCollision();

            PlayerMovement.Instance.onReachManualMoveTarget.AddListener(() =>
            {
                // Make sure the player is exactly inside the bubble, and parent them to it so they move with it
                PlayerMovement.Instance.transform.position = bubbleRenderer.transform.position;
                PlayerMovement.Instance.transform.SetParent(bubbleRenderer.transform);

                // Tween the bubble from current position (launch position) to other island (landing position)
                transform.LeanMove(doorData.landingPosition, 2f).setEaseInOutSine().setOnComplete(() =>
                {
                    // Unparent the player, re-enable movement and attacking, and end the travel process
                    PlayerMovement.Instance.transform.SetParent(null);
                    PlayerMovement.Instance.EnableCollision();
                    PlayerMovement.Instance.SetCanMove(true);
                    PlayerAttack.Instance.SetCanAttack(true);

                    transform.position = doorData.launchPosition; // Reset the bubble position to the launch position

                    GameManager.Instance.SetPlayerState(GameManager.PlayerState.Exploring);
                });
            });
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


    #endregion


    #region CONSTRUCTORS


    public DoorData(
        Vector3 launchPosition = default,
        Vector3 landingPosition = default,
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
    }


    #endregion

}
