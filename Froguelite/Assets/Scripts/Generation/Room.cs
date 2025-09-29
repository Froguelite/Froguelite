using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{

    // Room represents a single room in a zone, managing its type, position, doors, spawning, etc.


    #region VARIABLES


    public enum RoomType
    {
        Normal,
        Starter,
        Boss,
        Shop,
        Fly,
    }

    public RoomData roomData { get; protected set; }

    public List<Door> doors { get; private set; } = new List<Door>();


    #endregion


    #region SETUP


    // Initializes the room based on given roomData
    public void Initialize(RoomData roomData)
    {
        this.roomData = roomData;
    }


    // Adds a door to the room's door list
    public void AddDoor(Door door)
    {
        doors.Add(door);
    }


    #endregion


}


[System.Serializable]
public class RoomData
{


    #region VARIABLES


    public Room.RoomType roomType;
    public Vector2Int roomCoordinate;
    public bool[,] tileLayout; // 2D array representing walkable (true) and non-walkable (false) tiles in the room
    public int roomLength; // Length of one side of the square room in tiles
    public Dictionary<Door.DoorDirection, DoorData> doors;

    public float genWeight = 1f; // Weight for random selection during generation (higher = more likely)
    public bool isLeaf = false; // Whether this room is a leaf node (dead end) in the graph


    #endregion


    #region CONSTRUCTORS


    // Constructor to initialize base RoomData with type and coordinates. Doors are set as empty.
    public RoomData(Room.RoomType roomType, Vector2Int roomCoordinate)
    {
        this.roomType = roomType;
        this.roomCoordinate = roomCoordinate;
        InitializeDoors();
    }


    #endregion


    #region DOOR MANAGEMENT


    // Initializes all four doors for the room with default data (impassable, closed, unlocked)
    public void InitializeDoors()
    {
        doors = new Dictionary<Door.DoorDirection, DoorData>();

        for (int i = 0; i < 4; i++)
        {
            DoorData newDoorData = new DoorData
            {
                isImpassable = true,
                isOpen = false,
                isLocked = false,
                direction = (Door.DoorDirection)i
            };

            doors.Add((Door.DoorDirection)i, newDoorData);
        }
    }

    // Updates door in given direction with new door data
    public void SetDoor(Door.DoorDirection direction, DoorData doorData)
    {
        if (doors.ContainsKey(direction))
        {
            doors[direction] = doorData;
        }
    }


    #endregion

}

