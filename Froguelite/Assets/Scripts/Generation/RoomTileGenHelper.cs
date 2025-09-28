using UnityEngine;
using UnityEngine.Tilemaps;

public static class RoomTileGenHelper
{
    // RoomTileGenHelper is a static class to help with room tile generation


    #region TILE GENERATION


    // Generates a room layout using Perlin noise (with octaves)
    // Returns a 2d bool array of where land should be
    public static bool[,] GenRoomTiles(
        int width,
        int height,
        int offsetX,
        int offsetY,
        int octaves = 3,
        float persistence = 0.5f,
        float lacunarity = 2f,
        float noiseScale = 0.1f,
        float threshold = 0.4f,
        float landScale = 1f)
    {
        // Generate random offsets for each octave
        float[] octaveOffsetsX = new float[octaves];
        float[] octaveOffsetsY = new float[octaves];

        for (int i = 0; i < octaves; i++)
        {
            octaveOffsetsX[i] = Random.Range(-1000f, 1000f);
            octaveOffsetsY[i] = Random.Range(-1000f, 1000f);
        }

        bool[,] roomLayout = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = 0f;
                float amplitude = 1f;
                float frequency = noiseScale;

                // Combine multiple octaves of noise
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x + octaveOffsetsX[i] + offsetX) * frequency;
                    float sampleY = (y + octaveOffsetsY[i] + offsetY) * frequency;

                    float octaveValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseValue += octaveValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Normalize the noise value
                noiseValue = Mathf.Clamp01(noiseValue);

                // Apply radial falloff for island shape
                float centerX = width * 0.5f;
                float centerY = height * 0.5f;
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float maxDistance = Mathf.Min(width, height) * 0.5f;
                
                // Apply land scaling - smaller landScale makes land area smaller
                float scaledDistance = distanceFromCenter / landScale;
                float falloff = 1f - Mathf.Clamp01(scaledDistance / maxDistance);

                // Apply a smooth falloff curve
                falloff = Mathf.SmoothStep(0f, 1f, falloff);

                float finalValue = noiseValue * falloff;
                roomLayout[x, y] = finalValue > threshold;
            }
        }

        return roomLayout;
    }


    #endregion


    #region POST PROCESSING


    // Applies smoothing to given room layout
    public static bool[,] SmoothRoomLayout(bool[,] roomLayout, int iterations = 1)
    {
        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        for (int iter = 0; iter < iterations; iter++)
        {
            bool[,] smoothedLayout = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int solidNeighbors = 0;
                    int totalNeighbors = 0;

                    // Check 3x3 area around current tile
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        for (int offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            int neighborX = x + offsetX;
                            int neighborY = y + offsetY;

                            if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                            {
                                if (roomLayout[neighborX, neighborY])
                                    solidNeighbors++;
                                totalNeighbors++;
                            }
                            else
                            {
                                // Treat out-of-bounds as solid for border smoothing
                                solidNeighbors++;
                                totalNeighbors++;
                            }
                        }
                    }

                    // Apply smoothing rule: if more than half neighbors are solid, make this solid
                    smoothedLayout[x, y] = solidNeighbors > totalNeighbors / 2;
                }
            }

            roomLayout = smoothedLayout;
        }

        return roomLayout;
    }

    // Ensures the room layout is a single connected component
    // Removes any isolated sections of land
    public static bool[,] EnsureConnectivity(bool[,] roomLayout)
    {
        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        bool[,] visited = new bool[width, height];
        bool[,] result = new bool[width, height];

        // Find center point that is solid to start flood fill
        int centerX = width / 2;
        int centerY = height / 2;

        // Search for nearest solid tile to center if center is not solid
        if (!roomLayout[centerX, centerY])
        {
            float minDistance = float.MaxValue;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (roomLayout[x, y])
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            centerX = x;
                            centerY = y;
                        }
                    }
                }
            }
        }

        // Flood fill from center to find main connected component
        if (roomLayout[centerX, centerY])
        {
            FloodFill(roomLayout, visited, result, centerX, centerY, width, height);
        }

        return result;
    }

    private static void FloodFill(bool[,] original, bool[,] visited, bool[,] result, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || !original[x, y])
            return;

        visited[x, y] = true;
        result[x, y] = true;

        // Recursively fill adjacent tiles (4-directional)
        FloodFill(original, visited, result, x + 1, y, width, height);
        FloodFill(original, visited, result, x - 1, y, width, height);
        FloodFill(original, visited, result, x, y + 1, width, height);
        FloodFill(original, visited, result, x, y - 1, width, height);
    }


    #endregion


    #region TILEMAP HANDLING


    // Sets given tilemap to match the room layout
    // Does NOT use auto-tiling, just places land and water tiles directly (i.e. land OR water, no in-between)
    public static void SetTilemapToLayout(
        bool[,] roomLayout,
        Tilemap tilemap,
        TileBase landTile,
        TileBase waterTile,
        int xOffset,
        int yOffset)
    {
        if (roomLayout == null)
        {
            Debug.LogError("Room layout is null!");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap is null!");
            return;
        }

        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        // Create arrays for batch setting tiles (more efficient than setting one by one)
        Vector3Int[] positions = new Vector3Int[width * height];
        TileBase[] tilesToPlace = new TileBase[width * height];
        int tileIndex = 0;

        // Iterate through the layout and set tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Calculate world position with offset
                Vector3Int position = new Vector3Int(x + xOffset, y + yOffset, 0);
                positions[tileIndex] = position;

                // Determine which tile to place based on layout
                if (roomLayout[x, y])
                {
                    // Land tile
                    tilesToPlace[tileIndex] = landTile;
                }
                else
                {
                    // Water/empty tile
                    tilesToPlace[tileIndex] = waterTile;
                }

                tileIndex++;
            }
        }

        // Set all tiles at once for better performance
        tilemap.SetTiles(positions, tilesToPlace);
    }


    // Sets given tilemap to match the room layout
    // Uses auto-tiling (i.e. different tile variants based on neighbors)
    public static void SetTilemapToLayoutWithAutoTiling(
        bool[,] roomLayout,
        Tilemap tilemap,
        AutoTileSet tileSet,
        int xOffset,
        int yOffset)
    {
        if (roomLayout == null || tilemap == null || tileSet == null)
        {
            Debug.LogError("Room layout, tilemap, or tile set is null!");
            return;
        }

        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        // Create arrays for batch setting tiles
        Vector3Int[] positions = new Vector3Int[width * height];
        TileBase[] tilesToPlace = new TileBase[width * height];
        int tileIndex = 0;

        // Iterate through each position and determine the appropriate tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int position = new Vector3Int(x + xOffset, y + yOffset, 0);
                positions[tileIndex] = position;

                // Determine tile type based on neighbors
                AutoTileSet.AutoTileType tileType = DetermineAutoTileType(roomLayout, x, y, width, height);
                tilesToPlace[tileIndex] = tileSet.GetTile(tileType);

                tileIndex++;
            }
        }

        // Set all tiles at once
        tilemap.SetTiles(positions, tilesToPlace);
    }


    // Determines the appropriate auto-tile type based on the 3x3 neighborhood around a tile
    private static AutoTileSet.AutoTileType DetermineAutoTileType(bool[,] roomLayout, int x, int y, int width, int height)
    {
        // Get the 3x3 neighborhood (true = land, false = water)
        bool center = GetTileAt(roomLayout, x, y, width, height);
        bool top = GetTileAt(roomLayout, x, y + 1, width, height);
        bool bottom = GetTileAt(roomLayout, x, y - 1, width, height);
        bool left = GetTileAt(roomLayout, x - 1, y, width, height);
        bool right = GetTileAt(roomLayout, x + 1, y, width, height);
        bool topLeft = GetTileAt(roomLayout, x - 1, y + 1, width, height);
        bool topRight = GetTileAt(roomLayout, x + 1, y + 1, width, height);
        bool bottomLeft = GetTileAt(roomLayout, x - 1, y - 1, width, height);
        bool bottomRight = GetTileAt(roomLayout, x + 1, y - 1, width, height);

        if (center)
        {
            // Check if all four direct adjacencies are filled
            // This means we are either full land or need to check for 3/4 land tiles
            if (top && right && bottom && left)
            {
                if (topLeft && topRight && bottomLeft && bottomRight)
                    return AutoTileSet.AutoTileType.FullLand;
                if (topLeft && topRight && bottomLeft && !bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterLandBottomRight;
                if (topLeft && topRight && !bottomLeft && bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterLandBottomLeft;
                if (topLeft && !topRight && bottomLeft && bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterLandTopRight;
                if (!topLeft && topRight && bottomLeft && bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterLandTopLeft;
            }

            // If we made it this far, at least one of the four direct adjacencies are empty
            // This means we have to be either flat 1/2 land tiles or 1/4 land tiles

            // Check flat for 1/2 land tiles
            if (top && right && bottom && !left)
                return AutoTileSet.AutoTileType.HalfWaterLeft;
            if (top && right && !bottom && left)
                return AutoTileSet.AutoTileType.HalfWaterBottom;
            if (top && !right && bottom && left)
                return AutoTileSet.AutoTileType.HalfWaterRight;
            if (!top && right && bottom && left)
                return AutoTileSet.AutoTileType.HalfWaterTop;

            // Check corners for 1/4 land tiles
            if (top && right && !bottom && !left)
                return AutoTileSet.AutoTileType.ThreeQuarterWaterTopRight;
            if (top && !right && !bottom && left)
                return AutoTileSet.AutoTileType.ThreeQuarterWaterTopLeft;
            if (!top && right && bottom && !left)
                return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomRight;
            if (!top && !right && bottom && left)
                return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomLeft;

            // If we made it this far, this tile type is strange and not supported. Return a flat land tile.
            return AutoTileSet.AutoTileType.FullLand;
        }
        else
        {
            // Center is water, just return water
            return AutoTileSet.AutoTileType.FullWater;
        }
    }

    /// Gets the tile value at a specific position, treating out-of-bounds as water
    private static bool GetTileAt(bool[,] roomLayout, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return false; // Out of bounds is considered water
        }
        return roomLayout[x, y];
    }


    #endregion


}
