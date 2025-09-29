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

    [SerializeField] private Door doorPrefab;

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

        // TEMPORARY - Open all doors and set player position to starter room
        OpenAllDoors();
        SetPlayerToStarterRoom();
    }


    // Sets the player position to the center of the starter room
    private void SetPlayerToStarterRoom()
    {
        // Find the starter room in the room graph
        RoomData starterRoom = null;

        for (int x = 0; x < roomGraph.GetLength(0); x++)
        {
            for (int y = 0; y < roomGraph.GetLength(1); y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null && room.roomType == Room.RoomType.Starter)
                {
                    starterRoom = room;
                    break;
                }
            }
            if (starterRoom != null) break;
        }

        if (starterRoom == null)
        {
            Debug.LogError("Starter room not found in room graph");
            return;
        }

        // Calculate the center position of the starter room in world coordinates
        Vector3 starterRoomCenter = new Vector3(
            starterRoom.roomCoordinate.x * starterRoom.roomLength + starterRoom.roomLength * 0.5f,
            starterRoom.roomCoordinate.y * starterRoom.roomLength + starterRoom.roomLength * 0.5f,
            PlayerMovement.Instance.transform.position.z
        );

        // Set the player position
        PlayerMovement.Instance.transform.position = starterRoomCenter;

        Debug.Log($"Set player position to starter room center: {starterRoomCenter}");
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

        // First pass: Spawn all rooms
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

        Debug.Log($"Successfully spawned {spawnedRooms.Count} rooms");

        // Second pass: Spawn all doors after all rooms exist
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

        Debug.Log($"Successfully spawned doors for all rooms");
    }


    // Spawns a single room at the specified grid position
    private void SpawnRoom(RoomData roomData, Vector2Int gridPosition)
    {
        Room spawnedRoom = roomFactory.SpawnRoom(roomsTilemap, zoneAutoTileSet, roomParent, roomData, 32);
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
                SpawnDoor(roomData, direction, doorData, gridPosition);
            }
        }
    }

    // Spawns a single door at the specified direction from the room
    private void SpawnDoor(RoomData roomData, Door.DoorDirection direction, DoorData doorData, Vector2Int roomGridPos)
    {
        if (doorPrefab == null)
        {
            Debug.LogError("Door prefab is null - cannot spawn door");
            return;
        }

        if (roomData.tileLayout == null)
        {
            Debug.LogError("Room tile layout is null - cannot determine door position");
            return;
        }

        // Get the door launch position within the room's tile layout
        Vector2Int doorLaunchPos = RoomTileHelper.GetDoorLocation(roomData.tileLayout, direction, true);

        // Get the adjacent room position based on door direction
        Vector2Int adjacentRoomGridPos = GetAdjacentRoomPosition(roomGridPos, direction);
        
        // Get the door landing position from the adjacent room
        Vector2Int doorLandingPos = Vector2Int.zero;
        if (IsValidRoomPosition(adjacentRoomGridPos) && roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y] != null)
        {
            RoomData adjacentRoom = roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y];
            Door.DoorDirection oppositeDirection = GetOppositeDirection(direction);
            doorLandingPos = RoomTileHelper.GetDoorLocation(adjacentRoom.tileLayout, oppositeDirection, false);
        }

        // Calculate the world positions
        Vector3 doorLaunchWorldPos = new Vector3(
            roomData.roomCoordinate.x * roomData.roomLength + doorLaunchPos.x + 0.5f,
            roomData.roomCoordinate.y * roomData.roomLength + doorLaunchPos.y + 0.5f,
            -0.1f
        );

        Vector3 doorLandingWorldPos = Vector3.zero;
        if (IsValidRoomPosition(adjacentRoomGridPos) && roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y] != null)
        {
            RoomData adjacentRoom = roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y];
            doorLandingWorldPos = new Vector3(
                adjacentRoom.roomCoordinate.x * adjacentRoom.roomLength + doorLandingPos.x + 0.5f,
                adjacentRoom.roomCoordinate.y * adjacentRoom.roomLength + doorLandingPos.y + 0.5f,
                -0.1f
            );
        }

        // Set door launch and landing positions in door data
        doorData.launchPosition = doorLaunchWorldPos;
        doorData.landingPosition = doorLandingWorldPos;

        // Spawn the door prefab at the launch position
        Door doorInstance = Instantiate(doorPrefab, doorLaunchWorldPos, Quaternion.identity);

        // Set the door's parent to keep the hierarchy organized
        if (!spawnedRooms.ContainsKey(roomGridPos))
        {
            Debug.LogError($"Room at {roomGridPos} not found in spawned rooms");
            return;
        }

        Room roomObject = spawnedRooms[roomGridPos];
        doorInstance.transform.SetParent(roomObject.transform);
        roomObject.AddDoor(doorInstance);

        // Initialize the door with its data
        doorInstance.InitializeDoorData(doorData);
    }


    #endregion


    #region HELPER METHODS


    // Gets the adjacent room position based on the door direction
    private Vector2Int GetAdjacentRoomPosition(Vector2Int roomGridPos, Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up:
                return roomGridPos + new Vector2Int(0, 1);
            case Door.DoorDirection.Down:
                return roomGridPos + new Vector2Int(0, -1);
            case Door.DoorDirection.Left:
                return roomGridPos + new Vector2Int(-1, 0);
            case Door.DoorDirection.Right:
                return roomGridPos + new Vector2Int(1, 0);
            default:
                return roomGridPos;
        }
    }

    // Gets the opposite door direction
    private Door.DoorDirection GetOppositeDirection(Door.DoorDirection direction)
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

    // Checks if the given room position is valid within the room graph bounds
    private bool IsValidRoomPosition(Vector2Int position)
    {
        if (roomGraph == null) return false;
        return position.x >= 0 && position.x < roomGraph.GetLength(0) &&
               position.y >= 0 && position.y < roomGraph.GetLength(1);
    }


    #endregion


    #region TESTERS


    // Manually opens all doors in the zone (for testing purposes)
    public void OpenAllDoors()
    {
        foreach (var roomEntry in spawnedRooms)
        {
            Room room = roomEntry.Value;
            foreach (Door door in room.doors)
            {
                if (door != null && door.doorData != null && !door.doorData.isOpen)
                {
                    door.OpenDoor();
                }
            }
        }
    }


    #endregion


}