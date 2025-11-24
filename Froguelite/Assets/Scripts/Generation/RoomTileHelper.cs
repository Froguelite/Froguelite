using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class PerlinNoiseSettings
{
    public int octaves = 3;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float noiseScale = 0.1f;
    public float threshold = 0.4f; // Threshold to determine land vs water
    public float landScale = 1f; // Scale for land area (higher = larger land area)
    public float[] octaveOffsetsX; // Random offsets for each octave in X
    public float[] octaveOffsetsY; // Random offsets for each octave in Y
}

public static class RoomTileHelper
{
    // RoomTileHelper is a static class to help with room tiles, primarily generation and noise handling


    #region TILE GENERATION


    // Generates a room layout using Perlin noise (with octaves)
    // Returns a 2d char array of where land should be
    public static char[,] GenRoomTiles(
        int width,
        int height,
        int offsetX,
        int offsetY,
        PerlinNoiseSettings noiseSettings)
    {
        char[,] roomLayout = new char[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                roomLayout[x, y] = CoordinateMeetsTileGenThreshold(new Vector2(x + offsetX, y + offsetY), noiseSettings, width, height, offsetX, offsetY) ? 'l' : 'w'; // 'l' = land, 'w' = water
            }
        }

        return roomLayout;
    }


    // Helper function to get whether the given coordinate meets the tile generation threshold
    public static bool CoordinateMeetsTileGenThreshold(Vector2 coord, PerlinNoiseSettings noiseSettings, int roomWidth, int roomHeight, int tileOffsetX, int tileOffsetY)
    {
        // Sample Perlin noise at this position
        float finalValue = GetFullIslandGenValue(coord, noiseSettings, roomWidth, roomHeight, tileOffsetX, tileOffsetY);
        return finalValue > noiseSettings.threshold;
    }


    // Helper function to get the full island generation noise value at a coordinate
    private static float GetFullIslandGenValue(Vector2 coord, PerlinNoiseSettings noiseSettings, int roomWidth, int roomHeight, int tileOffsetX, float tileOffsetY)
    {
        float noiseValue = SamplePerlinNoise(coord.x, coord.y, noiseSettings);

        // Apply radial falloff for island shape
        float centerX = tileOffsetX + (roomWidth * 0.5f);
        float centerY = tileOffsetY + (roomHeight * 0.5f);
        float distanceFromCenter = Vector2.Distance(coord, new Vector2(centerX, centerY));
        float maxDistance = Mathf.Min(roomWidth, roomHeight) * 0.5f;

        // Apply land scaling - smaller landScale makes land area smaller
        float scaledDistance = distanceFromCenter / noiseSettings.landScale;
        float falloff = 1f - Mathf.Clamp01(scaledDistance / maxDistance);

        // Apply a smooth falloff curve
        falloff = Mathf.SmoothStep(0f, 1f, falloff);

        return noiseValue * falloff;
    }


    // Helper function to sample Perlin noise at a given coordinate using the provided settings
    public static float SamplePerlinNoise(float x, float y, PerlinNoiseSettings settings)
    {
        float noiseValue = 0f;
        float amplitude = 1f;
        float frequency = settings.noiseScale;

        // Combine multiple octaves of noise
        for (int i = 0; i < settings.octaves; i++)
        {
            float sampleX = (x + settings.octaveOffsetsX[i]) * frequency;
            float sampleY = (y + settings.octaveOffsetsY[i]) * frequency;

            float octaveValue = Mathf.PerlinNoise(sampleX, sampleY);
            noiseValue += octaveValue * amplitude;

            amplitude *= settings.persistence;
            frequency *= settings.lacunarity;
        }

        // Normalize the noise value
        return Mathf.Clamp01(noiseValue);
    }


    #endregion


    #region POST PROCESSING


    // Adds a large central landmass to the room layout (for starter rooms)
    public static char[,] AddCentralLandmass(char[,] roomLayout, int width, int height)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        int radius = 2; // This creates a 5x5 area (2 tiles in each direction from center)

        // Set the central 5x5 area to land
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                // Check bounds to prevent out-of-range errors
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    roomLayout[x, y] = 'l';
                }
            }
        }

        return roomLayout;
    }


    // Applies smoothing to given room layout
    public static char[,] SmoothRoomLayout(char[,] roomLayout, int iterations = 1)
    {
        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        for (int iter = 0; iter < iterations; iter++)
        {
            char[,] smoothedLayout = new char[width, height];

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
                                if (roomLayout[neighborX, neighborY] == 'l') // 'l' = land
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
                    smoothedLayout[x, y] = solidNeighbors > totalNeighbors / 2 ? 'l' : 'w'; // 'l' = land, 'w' = water
                }
            }

            roomLayout = smoothedLayout;
        }

        return roomLayout;
    }

    // Ensures the room layout is a single connected component
    // Removes any isolated sections of land
    public static char[,] EnsureConnectivity(char[,] roomLayout)
    {
        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        char[,] visited = new char[width, height];
        char[,] result = new char[width, height];

        // Initialize result array with water tiles by default
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                result[x, y] = 'w'; // Set all tiles to water by default
            }
        }

        // Find center point that is solid to start flood fill
        int centerX = width / 2;
        int centerY = height / 2;

        // Search for nearest solid tile to center if center is not solid
        if (roomLayout[centerX, centerY] != 'l')
        {
            float minDistance = float.MaxValue;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (roomLayout[x, y] == 'l')
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
        if (roomLayout[centerX, centerY] == 'l')
        {
            FloodFill(roomLayout, visited, result, centerX, centerY, width, height);
        }

        return result;
    }

    private static void FloodFill(char[,] original, char[,] visited, char[,] result, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] == 'l' || original[x, y] != 'l')
            return;

        visited[x, y] = 'l';
        result[x, y] = 'l';

        // Recursively fill adjacent tiles (4-directional)
        FloodFill(original, visited, result, x + 1, y, width, height);
        FloodFill(original, visited, result, x - 1, y, width, height);
        FloodFill(original, visited, result, x, y + 1, width, height);
        FloodFill(original, visited, result, x, y - 1, width, height);
    }


    // Adds arrival ('j') and path ('p') tiles to the room layout based on room data
    public static char[,] AddArrivalAndPathTiles(char[,] roomLayout, RoomData roomData)
    {
        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        // Create a copy of the layout to modify
        char[,] modifiedLayout = (char[,])roomLayout.Clone();

        // Process each door in the room data
        foreach (var doorEntry in roomData.doors)
        {
            DoorData doorData = doorEntry.Value;
            Door.DoorDirection direction = doorEntry.Key;

            // Only process doors that are not impassable (actual doors)
            if (!doorData.isImpassable)
            {
                // Get the landing position for this door direction
                Vector2Int landingPosition = GetDoorLocation(modifiedLayout, direction, false);

                // Mark the landing position and surrounding tiles as 'j' if they are land
                modifiedLayout = SetArrivalTilesAroundPosition(modifiedLayout, landingPosition, width, height);

                // Create path tiles from landing position to the edge of the room in the door's direction
                modifiedLayout = SetPathTilesToEdge(modifiedLayout, landingPosition, direction, width, height);
            }
        }

        return modifiedLayout;
    }

    // Helper method to set arrival tiles ('j') around a given position
    private static char[,] SetArrivalTilesAroundPosition(char[,] layout, Vector2Int centerPos, int width, int height)
    {
        // Check all tiles in a 5x5 area around the center position (2-tile surrounding)
        for (int offsetX = -2; offsetX <= 2; offsetX++)
        {
            for (int offsetY = -2; offsetY <= 2; offsetY++)
            {
                int tileX = centerPos.x + offsetX;
                int tileY = centerPos.y + offsetY;

                // Check if the tile is within bounds
                if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
                {
                    layout[tileX, tileY] = 'j';
                }
            }
        }

        return layout;
    }

    // Helper method to set path tiles ('p') from landing position to the edge of the room in the door's direction
    private static char[,] SetPathTilesToEdge(char[,] layout, Vector2Int landingPosition, Door.DoorDirection direction, int width, int height)
    {
        Vector2Int currentPos = landingPosition;
        Vector2Int directionVector = GetDirectionVector(direction);
        Vector2Int perpendicularVector = GetPerpendicularVector(direction);

        // Move from landing position towards the edge of the room in the door's direction
        while (true)
        {
            // Move one step in the direction
            currentPos += directionVector;

            // Check if we've reached the edge of the room
            if (currentPos.x < 0 || currentPos.x >= width || currentPos.y < 0 || currentPos.y >= height)
            {
                break;
            }

            // Set path tiles in a 3-wide pattern: center, left, and right
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int tilePos = currentPos + (perpendicularVector * offset);

                // Check if the tile position is within bounds
                if (tilePos.x >= 0 && tilePos.x < width && tilePos.y >= 0 && tilePos.y < height)
                {
                    // Only convert water tiles ('w') to path tiles ('p')
                    if (layout[tilePos.x, tilePos.y] == 'w')
                    {
                        layout[tilePos.x, tilePos.y] = 'p';
                    }
                }
            }
        }

        return layout;
    }

    // Helper method to get the direction vector for a door direction
    private static Vector2Int GetDirectionVector(Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up:
                return new Vector2Int(0, 1);
            case Door.DoorDirection.Down:
                return new Vector2Int(0, -1);
            case Door.DoorDirection.Left:
                return new Vector2Int(-1, 0);
            case Door.DoorDirection.Right:
                return new Vector2Int(1, 0);
            default:
                return Vector2Int.zero;
        }
    }

    // Helper method to get the perpendicular vector for a door direction (for creating 3-wide paths)
    private static Vector2Int GetPerpendicularVector(Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up:
            case Door.DoorDirection.Down:
                return new Vector2Int(1, 0); // Horizontal perpendicular for vertical movement
            case Door.DoorDirection.Left:
            case Door.DoorDirection.Right:
                return new Vector2Int(0, 1); // Vertical perpendicular for horizontal movement
            default:
                return Vector2Int.zero;
        }
    }


    #endregion


    #region TILEMAP HANDLING


    // Sets given tilemap to match the room layout
    // Does NOT use auto-tiling, just places land and water tiles directly (i.e. land OR water, no in-between)
    public static void SetTilemapToLayout(
        char[,] roomLayout,
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
                if (roomLayout[x, y] == 'l')
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
        char[,] roomLayout,
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
    private static AutoTileSet.AutoTileType DetermineAutoTileType(char[,] roomLayout, int x, int y, int width, int height)
    {
        // Get the 3x3 neighborhood (true = land, false = water)
        char centerChar = GetTileAt(roomLayout, x, y, width, height);
        char topChar = GetTileAt(roomLayout, x, y + 1, width, height);
        char bottomChar = GetTileAt(roomLayout, x, y - 1, width, height);
        char leftChar = GetTileAt(roomLayout, x - 1, y, width, height);
        char rightChar = GetTileAt(roomLayout, x + 1, y, width, height);
        char topLeftChar = GetTileAt(roomLayout, x - 1, y + 1, width, height);
        char topRightChar = GetTileAt(roomLayout, x + 1, y + 1, width, height);
        char bottomLeftChar = GetTileAt(roomLayout, x - 1, y - 1, width, height);
        char bottomRightChar = GetTileAt(roomLayout, x + 1, y - 1, width, height);

        bool center = centerChar == 'l' || centerChar == 'j';
        bool top = topChar == 'l' || topChar == 'j';
        bool bottom = bottomChar == 'l' || bottomChar == 'j';
        bool left = leftChar == 'l' || leftChar == 'j';
        bool right = rightChar == 'l' || rightChar == 'j';
        bool topLeft = topLeftChar == 'l' || topLeftChar == 'j';
        bool topRight = topRightChar == 'l' || topRightChar == 'j';
        bool bottomLeft = bottomLeftChar == 'l' || bottomLeftChar == 'j';
        bool bottomRight = bottomRightChar == 'l' || bottomRightChar == 'j';

        if (center)
        {
            // Special case: If only one cardinal direction has land, treat as water
            int cardinalLandCount = (top ? 1 : 0) + (right ? 1 : 0) + (bottom ? 1 : 0) + (left ? 1 : 0);
            if (cardinalLandCount == 1)
            {
                return AutoTileSet.AutoTileType.FullWater;
            }

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

                // Special case: If all cardinals are filled but exactly two corners are water
                // and those corners are on the same side, treat as half-water on that side
                int waterCornerCount = (!topLeft ? 1 : 0) + (!topRight ? 1 : 0) + (!bottomLeft ? 1 : 0) + (!bottomRight ? 1 : 0);
                if (waterCornerCount == 2)
                {
                    // Check if both water corners are on the left side
                    if (!topLeft && !bottomLeft)
                        return AutoTileSet.AutoTileType.HalfWaterLeft;
                    // Check if both water corners are on the right side
                    if (!topRight && !bottomRight)
                        return AutoTileSet.AutoTileType.HalfWaterRight;
                    // Check if both water corners are on the top
                    if (!topLeft && !topRight)
                        return AutoTileSet.AutoTileType.HalfWaterTop;
                    // Check if both water corners are on the bottom
                    if (!bottomLeft && !bottomRight)
                        return AutoTileSet.AutoTileType.HalfWaterBottom;
                }

                // Special case: If all cardinals are filled but only one corner is land, treat as 1/4 land on that corner
                if (waterCornerCount == 3)
                {
                    if (topLeft && !topRight && !bottomLeft && !bottomRight)
                        return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomRight;
                    if (!topLeft && topRight && !bottomLeft && !bottomRight)
                        return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomLeft;
                    if (!topLeft && !topRight && bottomLeft && !bottomRight)
                        return AutoTileSet.AutoTileType.ThreeQuarterWaterTopRight;
                    if (!topLeft && !topRight && !bottomLeft && bottomRight)
                        return AutoTileSet.AutoTileType.ThreeQuarterWaterTopLeft;
                }
            }

            // If we made it this far, at least one of the four direct adjacencies are empty
            // This means we have to be either flat 1/2 land tiles or 1/4 land tiles

            // Check flat for 1/2 land tiles
            // But if there's water in an enclosed corner, downgrade to 1/4 tile instead
            if (top && right && bottom && !left)
            {
                // Check enclosed corners (top-right and bottom-right)
                if (!topRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomRight; // Ignore top cardinal, use bottom-left quarter
                if (!bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterTopRight; // Ignore bottom cardinal, use top-left quarter
                return AutoTileSet.AutoTileType.HalfWaterLeft;
            }
            if (top && right && !bottom && left)
            {
                // Check enclosed corners (top-left and top-right)
                if (!topLeft)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterTopRight; // Ignore left cardinal, use bottom-right quarter
                if (!topRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterTopLeft; // Ignore right cardinal, use bottom-left quarter
                return AutoTileSet.AutoTileType.HalfWaterBottom;
            }
            if (top && !right && bottom && left)
            {
                // Check enclosed corners (top-left and bottom-left)
                if (!topLeft)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomLeft; // Ignore top cardinal, use bottom-right quarter
                if (!bottomLeft)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterTopLeft; // Ignore bottom cardinal, use top-right quarter
                return AutoTileSet.AutoTileType.HalfWaterRight;
            }
            if (!top && right && bottom && left)
            {
                // Check enclosed corners (bottom-left and bottom-right)
                if (!bottomLeft)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomRight; // Ignore left cardinal, use top-right quarter
                if (!bottomRight)
                    return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomLeft; // Ignore right cardinal, use top-left quarter
                return AutoTileSet.AutoTileType.HalfWaterTop;
            }

            // Check corners for 1/4 land tiles (2 adjacent cardinals filled)
            // But if the enclosed corner is water, default to water instead
            if (top && right && !bottom && !left)
            {
                // Check if the enclosed corner (top-right) is water
                if (!topRight)
                    return AutoTileSet.AutoTileType.FullWater;
                return AutoTileSet.AutoTileType.ThreeQuarterWaterTopRight;
            }
            if (top && !right && !bottom && left)
            {
                // Check if the enclosed corner (top-left) is water
                if (!topLeft)
                    return AutoTileSet.AutoTileType.FullWater;
                return AutoTileSet.AutoTileType.ThreeQuarterWaterTopLeft;
            }
            if (!top && right && bottom && !left)
            {
                // Check if the enclosed corner (bottom-right) is water
                if (!bottomRight)
                    return AutoTileSet.AutoTileType.FullWater;
                return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomRight;
            }
            if (!top && !right && bottom && left)
            {
                // Check if the enclosed corner (bottom-left) is water
                if (!bottomLeft)
                    return AutoTileSet.AutoTileType.FullWater;
                return AutoTileSet.AutoTileType.ThreeQuarterWaterBottomLeft;
            }

            // Check diagonal pairs (opposite cardinals filled)
            if (top && !right && bottom && !left)
                return AutoTileSet.AutoTileType.HalfWaterRight; // Vertical water strip on sides
            if (!top && right && !bottom && left)
                return AutoTileSet.AutoTileType.HalfWaterTop; // Horizontal water strip on top/bottom

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
    private static char GetTileAt(char[,] roomLayout, int x, int y, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return 'w'; // Out of bounds is considered water
        }
        return roomLayout[x, y];
    }


    #endregion


    #region DOOR LOCATIONS


    // Gets the door location in a given direction for a room layout
    // If launchLocation = true, returns the location where the bubble hovers, ready for launch
    // If launchLocation = false, returns the location where the player should end up inside the room (i.e. "landing" location)
    public static Vector2Int GetDoorLocation(char[,] roomLayout, Door.DoorDirection doorDirection, bool launchLocation = true)
    {
        if (roomLayout == null)
        {
            Debug.LogError("Room layout is null!");
            return Vector2Int.zero;
        }

        int width = roomLayout.GetLength(0);
        int height = roomLayout.GetLength(1);

        int centerX = width / 2;
        int centerY = height / 2;

        switch (doorDirection)
        {
            case Door.DoorDirection.Up:
                // Start at highest y, centered on x, and go down until we find land
                for (int y = height - 1; y >= 0; y--)
                {
                    if (roomLayout[centerX, y] == 'l' || roomLayout[centerX, y] == 'j')
                    {
                        // Go a bit further or less depending on launch state, so we are in the water or on land
                        return new Vector2Int(centerX, y + (launchLocation ? 2 : -2));
                    }
                }
                break;

            case Door.DoorDirection.Down:
                // Start at lowest y, centered on x, and go up until we find land
                for (int y = 0; y < height; y++)
                {
                    if (roomLayout[centerX, y] == 'l' || roomLayout[centerX, y] == 'j')
                    {
                        // Go a bit further or less depending on launch state, so we are in the water or on land
                        return new Vector2Int(centerX, y + (launchLocation ? -2 : 2));
                    }
                }
                break;

            case Door.DoorDirection.Left:
                // Start leftmost x, centered on y, and go right until we find land
                for (int x = 0; x < width; x++)
                {
                    if (roomLayout[x, centerY] == 'l' || roomLayout[x, centerY] == 'j')
                    {
                        // Go a bit further or less depending on launch state, so we are in the water or on land
                        return new Vector2Int(x + (launchLocation ? -2 : 2), centerY);
                    }
                }
                break;

            case Door.DoorDirection.Right:
                // Start rightmost x, centered on y, and go left until we find land
                for (int x = width - 1; x >= 0; x--)
                {
                    if (roomLayout[x, centerY] == 'l' || roomLayout[x, centerY] == 'j')
                    {
                        // Go a bit further or less depending on launch state, so we are in the water or on land
                        return new Vector2Int(x + (launchLocation ? 2 : -2), centerY);
                    }
                }
                break;
        }

        // If no suitable position found, return center of layout as fallback
        Debug.LogWarning($"No suitable door location found for direction {doorDirection}. Returning center as fallback.");
        return new Vector2Int(width / 2, height / 2);
    }


    #endregion


}
