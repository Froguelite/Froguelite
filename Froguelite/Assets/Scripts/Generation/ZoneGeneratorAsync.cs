using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NavMeshPlus.Components;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ZoneGeneratorAsync : MonoBehaviour
{
    #region VARIABLES

    public static ZoneGeneratorAsync Instance;
    public bool zoneGenerated { get; private set; } = false;
    public bool isGenerating { get; private set; } = false;

    [SerializeField] private bool generateZoneOnStart = true;
    [SerializeField] private int generateZoneOnStartZone = 0;
    [SerializeField] private int generateZoneOnStartSubZone = 0;
    [SerializeField] private bool teleportPlayerToStarterRoom = true;
    
    [SerializeField] private RoomFactory roomFactory;
    [SerializeField] private FoliageFactory foliageFactory;
    [SerializeField] private Tilemap roomsTilemap;
    [SerializeField] private NavMeshSurface navigationSurface;
    [SerializeField] private AutoTileSet zone1AutoTileSet;
    [SerializeField] private AutoTileSet zone2AutoTileSet;
    [SerializeField] private Transform roomParent;

    [SerializeField] private Door swampDoorPrefab;
    [SerializeField] private Door forestDoorPrefab;

    // Progress tracking
    [SerializeField] private int maxRoomsPerFrame = 2; // Number of rooms to process per frame
    [SerializeField] private int maxFoliageGroupsPerFrame = 1; // Number of foliage groups per frame
    [SerializeField] private bool enableGarbageCollection = true; // Enable periodic GC during generation
    [SerializeField] private int gcInterval = 8; // GC every N rooms (0 = disabled)
    [SerializeField] private bool enableDiagnostics = false; // Enable performance diagnostics

    private RoomData[,] roomGraph;
    private Dictionary<Vector2Int, Room> spawnedRooms = new Dictionary<Vector2Int, Room>();
    private char[,] combinedTileLayout;

    // Events for progress tracking
    public event Action<float> OnGenerationProgress; // 0-1 progress value

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
        {
            LevelManager.Instance.ManuallySetCurrentZone(generateZoneOnStartZone);
            StartCoroutine(GenerateZoneAsync(generateZoneOnStartZone, generateZoneOnStartSubZone));
        }
    }

    #endregion

    #region ASYNC GENERATION

    /// <summary>
    /// Main async generation coroutine that spreads work across multiple frames
    /// </summary>
    public IEnumerator GenerateZoneAsync(int zone, int subZone, Action onComplete = null)
    {
        Debug.Log("Generating Zone: " + zone + ", SubZone: " + subZone);

        if (isGenerating)
        {
            Debug.LogWarning("Zone generation already in progress!");
            yield break;
        }

        isGenerating = true;
        zoneGenerated = false;
        
        if (enableDiagnostics && GenerationDiagnostics.Instance != null)
            GenerationDiagnostics.Instance.StartPhase("Zone Generation");
        
        // TODO: Temporary, chooses a random seed every time
        UnityEngine.Random.InitState(UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        // Step 1: Generate room graph (this is fast, can be done immediately)
        OnGenerationProgress?.Invoke(0.05f);
        roomGraph = RoomGraphGenerator.GetRoomGraph(zone, 8, subZone);
        yield return null; // Yield to prevent frame spike

        Debug.Log("Step 1: Room graph successfully generated.");

        // Step 2: Spawn rooms across multiple frames
        OnGenerationProgress?.Invoke(0.10f);
        yield return StartCoroutine(SpawnRoomsFromGraphAsync(zone));

        Debug.Log("Step 2: Rooms successfully spawned.");

        // Step 3: Combine tile layouts (relatively fast)
        OnGenerationProgress?.Invoke(0.70f);
        combinedTileLayout = CombineRoomTileLayouts();
        yield return null;

        Debug.Log("Step 3: Tile layouts successfully combined.");

        // Step 4: Initialize minimap
        OnGenerationProgress?.Invoke(0.75f);
        MinimapManager.Instance.InitializeMinimap(combinedTileLayout);
        yield return null;

        Debug.Log("Step 4: Minimap successfully initialized.");

        // Step 5: Initialize RoomManager
        OnGenerationProgress?.Invoke(0.80f);
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.Initialize(roomGraph, spawnedRooms);
        }
        else
        {
            Debug.LogWarning("RoomManager Instance is null - rooms will not be managed properly");
        }
        yield return null;

        Debug.Log("Step 5: RoomManager successfully initialized.");

        // Step 6: Set player to starter room
        OnGenerationProgress?.Invoke(0.85f);
        if (teleportPlayerToStarterRoom)
            SetPlayerToStarterRoomAndClear();
        yield return null;

        Debug.Log("Step 6: Player successfully teleported to starter room.");

        // Step 7: Build NavMesh (can be heavy, spread across frames)
        OnGenerationProgress?.Invoke(0.90f);
        yield return StartCoroutine(BuildNavMeshAsync());

        Debug.Log("Step 7: NavMesh successfully built.");

        OnGenerationProgress?.Invoke(1.0f);
        
        if (enableDiagnostics && GenerationDiagnostics.Instance != null)
        {
            GenerationDiagnostics.Instance.EndPhase("Zone Generation");
            GenerationDiagnostics.Instance.LogDetailedMemoryReport();
        }
        
        // Final cleanup to free memory
        if (enableGarbageCollection)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
        yield return null;
        
        zoneGenerated = true;
        isGenerating = false;

        Debug.Log("Successfully finished generating");

        if (generateZoneOnStart)
        {
            FindAnyObjectByType<AmbiantParticleHandler>()?.ResetAmbiantParticles(true, zone);
        }
        
        onComplete?.Invoke();
    }

    /// <summary>
    /// Spawns rooms across multiple frames to prevent freezing
    /// </summary>
    private IEnumerator SpawnRoomsFromGraphAsync(int zone)
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room Graph is null - cannot spawn rooms");
            yield break;
        }

        int width = roomGraph.GetLength(0);
        int height = roomGraph.GetLength(1);
        int totalRooms = width * height;
        int processedRooms = 0;

        Debug.Log($"=== SPAWNING ROOMS FROM GRAPH ({width}x{height}) ===");

        // Create room parent if it doesn't exist
        if (roomParent == null)
        {
            GameObject parentObject = new GameObject("Generated Rooms");
            roomParent = parentObject.transform;
        }

        // First pass: Spawn all rooms (spread across frames)
        // Optimization: Only spawn water rooms adjacent to land rooms
        int batchIndex = 0;
        int skippedRooms = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomData room = roomGraph[x, y];
                Vector2Int position = new Vector2Int(x, y);
                
                if (room != null)
                {
                    // Land-containing room - always generate
                    yield return StartCoroutine(SpawnRoomAsync(zone, room, position, batchIndex));
                }
                else if (ShouldGenerateWaterRoom(position))
                {
                    // Water room adjacent to land - generate as buffer
                    SpawnEmptyRoom(zone, position);
                }
                else
                {
                    // Water room far from land - skip generation
                    skippedRooms++;
                    // Don't add to spawnedRooms, don't increment batchIndex
                    continue; // Skip to next iteration
                }

                processedRooms++;
                batchIndex++;

                // Yield and cleanup every N rooms to prevent memory buildup
                if (batchIndex % maxRoomsPerFrame == 0)
                {
                    float progress = 0.10f + (0.50f * processedRooms / totalRooms); // 10% to 60%
                    OnGenerationProgress?.Invoke(progress);
                    
                    if (enableDiagnostics && GenerationDiagnostics.Instance != null)
                        GenerationDiagnostics.Instance.CheckFrame($"(Room {processedRooms}/{totalRooms})");
                    
                    // Force garbage collection periodically to prevent memory spikes
                    if (enableGarbageCollection && gcInterval > 0 && batchIndex % gcInterval == 0)
                    {
                        Resources.UnloadUnusedAssets();
                        System.GC.Collect();
                    }
                    
                    yield return null;
                }
            }
        }

        Debug.Log($"Successfully spawned {spawnedRooms.Count} rooms (skipped {skippedRooms} unnecessary water rooms)");

        // Second pass: Spawn all doors (also spread across frames)
        processedRooms = 0;
        batchIndex = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null)
                {
                    SpawnDoorsForRoom(zone, room, new Vector2Int(x, y));
                    
                    processedRooms++;
                    batchIndex++;

                    if (batchIndex % maxRoomsPerFrame == 0)
                    {
                        float progress = 0.60f + (0.10f * processedRooms / totalRooms); // 60% to 70%
                        OnGenerationProgress?.Invoke(progress);
                        
                        // Periodic garbage collection
                        if (enableGarbageCollection && gcInterval > 0 && batchIndex % gcInterval == 0)
                        {
                            System.GC.Collect();
                        }
                        
                        yield return null;
                    }
                }
            }
        }

        Debug.Log($"Successfully spawned doors for all rooms");
    }

    /// <summary>
    /// Builds NavMesh asynchronously using Unity's async operations
    /// </summary>
    private IEnumerator BuildNavMeshAsync()
    {
        // NavMesh building can be heavy - start the operation and yield
        var asyncOp = navigationSurface.BuildNavMeshAsync();
        
        // Wait for completion while allowing other operations
        while (!asyncOp.isDone)
        {
            yield return null;
        }
    }

    #endregion

    #region SPAWNING ROOMS

    private IEnumerator SpawnRoomAsync(int zone, RoomData roomData, Vector2Int gridPosition, int batchIndex)
    {
        AutoTileSet autoTileSet = (zone == 0) ? zone1AutoTileSet : zone2AutoTileSet;
        Room spawnedRoom = roomFactory.SpawnRoom(zone, roomsTilemap, autoTileSet, roomParent, roomData, 32);
        spawnedRooms[gridPosition] = spawnedRoom;

        // Yield after room creation to prevent frame spikes
        if (batchIndex % maxRoomsPerFrame == 0)
            yield return null;

        // Generate foliage for the room
        float foliageLandDensity = 1f;

        if (roomData.roomType == Room.RoomType.SubZoneBoss || 
            roomData.roomType == Room.RoomType.BossPortal || 
            roomData.roomType == Room.RoomType.Fly || 
            roomData.roomType == Room.RoomType.Shop)
        {
            foliageLandDensity = 5f; // Less foliage in special rooms
        }

        foliageFactory.GenerateFoliageForRoom(zone, spawnedRoom, foliageLandDensity);
    }

    private void SpawnEmptyRoom(int zone, Vector2Int gridPosition)
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room Graph is null - cannot spawn empty room");
            return;
        }

        int roomLength = 32;
        
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

        Vector2Int tileOffset = new Vector2Int(
            gridPosition.x * roomLength,
            gridPosition.y * roomLength
        );

        char[,] waterLayout = new char[roomLength, roomLength];
        
        for (int x = 0; x < roomLength; x++)
        {
            for (int y = 0; y < roomLength; y++)
            {
                waterLayout[x, y] = 'w';
            }
        }

        AutoTileSet autoTileSet = (zone == 0) ? zone1AutoTileSet : zone2AutoTileSet;

        RoomTileHelper.SetTilemapToLayoutWithAutoTiling(
            waterLayout,
            roomsTilemap,
            autoTileSet,
            tileOffset.x,
            tileOffset.y
        );

        // Create a RoomData for this water-only room for minimap and other systems
        RoomData waterRoomData = new RoomData(Room.RoomType.Normal, gridPosition, zone);
        waterRoomData.tileLayout = waterLayout;
        waterRoomData.roomLength = roomLength;

        // Create a Room component to track this position
        GameObject roomObject = new GameObject($"Room_Water_{gridPosition.x}_{gridPosition.y}");
        Room roomComponent = roomObject.AddComponent<Room>();
        roomComponent.Initialize(waterRoomData);
        roomObject.transform.SetParent(roomParent);

        // Add to spawned rooms dictionary
        spawnedRooms[gridPosition] = roomComponent;
    }

    #endregion

    #region SPAWNING DOORS

    private void SpawnDoorsForRoom(int zone, RoomData roomData, Vector2Int gridPosition)
    {
        if (!spawnedRooms.ContainsKey(gridPosition))
        {
            Debug.LogError($"Room at {gridPosition} not found in spawned rooms");
            return;
        }

        foreach (var doorEntry in roomData.doors)
        {
            Door.DoorDirection direction = doorEntry.Key;
            DoorData doorData = doorEntry.Value;

            if (!doorData.isImpassable)
            {
                SpawnDoor(zone, roomData, direction, doorData, gridPosition);
            }
        }
    }

    private void SpawnDoor(int zone, RoomData roomData, Door.DoorDirection direction, DoorData doorData, Vector2Int roomGridPos)
    {
        Door doorPrefab = null;
        switch (zone)
        {
            case 0:
                doorPrefab = swampDoorPrefab;
                break;
            case 1:
                doorPrefab = forestDoorPrefab;
                break;
        }

        if (doorPrefab == null || roomData.tileLayout == null)
        {
            Debug.LogError("Door prefab or room tile layout is null");
            return;
        }

        Vector2Int doorLaunchPos = RoomTileHelper.GetDoorLocation(roomData.tileLayout, direction, true);
        Vector2Int adjacentRoomGridPos = GetAdjacentRoomPosition(roomGridPos, direction);
        Vector2Int doorLandingPos = Vector2Int.zero;
        
        if (IsValidRoomPosition(adjacentRoomGridPos) && roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y] != null)
        {
            RoomData adjacentRoom = roomGraph[adjacentRoomGridPos.x, adjacentRoomGridPos.y];
            Door.DoorDirection oppositeDirection = GetOppositeDirection(direction);
            doorLandingPos = RoomTileHelper.GetDoorLocation(adjacentRoom.tileLayout, oppositeDirection, false);
        }

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

            Door.DoorDirection oppositeDirection = GetOppositeDirection(direction);
            Vector2Int otherRoomDoorLaunchPos = RoomTileHelper.GetDoorLocation(adjacentRoom.tileLayout, oppositeDirection, true);
            otherRoomLaunchWorldPos = new Vector3(
                adjacentRoom.roomCoordinate.x * adjacentRoom.roomLength + otherRoomDoorLaunchPos.x + 0.5f,
                adjacentRoom.roomCoordinate.y * adjacentRoom.roomLength + otherRoomDoorLaunchPos.y + 0.5f,
                -0.1f
            );
        }

        doorData.launchPosition = doorLaunchWorldPos;
        doorData.landingPosition = doorLandingWorldPos;
        doorData.otherRoomLaunchPosition = otherRoomLaunchWorldPos;

        Door doorInstance = Instantiate(doorPrefab, doorLaunchWorldPos, Quaternion.identity);

        if (!spawnedRooms.ContainsKey(roomGridPos))
        {
            Debug.LogError($"Room at {roomGridPos} not found in spawned rooms");
            return;
        }

        Room roomObject = spawnedRooms[roomGridPos];
        doorInstance.transform.SetParent(roomObject.transform);
        roomObject.AddDoor(doorInstance);
        doorInstance.InitializeDoorData(doorData);
    }

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Determines if a position needs a water room generated
    /// Only generate water rooms if adjacent to a land-containing room
    /// </summary>
    private bool ShouldGenerateWaterRoom(Vector2Int position)
    {
        if (roomGraph == null) return false;

        int width = roomGraph.GetLength(0);
        int height = roomGraph.GetLength(1);

        // Check all adjacent positions (4-directional)
        Vector2Int[] adjacentOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0)    // Right
        };

        foreach (Vector2Int offset in adjacentOffsets)
        {
            Vector2Int adjacentPos = position + offset;

            // Check if adjacent position is valid and has a room
            if (adjacentPos.x >= 0 && adjacentPos.x < width &&
                adjacentPos.y >= 0 && adjacentPos.y < height)
            {
                RoomData adjacentRoom = roomGraph[adjacentPos.x, adjacentPos.y];

                // If adjacent room exists and has land, we need this water room as buffer
                if (adjacentRoom != null)
                {
                    return true;
                }
            }
        }

        return false; // No adjacent land rooms, skip this water room
    }

    private void SetPlayerToStarterRoomAndClear()
    {
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

        Vector3 starterRoomCenter = new Vector3(
            starterRoom.roomCoordinate.x * starterRoom.roomLength + starterRoom.roomLength * 0.5f,
            starterRoom.roomCoordinate.y * starterRoom.roomLength + starterRoom.roomLength * 0.5f,
            PlayerMovement.Instance.transform.position.z
        );

        PlayerMovement.Instance.transform.position = starterRoomCenter;
        ForceCameraUpdate();
        RoomManager.Instance.GetRoomAtWorldPosition(starterRoomCenter)?.OnRoomCleared();
    }

    private Vector2Int GetAdjacentRoomPosition(Vector2Int roomGridPos, Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up: return roomGridPos + new Vector2Int(0, 1);
            case Door.DoorDirection.Down: return roomGridPos + new Vector2Int(0, -1);
            case Door.DoorDirection.Left: return roomGridPos + new Vector2Int(-1, 0);
            case Door.DoorDirection.Right: return roomGridPos + new Vector2Int(1, 0);
            default: return roomGridPos;
        }
    }

    private Door.DoorDirection GetOppositeDirection(Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up: return Door.DoorDirection.Down;
            case Door.DoorDirection.Down: return Door.DoorDirection.Up;
            case Door.DoorDirection.Left: return Door.DoorDirection.Right;
            case Door.DoorDirection.Right: return Door.DoorDirection.Left;
            default: return direction;
        }
    }

    private bool IsValidRoomPosition(Vector2Int position)
    {
        if (roomGraph == null) return false;
        return position.x >= 0 && position.x < roomGraph.GetLength(0) &&
               position.y >= 0 && position.y < roomGraph.GetLength(1);
    }

    private char[,] CombineRoomTileLayouts()
    {
        if (roomGraph == null)
        {
            Debug.LogError("Room graph is null - cannot combine tile layouts");
            return null;
        }

        int roomGraphWidth = roomGraph.GetLength(0);
        int roomGraphHeight = roomGraph.GetLength(1);
        int roomLength = 32;
        
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

        int totalWidth = roomGraphWidth * roomLength;
        int totalHeight = roomGraphHeight * roomLength;
        char[,] combinedLayout = new char[totalWidth, totalHeight];

        for (int roomX = 0; roomX < roomGraphWidth; roomX++)
        {
            for (int roomY = 0; roomY < roomGraphHeight; roomY++)
            {
                Vector2Int position = new Vector2Int(roomX, roomY);
                
                // Check if this position was actually spawned
                if (spawnedRooms.ContainsKey(position))
                {
                    Room spawnedRoom = spawnedRooms[position];
                    if (spawnedRoom.roomData != null && spawnedRoom.roomData.tileLayout != null)
                    {
                        int offsetX = roomX * roomLength;
                        int offsetY = roomY * roomLength;

                        for (int tileX = 0; tileX < roomLength; tileX++)
                        {
                            for (int tileY = 0; tileY < roomLength; tileY++)
                            {
                                if (tileX < spawnedRoom.roomData.tileLayout.GetLength(0) && 
                                    tileY < spawnedRoom.roomData.tileLayout.GetLength(1))
                                {
                                    combinedLayout[offsetX + tileX, offsetY + tileY] = 
                                        spawnedRoom.roomData.tileLayout[tileX, tileY];
                                }
                            }
                        }
                    }
                }
                // If position not spawned, area remains water (default char value)
            }
        }

        Debug.Log($"Combined tile layouts into {totalWidth}x{totalHeight} array");
        return combinedLayout;
    }

    private void ForceCameraUpdate()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera cam in cameras)
        {
            if (cam.isActiveAndEnabled)
            {
                cam.Follow = PlayerMovement.Instance.transform;
                cam.ForceCameraPosition(PlayerMovement.Instance.transform.position, Quaternion.identity);
                cam.UpdateCameraState(Vector3.up, Time.deltaTime);
            }
        }
    }

    #endregion

    #region TESTERS

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
