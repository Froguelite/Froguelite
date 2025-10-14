using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{


    #region VARIABLES


    public static RoomManager Instance { get; private set; }

    [SerializeField] private FloatingText roomClearTextPrefab;
    [SerializeField] private Collectable_Lotus lotusPrefab;
    [SerializeField] private Collectable_Heart heartPrefab;
    [SerializeField] private Collectable_Woodpecker woodpeckerPrefab;

    private Room[,] rooms; // 2D array to hold rooms based on their grid positions
    private int roomGraphWidth;
    private int roomGraphHeight;
    private int roomLength = 32; // Default room length in tiles


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    #endregion


    #region INITIALIZATION


    // Initializes the room manager with the room graph dimensions and creates the rooms array
    public void Initialize(RoomData[,] roomGraph, Dictionary<Vector2Int, Room> spawnedRooms)
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room graph is null - cannot initialize RoomManager");
            return;
        }

        roomGraphWidth = roomGraph.GetLength(0);
        roomGraphHeight = roomGraph.GetLength(1);

        // Initialize the rooms array
        rooms = new Room[roomGraphWidth, roomGraphHeight];

        // Get room length from the first available room data
        for (int x = 0; x < roomGraphWidth && roomLength == 32; x++)
        {
            for (int y = 0; y < roomGraphHeight && roomLength == 32; y++)
            {
                RoomData roomData = roomGraph[x, y];
                if (roomData != null && roomData.tileLayout != null)
                {
                    roomLength = roomData.roomLength;
                    break;
                }
            }
        }

        // Populate the rooms array with the spawned rooms
        foreach (var spawnedRoom in spawnedRooms)
        {
            Vector2Int gridPos = spawnedRoom.Key;
            Room room = spawnedRoom.Value;

            if (IsValidGridPosition(gridPos))
            {
                rooms[gridPos.x, gridPos.y] = room;
            }
        }

        Debug.Log($"RoomManager initialized with {roomGraphWidth}x{roomGraphHeight} room grid");
    }


    #endregion


    #region ROOM MANAGEMENT


    // Gets the room at the specified grid position
    public Room GetRoomAtGridPosition(Vector2Int gridPosition)
    {
        if (!IsValidGridPosition(gridPosition))
        {
            return null;
        }

        return rooms[gridPosition.x, gridPosition.y];
    }

    // Gets the room at the specified world position
    public Room GetRoomAtWorldPosition(Vector2 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        return GetRoomAtGridPosition(gridPosition);
    }

    // Gets all rooms in the manager (including null entries for empty spaces)
    public Room[,] GetAllRooms()
    {
        return rooms;
    }

    // Gets only the non-null rooms as a list
    public List<Room> GetActiveRooms()
    {
        List<Room> activeRooms = new List<Room>();

        if (rooms == null)
            return activeRooms;

        for (int x = 0; x < roomGraphWidth; x++)
        {
            for (int y = 0; y < roomGraphHeight; y++)
            {
                Room room = rooms[x, y];
                if (room != null)
                {
                    activeRooms.Add(room);
                }
            }
        }

        return activeRooms;
    }

    // Gets the room of a specific type (returns the first found)
    public Room GetRoomOfType(Room.RoomType roomType)
    {
        if (rooms == null)
            return null;

        for (int x = 0; x < roomGraphWidth; x++)
        {
            for (int y = 0; y < roomGraphHeight; y++)
            {
                Room room = rooms[x, y];
                if (room != null && room.roomData != null && room.roomData.roomType == roomType)
                {
                    return room;
                }
            }
        }

        return null;
    }

    // Gets all rooms of a specific type
    public List<Room> GetRoomsOfType(Room.RoomType roomType)
    {
        List<Room> roomsOfType = new List<Room>();

        if (rooms == null)
            return roomsOfType;

        for (int x = 0; x < roomGraphWidth; x++)
        {
            for (int y = 0; y < roomGraphHeight; y++)
            {
                Room room = rooms[x, y];
                if (room != null && room.roomData != null && room.roomData.roomType == roomType)
                {
                    roomsOfType.Add(room);
                }
            }
        }

        return roomsOfType;
    }

    // Gets the grid position of a specific room
    public Vector2Int GetGridPositionOfRoom(Room targetRoom)
    {
        if (rooms == null || targetRoom == null)
            return Vector2Int.one * -1; // Return invalid position

        for (int x = 0; x < roomGraphWidth; x++)
        {
            for (int y = 0; y < roomGraphHeight; y++)
            {
                if (rooms[x, y] == targetRoom)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return Vector2Int.one * -1; // Return invalid position if not found
    }

    // Gets adjacent rooms to the specified grid position
    public List<Room> GetAdjacentRooms(Vector2Int gridPosition)
    {
        List<Room> adjacentRooms = new List<Room>();

        if (!IsValidGridPosition(gridPosition))
            return adjacentRooms;

        // Check all four cardinal directions
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentPos = gridPosition + direction;
            Room adjacentRoom = GetRoomAtGridPosition(adjacentPos);
            if (adjacentRoom != null)
            {
                adjacentRooms.Add(adjacentRoom);
            }
        }

        return adjacentRooms;
    }


    #endregion


    #region COORDINATE CONVERSION


    // Converts world position to grid position
    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        int gridX = Mathf.FloorToInt(worldPosition.x / roomLength);
        int gridY = Mathf.FloorToInt(worldPosition.y / roomLength);
        return new Vector2Int(gridX, gridY);
    }

    // Converts grid position to world position (center of the room)
    public Vector2 GridToWorldPosition(Vector2Int gridPosition)
    {
        float worldX = gridPosition.x * roomLength + roomLength * 0.5f;
        float worldY = gridPosition.y * roomLength + roomLength * 0.5f;
        return new Vector2(worldX, worldY);
    }


    #endregion


    #region UTILITY


    // Checks if the given grid position is valid within the room graph bounds
    private bool IsValidGridPosition(Vector2Int gridPosition)
    {
        if (rooms == null)
            return false;

        return gridPosition.x >= 0 && gridPosition.x < roomGraphWidth &&
               gridPosition.y >= 0 && gridPosition.y < roomGraphHeight;
    }

    // Gets the dimensions of the room grid
    public Vector2Int GetGridDimensions()
    {
        return new Vector2Int(roomGraphWidth, roomGraphHeight);
    }

    // Gets the room length in tiles
    public int GetRoomLength()
    {
        return roomLength;
    }


    #endregion


    #region ROOM COMPLETION


    // Spawns room clear items (floating text, Lotus, Woodpecker, heart...) at the given position
    public void SpawnRoomClearItems(Vector3 position)
    {
        // Show the room clear text
        if (roomClearTextPrefab != null)
        {
            FloatingText clearText = Instantiate(roomClearTextPrefab, position, Quaternion.identity);
            clearText.transform.position = new Vector3(clearText.transform.position.x, clearText.transform.position.y + 0.5f, -1f); // Ensure text is above other elements
            clearText.SetText("ISLAND CLEAR!");
            clearText.StartAnimation();
        }

        // Spawn collectible items with luck-based probability
        SpawnCollectibleItem(position);
    }

    // Spawns a collectible item based on probability and luck
    private void SpawnCollectibleItem(Vector3 position)
    {
        // Base chances (excluding nothing)
        float baseLotusChance = 0.60f;      // 60%
        float baseWoodpeckerChance = 0.25f; // 25%
        float baseHeartChance = 0.15f;      // 15%

        // Get luck multiplier from player stats
        float luckMultiplier = 1f;
        if (StatsManager.Instance != null && StatsManager.Instance.playerLuck != null)
        {
            luckMultiplier = StatsManager.Instance.playerLuck.GetValueAsMultiplier();
        }

        // Scale item chances by luck (but not the nothing chance)
        float scaledLotusChance = baseLotusChance * luckMultiplier;
        float scaledWoodpeckerChance = baseWoodpeckerChance * luckMultiplier;
        float scaledHeartChance = baseHeartChance * luckMultiplier;

        // Calculate total probability for normalization
        float totalItemChance = scaledLotusChance + scaledWoodpeckerChance + scaledHeartChance;

        // Normalize all chances to sum to 1.0
        scaledLotusChance /= totalItemChance;
        scaledWoodpeckerChance /= totalItemChance;
        scaledHeartChance /= totalItemChance;

        // Generate random value and determine what to spawn
        float randomValue = Random.Range(0f, 1f);
        
        if (randomValue < scaledLotusChance)
        {
            // Spawn Lotus
            if (lotusPrefab != null)
            {
                Instantiate(lotusPrefab, position, Quaternion.identity);
            }
        }
        else if (randomValue < scaledLotusChance + scaledWoodpeckerChance)
        {
            // Spawn Woodpecker
            if (woodpeckerPrefab != null)
            {
                Instantiate(woodpeckerPrefab, position, Quaternion.identity);
            }
        }
        else if (randomValue < scaledLotusChance + scaledWoodpeckerChance + scaledHeartChance)
        {
            // Spawn Heart
            if (heartPrefab != null)
            {
                Instantiate(heartPrefab, position, Quaternion.identity);
            }
        }
        // Otherwise spawn nothing (remaining probability)
    }


    #endregion


}
