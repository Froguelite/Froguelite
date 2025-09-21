using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneGenerator : MonoBehaviour
{


    #region VARIABLES


    [SerializeField] private int seed;

    [SerializeField] private SpriteRenderer roomPrefab;
    [SerializeField] private SpriteRenderer doorPrefab;
    
    [SerializeField] private float roomSpacing = 5f; // Distance between rooms in world units
    [SerializeField] private Transform roomParent; // Parent object to organize spawned rooms

    private RoomData[,] roomGraph;
    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Start()
    {
        UnityEngine.Random.InitState(seed);
        GenerateZone();
    }


    #endregion


    #region GENERATION


    // Generates the zone by creating a room graph and spawning rooms and doors
    public void GenerateZone()
    {
        roomGraph = RoomGraphGenerator.GetRoomGraph(8);
        SpawnRoomsFromGraph();
    }
    

    // Regenerates the zone with a new seed
    public void RegenerateZone()
    {
        UnityEngine.Random.InitState(seed);
        GenerateZone();
    }


    // Regenerates the zone with a specific seed
    public void RegenerateZone(int newSeed)
    {
        seed = newSeed;
        UnityEngine.Random.InitState(seed);
        GenerateZone();
    }


    // Regenerates the zone with a completely random seed
    [ContextMenu("Regenerate Zone with Random Seed")]
    public void RegenerateZoneWithRandomSeed()
    {
        seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        UnityEngine.Random.InitState(seed);
        GenerateZone();
    }


    #endregion


    #region SPAWNING ROOMS


    // Spawns rooms and doors from the room graph
    private void SpawnRoomsFromGraph()
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room Graph is null - cannot spawn rooms");
            return;
        }

        int width = roomGraph.GetLength(0);
        int height = roomGraph.GetLength(1);

        Debug.Log($"=== SPAWNING ROOMS FROM GRAPH ({width}x{height}) ===");

        // Clear any existing spawned rooms
        ClearSpawnedRooms();

        // Create room parent if it doesn't exist
        if (roomParent == null)
        {
            GameObject parentObject = new GameObject("Generated Rooms");
            roomParent = parentObject.transform;
        }

        // Spawn each room in the graph
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null)
                {
                    SpawnRoom(room, new Vector2Int(x, y));
                }
            }
        }

        // Spawn doors for all rooms after all rooms are created
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null)
                {
                    SpawnDoorsForRoom(room, new Vector2Int(x, y));
                }
            }
        }

        Debug.Log($"Successfully spawned {spawnedRooms.Count} rooms with their doors");
    }
    
    // Spawns a single room at the specified grid position
    private void SpawnRoom(RoomData roomData, Vector2Int gridPosition)
    {
        if (roomPrefab == null)
        {
            Debug.LogError("Room prefab is null - cannot spawn room");
            return;
        }
        
        // Calculate world position from grid position
        Vector3 worldPosition = new Vector3(gridPosition.x * roomSpacing, gridPosition.y * roomSpacing, 0);
        
        // Instantiate the room
        GameObject roomObject = Instantiate(roomPrefab.gameObject, worldPosition, Quaternion.identity, roomParent);
        roomObject.name = $"Room_{gridPosition.x},{gridPosition.y}_{roomData.roomType}";
        
        // Store reference to spawned room
        spawnedRooms[gridPosition] = roomObject;
        
        // Change room color based on type for visual distinction
        SpriteRenderer roomSprite = roomObject.GetComponent<SpriteRenderer>();
        if (roomSprite != null)
        {
            roomSprite.color = GetRoomColor(roomData.roomType);
        }
    }
    
    // Returns a color for the room based on its type
    private Color GetRoomColor(Room.RoomType roomType)
    {
        return roomType switch
        {
            Room.RoomType.Starter => Color.green,
            Room.RoomType.Boss => Color.red,
            Room.RoomType.Normal => Color.white,
            Room.RoomType.Shop => Color.yellow,
            Room.RoomType.Fly => Color.cyan,
            _ => Color.gray
        };
    }
    
    // Clears all previously spawned rooms
    private void ClearSpawnedRooms()
    {
        foreach (var roomEntry in spawnedRooms)
        {
            if (roomEntry.Value != null)
            {
                DestroyImmediate(roomEntry.Value);
            }
        }
        spawnedRooms.Clear();
    }


    #endregion


    #region SPAWNING DOORS
    
    
    // Spawns doors for a room based on its connections
    private void SpawnDoorsForRoom(RoomData roomData, Vector2Int gridPosition)
    {
        if (doorPrefab == null)
        {
            Debug.LogError("Door prefab is null - cannot spawn doors");
            return;
        }

        if (!spawnedRooms.ContainsKey(gridPosition))
        {
            Debug.LogError($"Room at {gridPosition} not found in spawned rooms");
            return;
        }

        GameObject roomObject = spawnedRooms[gridPosition];
        Transform roomTransform = roomObject.transform;

        // Check each direction for doors
        foreach (var doorEntry in roomData.doors)
        {
            Door.DoorDirection direction = doorEntry.Key;
            DoorData doorData = doorEntry.Value;

            // Only spawn doors that are not impassable (i.e., actual doors, not walls)
            if (!doorData.isImpassable)
            {
                SpawnDoor(roomTransform, direction, doorData, gridPosition);
            }
        }
    }
    
    // Spawns a single door at the specified direction from the room
    private void SpawnDoor(Transform roomTransform, Door.DoorDirection direction, DoorData doorData, Vector2Int roomGridPos)
    {
        // Calculate door position relative to room center
        Vector3 doorOffset = GetDoorOffset(direction);
        Vector3 doorPosition = roomTransform.position + doorOffset;
        
        // Calculate door rotation based on direction
        Quaternion doorRotation = GetDoorRotation(direction);
        
        // Instantiate the door
        GameObject doorObject = Instantiate(doorPrefab.gameObject, doorPosition, doorRotation, roomTransform);
        doorObject.name = $"Door_{direction}_{roomGridPos.x},{roomGridPos.y}";
        
        // Set door color based on state
        SpriteRenderer doorSprite = doorObject.GetComponent<SpriteRenderer>();
        if (doorSprite != null)
        {
            doorSprite.color = GetDoorColor(doorData);
        }
        
        // Optional: Initialize Door component if it exists
        Door doorComponent = doorObject.GetComponent<Door>();
        if (doorComponent != null)
        {
            doorComponent.InitializeDoorData(doorData);
        }
    }
    
    // Returns the offset position for a door based on its direction
    private Vector3 GetDoorOffset(Door.DoorDirection direction)
    {
        float doorDistance = roomSpacing * 0.4f; // Position doors partway between rooms
        
        return direction switch
        {
            Door.DoorDirection.Right => new Vector3(doorDistance, 0, 0),
            Door.DoorDirection.Left => new Vector3(-doorDistance, 0, 0),
            Door.DoorDirection.Up => new Vector3(0, doorDistance, 0),
            Door.DoorDirection.Down => new Vector3(0, -doorDistance, 0),
            _ => Vector3.zero
        };
    }
    
    // Returns the rotation for a door based on its direction
    private Quaternion GetDoorRotation(Door.DoorDirection direction)
    {
        return direction switch
        {
            Door.DoorDirection.Right => Quaternion.identity,
            Door.DoorDirection.Left => Quaternion.Euler(0, 0, 180),
            Door.DoorDirection.Up => Quaternion.Euler(0, 0, 90),
            Door.DoorDirection.Down => Quaternion.Euler(0, 0, -90),
            _ => Quaternion.identity
        };
    }
    
    // Returns a color for the door based on its state
    private Color GetDoorColor(DoorData doorData)
    {
        if (doorData.isLocked)
            return Color.red;
        else if (!doorData.isOpen)
            return Color.blue;
        else
            return Color.green;
    }


    #endregion
    

}