using System;
using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ZoneGenerator : MonoBehaviour
{


    #region VARIABLES


    public static ZoneGenerator Instance;
    public bool zoneGenerated { get; private set; } = false;

    [SerializeField] private bool generateZoneOnStart = true;
    [SerializeField] private int generateZoneOnStartSubZone = 0;
    [SerializeField] private bool teleportPlayerToStarterRoom = true;
    
    [SerializeField] private RoomFactory roomFactory;
    [SerializeField] private FoliageFactory foliageFactory;
    [SerializeField] private Tilemap roomsTilemap;
    [SerializeField] private NavMeshSurface navigationSurface;
    [SerializeField] private AutoTileSet zoneAutoTileSet;
    [SerializeField] private Transform roomParent; // Parent object to organize spawned rooms

    [SerializeField] private Door doorPrefab;

    private RoomData[,] roomGraph;
    private Dictionary<Vector2Int, Room> spawnedRooms = new Dictionary<Vector2Int, Room>();
    private char[,] combinedTileLayout; // Combined tile layout of the entire zone
    private int randomSeed;
    private bool useLoadedSeed = true;

    #endregion


    #region MONOBEHAVIOUR AND SETUP

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        
        Instance = this;
    }


    void Start()
    {
        if (generateZoneOnStart)
            GenerateZone(generateZoneOnStartSubZone);
    }


    #endregion

    #region SAVE AND LOAD RANDOM SEED
    private void SaveRandomSeed()
    {
        SaveManager.SaveForProfile<int>(SaveVariable.RandomSeed, randomSeed);
    }

    private void LoadRandomSeed()
    {
        //First load player's current health
        try
        {
            randomSeed = SaveManager.LoadForProfile<int>(SaveVariable.RandomSeed);
            Debug.Log($"[ZoneGenerator] Loaded {randomSeed} random seed from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use random value
            randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Debug.Log($"[ZoneGenerator] No saved random seed found, defaulting to random value {randomSeed}");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[ZoneGenerator] Failed to load current random seed: {ex.Message}");
            randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        useLoadedSeed = true;
    }

    #endregion

    #region GENERATION


    // Generates the zone by creating a room graph and spawning rooms and doors
    public void GenerateZone(int subZone)
    {
        // TODO: Temporary, chooses a random seed every time
        //Uses Loaded seed from profile only the first time, afterwards it will be randomly generated
        if (useLoadedSeed)
        {
            UnityEngine.Random.InitState(randomSeed);
            useLoadedSeed = false;
        } else
        {
            randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(randomSeed);
            SaveManager.WriteToFile(); //Need to save the next seed for generation ?
        }

        roomGraph = RoomGraphGenerator.GetRoomGraph(8, subZone);
        combinedTileLayout = SpawnRoomsFromGraph();
        MinimapManager.Instance.InitializeMinimap(combinedTileLayout);

        // Initialize the RoomManager with the generated rooms
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.Initialize(roomGraph, spawnedRooms);
        }
        else
        {
            Debug.LogWarning("RoomManager Instance is null - rooms will not be managed properly");
        }

        // Set player to starter room and clear it
        if (teleportPlayerToStarterRoom)
            SetPlayerToStarterRoomAndClear();

        // Regenerate the NavMesh for the new layout
        navigationSurface.BuildNavMesh();

        zoneGenerated = true;
    }


    // Sets the player position to the center of the starter room, and clears the room
    private void SetPlayerToStarterRoomAndClear()
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

        // Force the camera to update immediately to the new player position
        ForceCameraUpdate();

        // Clear the starter room
        RoomManager.Instance.GetRoomAtWorldPosition(starterRoomCenter)?.OnRoomCleared();
    }


    #endregion


    #region SPAWNING ROOMS


    // Spawns rooms and doors from the room graph
    private char[,] SpawnRoomsFromGraph()
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room Graph is null - cannot spawn rooms");
            return null;
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
                else
                {
                    SpawnEmptyRoom(new Vector2Int(x, y));
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

        // Third pass: Combine all room tile layouts into a single 2D bool array
        char[,] combinedTileLayout = CombineRoomTileLayouts();
        
        return combinedTileLayout;
    }


    // Spawns a single room at the specified grid position
    private void SpawnRoom(RoomData roomData, Vector2Int gridPosition)
    {
        Room spawnedRoom = roomFactory.SpawnRoom(roomsTilemap, zoneAutoTileSet, roomParent, roomData, 32);
        spawnedRooms[gridPosition] = spawnedRoom;

        // Generate foliage for the room
        float foliageLandDensity = 1f;

        if (roomData.roomType == Room.RoomType.SubZoneBoss || roomData.roomType == Room.RoomType.BossPortal || roomData.roomType == Room.RoomType.Fly || roomData.roomType == Room.RoomType.Shop)
        {
            foliageLandDensity = 5f; // Less foliage in special rooms
        }

        foliageFactory.GenerateFoliageForRoom(spawnedRoom, foliageLandDensity);
    }


    // Spawns an empty room area filled with water tiles instead of generating a room
    private void SpawnEmptyRoom(Vector2Int gridPosition)
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room Graph is null - cannot spawn empty room");
            return;
        }

        // Calculate room length - use the same size as other rooms (32 by default)
        int roomLength = 32;
        
        // Try to get room length from existing rooms if available
        for (int x = 0; x < roomGraph.GetLength(0) && roomLength == 32; x++)
        {
            for (int y = 0; y < roomGraph.GetLength(1) && roomLength == 32; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null && room.tileLayout != null)
                {
                    roomLength = room.roomLength;
                    break;
                }
            }
        }

        // Get the tile offset for this empty room area
        Vector2Int tileOffset = new Vector2Int(
            gridPosition.x * roomLength,
            gridPosition.y * roomLength
        );

        // Create a layout that's entirely water (all false values)
        char[,] waterLayout = new char[roomLength, roomLength];
        
        // Initialize all positions with 'w' (water)
        for (int x = 0; x < roomLength; x++)
        {
            for (int y = 0; y < roomLength; y++)
            {
                waterLayout[x, y] = 'w';
            }
        }

        // Apply the water tiles to the tilemap using auto-tiling
        RoomTileHelper.SetTilemapToLayoutWithAutoTiling(
            waterLayout,
            roomsTilemap,
            zoneAutoTileSet,
            tileOffset.x,
            tileOffset.y
        );
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
        Vector3 otherRoomLaunchWorldPos = Vector3.zero;
        if (IsValidRoomPosition(adjacentRoomGridPos) && roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y] != null)
        {
            RoomData adjacentRoom = roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y];
            doorLandingWorldPos = new Vector3(
                adjacentRoom.roomCoordinate.x * adjacentRoom.roomLength + doorLandingPos.x + 0.5f,
                adjacentRoom.roomCoordinate.y * adjacentRoom.roomLength + doorLandingPos.y + 0.5f,
                -0.1f
            );

            // Calculate the other room's launch position (where the door in the adjacent room launches from)
            Door.DoorDirection oppositeDirection = GetOppositeDirection(direction);
            Vector2Int otherRoomDoorLaunchPos = RoomTileHelper.GetDoorLocation(adjacentRoom.tileLayout, oppositeDirection, true);
            otherRoomLaunchWorldPos = new Vector3(
                adjacentRoom.roomCoordinate.x * adjacentRoom.roomLength + otherRoomDoorLaunchPos.x + 0.5f,
                adjacentRoom.roomCoordinate.y * adjacentRoom.roomLength + otherRoomDoorLaunchPos.y + 0.5f,
                -0.1f
            );
        }

        // Set door launch and landing positions in door data
        doorData.launchPosition = doorLaunchWorldPos;
        doorData.landingPosition = doorLandingWorldPos;
        doorData.otherRoomLaunchPosition = otherRoomLaunchWorldPos;

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

    // Combines all individual room tile layouts into a single 2D char array representing the entire zone
    private char[,] CombineRoomTileLayouts()
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room graph is null - cannot combine tile layouts");
            return null;
        }

        int roomGraphWidth = roomGraph.GetLength(0);
        int roomGraphHeight = roomGraph.GetLength(1);

        // Find a room to get the room length (assuming all rooms are the same size)
        int roomLength = 32; // Default fallback
        for (int x = 0; x < roomGraphWidth && roomLength == 32; x++)
        {
            for (int y = 0; y < roomGraphHeight && roomLength == 32; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null && room.tileLayout != null)
                {
                    roomLength = room.roomLength;
                    break;
                }
            }
        }

        // Calculate the total size of the combined tile layout
        int totalWidth = roomGraphWidth * roomLength;
        int totalHeight = roomGraphHeight * roomLength;

        // Create the combined tile layout (false = water by default)
        char[,] combinedLayout = new char[totalWidth, totalHeight];

        // Copy each room's tile layout into the appropriate position in the combined layout
        for (int roomX = 0; roomX < roomGraphWidth; roomX++)
        {
            for (int roomY = 0; roomY < roomGraphHeight; roomY++)
            {
                RoomData room = roomGraph[roomX, roomY];
                if (room != null && room.tileLayout != null)
                {
                    // Calculate the offset for this room in the combined layout
                    int offsetX = roomX * roomLength;
                    int offsetY = roomY * roomLength;

                    // Copy this room's tile layout to the combined layout
                    for (int tileX = 0; tileX < roomLength; tileX++)
                    {
                        for (int tileY = 0; tileY < roomLength; tileY++)
                        {
                            if (tileX < room.tileLayout.GetLength(0) && tileY < room.tileLayout.GetLength(1))
                            {
                                combinedLayout[offsetX + tileX, offsetY + tileY] = room.tileLayout[tileX, tileY];
                            }
                        }
                    }
                }
                // If room is null, that area remains water (false) by default
            }
        }

        Debug.Log($"Combined tile layouts into {totalWidth}x{totalHeight} array");
        return combinedLayout;
    }
    

    // Forces the Cinemachine camera to immediately update to the current player position
    private void ForceCameraUpdate()
    {
        // Find and update all active CinemachineCamera components
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera cam in cameras)
        {
            if (cam.isActiveAndEnabled)
            {
                // Force the camera to update its position immediately
                cam.ForceCameraPosition(PlayerMovement.Instance.transform.position, Quaternion.identity);
                
                // Manually update the camera's internal state
                cam.UpdateCameraState(Vector3.up, Time.deltaTime);
            }
        }
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