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
        Debug.LogWarning("Door spawning not yet implemented.");
    }

    // Updates the door's visual representation based on its current state (open, closed, locked, etc.)
    private void UpdateDoorVisuals()
    {
        // TODO
        Debug.LogWarning("Door visual update not yet implemented.");
    }


    #endregion


    #region INTERACTIONS


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


    #endregion


    #region CONSTRUCTORS


    public DoorData(
        bool isImpassable = true,
        bool isLocked = false,
        bool isOpen = true,
        Door.DoorDirection direction = Door.DoorDirection.Up
    )
    {
        this.isImpassable = isImpassable;
        this.isLocked = isLocked;
        this.isOpen = isOpen;
        this.direction = direction;
    }


    #endregion

}
