using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ZoneGenerator : MonoBehaviour
{


    #region VARIABLES


    [SerializeField] private RoomFactory roomFactory;
    [SerializeField] private Tilemap roomsTilemap;
    [SerializeField] private AutoTileSet zoneAutoTileSet;
    [SerializeField] private Transform roomParent; // Parent object to organize spawned rooms

    [SerializeField] private SpriteRenderer doorPrefab;

    private RoomData[,] roomGraph;
    private Dictionary<Vector2Int, Room> spawnedRooms = new Dictionary<Vector2Int, Room>();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Start()
    {
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

        Debug.Log($"Successfully spawned {spawnedRooms.Count} rooms with their doors");
    }
    
    
    // Spawns a single room at the specified grid position
    private void SpawnRoom(RoomData roomData, Vector2Int gridPosition)
    {
        Room spawnedRoom = roomFactory.SpawnRoom(roomsTilemap, zoneAutoTileSet, roomParent, roomData, 64);
        spawnedRooms[gridPosition] = spawnedRoom;
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

        GameObject roomObject = spawnedRooms[gridPosition].gameObject;
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
        float doorDistance = 5f * 0.4f; // Position doors partway between rooms
        
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