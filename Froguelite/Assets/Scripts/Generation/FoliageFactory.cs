using System.Collections.Generic;
using UnityEngine;

public class FoliageFactory : MonoBehaviour
{

    // FoliageFactory generates foliage instances in rooms using Poisson-disc sampling


    #region VARIABLES


    [System.Serializable]
    public class FoliageSpawnGroup
    {
        public string groupName;
        public FoliageSpawnParams[] foliageSpawnParams;

        [Header("Poisson-Disc Sampling Parameters")]
        public float minDistance = 1.5f; // Minimum distance between foliage in this group
        public int maxSamplingAttempts = 30; // Maximum attempts to place each foliage piece for this group
        public bool isLandGroup = true; // Whether this group spawns on land (true) or water (false)

        [Header("Perlin Filtering")]
        public bool usePerlinFiltering = false; // Whether to apply Perlin noise filtering to samples
        public bool matchWithRoomNoise = false; // If true, uses the same noise pattern as room generation
        public float perlinScale = 0.1f; // Scale of the Perlin noise (smaller = larger noise patterns)
        public float perlinThreshold = 0.5f; // Minimum noise value required to keep a sample (0-1)
        public Vector2 perlinOffset = Vector2.zero; // Offset for the Perlin noise sampling
    }

    [System.Serializable]
    public class FoliageSpawnParams
    {
        public Foliage foliagePrefab;
        public float relativeWeight; // Relative weight for selection (higher = more likely)
    }

    // List of foliage prefab groups - each group has its own sampling parameters
    [SerializeField] private FoliageSpawnGroup[] foliageGroups;


    #endregion


    #region ROOM FOLIAGE GENERATION


    // Generates foliage for the given room using Poisson-disc sampling
    public void GenerateFoliageForRoom(Room room, float landFoliageDensityScale)
    {
        // Get room bounds in world coordinates
        Vector3 roomWorldPos = new Vector3(
            room.roomData.roomCoordinate.x * room.roomData.roomLength,
            room.roomData.roomCoordinate.y * room.roomData.roomLength,
            0
        );
        float roomSize = room.roomData.roomLength;

        // Generate foliage for each group separately with their own sampling parameters
        foreach (FoliageSpawnGroup group in foliageGroups)
        {
            if (group.foliageSpawnParams == null || group.foliageSpawnParams.Length == 0)
                continue;

            // Calculate adjusted minimum distance based on density scale (only for land groups)
            float adjustedMinDistance = group.isLandGroup ? 
            group.minDistance * landFoliageDensityScale : 
            group.minDistance;

            // Generate sample points for this group
            List<Vector2> samplePoints = GeneratePoissonDiscSamples(
                roomWorldPos, roomSize, adjustedMinDistance, group.maxSamplingAttempts
            );

            // Apply Perlin filtering if enabled for this group
            if (group.usePerlinFiltering)
            {
                samplePoints = ApplyPerlinFiltering(samplePoints, group, room.roomData);
            }

            foreach (Vector2 samplePoint in samplePoints)
            {
                // Convert world position to tile coordinates to check if it's valid
                Vector2Int tileCoord = WorldToTileCoordinate(samplePoint, room.roomData);
                
                bool isValidPosition = group.isLandGroup ? 
                    IsValidLandFoliagePosition(tileCoord, room.roomData) :
                    IsValidWaterFoliagePosition(tileCoord, room.roomData);

                if (isValidPosition)
                {
                    Foliage selectedFoliage = SelectFoliageByWeight(group.foliageSpawnParams);
                    if (selectedFoliage != null)
                    {
                        SpawnFoliageAtWorldPosition(selectedFoliage, room, samplePoint);
                    }
                }
            }
        }
    }


    // Spawns a foliage instance at the specified world position
    private void SpawnFoliageAtWorldPosition(Foliage foliagePrefab, Room room, Vector2 worldPosition)
    {
        Vector3 spawnPosition = new Vector3(worldPosition.x, worldPosition.y, -0.1f);
        spawnPosition += new Vector3(Random.Range(-.2f, .2f), Random.Range(-.2f, .2f), 0); // Small random offset
        Foliage spawnedFoliage = Instantiate(foliagePrefab, room.transform);
        spawnedFoliage.transform.position = spawnPosition;
    }


    #endregion


    #region POISSON-DISC SAMPLING


    // Generates sample points using Poisson-disc sampling within the given bounds
    private List<Vector2> GeneratePoissonDiscSamples(Vector3 roomWorldPos, float roomSize, float minDistance, int maxAttempts)
    {
        List<Vector2> samplePoints = new List<Vector2>();
        List<Vector2> activeList = new List<Vector2>();

        // Grid for spatial acceleration
        float cellSize = minDistance / Mathf.Sqrt(2);
        int gridWidth = Mathf.CeilToInt(roomSize / cellSize);
        int gridHeight = Mathf.CeilToInt(roomSize / cellSize);
        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        bool[,] gridOccupied = new bool[gridWidth, gridHeight];

        // Helper function to get grid coordinates
        System.Func<Vector2, Vector2Int> getGridCoord = (point) =>
        {
            Vector2 localPoint = point - new Vector2(roomWorldPos.x, roomWorldPos.y);
            return new Vector2Int(
                Mathf.FloorToInt(localPoint.x / cellSize),
                Mathf.FloorToInt(localPoint.y / cellSize)
            );
        };

        // Generate initial sample
        Vector2 initialSample = new Vector2(
            roomWorldPos.x + Random.Range(0f, roomSize),
            roomWorldPos.y + Random.Range(0f, roomSize)
        );

        samplePoints.Add(initialSample);
        activeList.Add(initialSample);

        Vector2Int initialGridCoord = getGridCoord(initialSample);
        if (initialGridCoord.x >= 0 && initialGridCoord.x < gridWidth && 
            initialGridCoord.y >= 0 && initialGridCoord.y < gridHeight)
        {
            grid[initialGridCoord.x, initialGridCoord.y] = initialSample;
            gridOccupied[initialGridCoord.x, initialGridCoord.y] = true;
        }

        // Generate samples
        while (activeList.Count > 0)
        {
            int randomIndex = Random.Range(0, activeList.Count);
            Vector2 currentPoint = activeList[randomIndex];
            bool foundValidSample = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Generate candidate sample in annulus between minDistance and 2*minDistance
                float angle = Random.Range(0f, 2f * Mathf.PI);
                float distance = Random.Range(minDistance, 2f * minDistance);
                Vector2 candidate = currentPoint + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );

                // Check if candidate is within room bounds
                if (candidate.x < roomWorldPos.x || candidate.x >= roomWorldPos.x + roomSize ||
                    candidate.y < roomWorldPos.y || candidate.y >= roomWorldPos.y + roomSize)
                    continue;

                // Check if candidate is far enough from existing samples
                if (IsValidPoissonSample(candidate, grid, gridOccupied, gridWidth, gridHeight, cellSize, minDistance, roomWorldPos))
                {
                    samplePoints.Add(candidate);
                    activeList.Add(candidate);

                    Vector2Int gridCoord = getGridCoord(candidate);
                    if (gridCoord.x >= 0 && gridCoord.x < gridWidth && 
                        gridCoord.y >= 0 && gridCoord.y < gridHeight)
                    {
                        grid[gridCoord.x, gridCoord.y] = candidate;
                        gridOccupied[gridCoord.x, gridCoord.y] = true;
                    }

                    foundValidSample = true;
                    break;
                }
            }

            if (!foundValidSample)
            {
                activeList.RemoveAt(randomIndex);
            }
        }

        return samplePoints;
    }


    // Checks if a sample point is valid for Poisson-disc sampling
    private bool IsValidPoissonSample(Vector2 candidate, Vector2[,] grid, bool[,] gridOccupied,
        int gridWidth, int gridHeight, float cellSize, float minDistance, Vector3 roomWorldPos)
    {
        Vector2 localCandidate = candidate - new Vector2(roomWorldPos.x, roomWorldPos.y);
        Vector2Int gridCoord = new Vector2Int(
            Mathf.FloorToInt(localCandidate.x / cellSize),
            Mathf.FloorToInt(localCandidate.y / cellSize)
        );

        // Check surrounding grid cells
        for (int x = Mathf.Max(0, gridCoord.x - 1); x <= Mathf.Min(gridWidth - 1, gridCoord.x + 1); x++)
        {
            for (int y = Mathf.Max(0, gridCoord.y - 1); y <= Mathf.Min(gridHeight - 1, gridCoord.y + 1); y++)
            {
                if (gridOccupied[x, y])
                {
                    Vector2 existingPoint = grid[x, y];
                    if (Vector2.Distance(candidate, existingPoint) < minDistance)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }


    #endregion


    #region HELPER METHODS


    // Converts world position to tile coordinate within the room
    private Vector2Int WorldToTileCoordinate(Vector2 worldPosition, RoomData roomData)
    {
        Vector2 localPosition = worldPosition - new Vector2(
            roomData.roomCoordinate.x * roomData.roomLength,
            roomData.roomCoordinate.y * roomData.roomLength
        );

        return new Vector2Int(
            Mathf.FloorToInt(localPosition.x),
            Mathf.FloorToInt(localPosition.y)
        );
    }


    // Checks if the tile coordinate is valid for land foliage placement
    private bool IsValidLandFoliagePosition(Vector2Int tileCoord, RoomData roomData)
    {
        // Check bounds
        if (tileCoord.x < 0 || tileCoord.x >= roomData.roomLength ||
            tileCoord.y < 0 || tileCoord.y >= roomData.roomLength)
            return false;

        // Check if it's land and not bordering water
        return roomData.tileLayout[tileCoord.x, tileCoord.y] &&
               !roomData.IsTileBorderingChange(tileCoord);
    }


    // Checks if the tile coordinate is valid for water foliage placement
    private bool IsValidWaterFoliagePosition(Vector2Int tileCoord, RoomData roomData)
    {
        // Check bounds
        if (tileCoord.x < 0 || tileCoord.x >= roomData.roomLength ||
            tileCoord.y < 0 || tileCoord.y >= roomData.roomLength)
            return false;

        // Check if it's water and not bordering land
        return !roomData.tileLayout[tileCoord.x, tileCoord.y] &&
               !roomData.IsTileBorderingChange(tileCoord);
    }


    // Applies Perlin noise filtering to sample points, removing samples below the threshold
    private List<Vector2> ApplyPerlinFiltering(List<Vector2> samplePoints, FoliageSpawnGroup group, RoomData roomData)
    {
        List<Vector2> filteredPoints = new List<Vector2>();

        foreach (Vector2 samplePoint in samplePoints)
        {
            if (group.matchWithRoomNoise)
            {
                // Use the same noise pattern as room generation
                if (SampleRoomNoise(samplePoint, roomData, group.perlinThreshold))
                {
                    filteredPoints.Add(samplePoint);
                }
            }
            else
            {
                // Use simple Perlin noise with group settings
                float noiseX = (samplePoint.x + group.perlinOffset.x) * group.perlinScale;
                float noiseY = (samplePoint.y + group.perlinOffset.y) * group.perlinScale;
                float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

                // Keep the sample if the noise value is above the threshold
                if (noiseValue >= group.perlinThreshold)
                {
                    filteredPoints.Add(samplePoint);
                }
            }
        }

        return filteredPoints;
    }

    // Samples the same noise pattern used for room generation at a given world position
    private bool SampleRoomNoise(Vector2 worldPosition, RoomData roomData, float threshold)
    {
        PerlinNoiseSettings sampleSettings = roomData.originalNoiseSettings;
        sampleSettings.threshold = threshold;

        bool meetsThreshold = RoomTileHelper.CoordinateMeetsTileGenThreshold(
            worldPosition,
            sampleSettings,
            roomData.roomLength,
            roomData.roomLength,
            roomData.roomCoordinate.x * roomData.roomLength,
            roomData.roomCoordinate.y * roomData.roomLength
        );

        return meetsThreshold;
    }

    // Selects a foliage prefab based on relative weights
    private Foliage SelectFoliageByWeight(FoliageSpawnParams[] foliageOptions)
    {
        if (foliageOptions == null || foliageOptions.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (var option in foliageOptions)
        {
            totalWeight += option.relativeWeight;
        }

        if (totalWeight <= 0f)
            return foliageOptions[0].foliagePrefab; // Fallback to first option

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var option in foliageOptions)
        {
            currentWeight += option.relativeWeight;
            if (randomValue <= currentWeight)
            {
                return option.foliagePrefab;
            }
        }

        return foliageOptions[foliageOptions.Length - 1].foliagePrefab; // Fallback to last option
    }


    #endregion


}
