using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Helper class for offloading heavy computations to background threads
/// </summary>
public static class ThreadedGenerationHelper
{
    /// <summary>
    /// Generates Perlin noise tile layout on a background thread
    /// </summary>
    public static Task<char[,]> GenerateRoomTilesAsync(
        int width,
        int height,
        int offsetX,
        int offsetY,
        PerlinNoiseSettings noiseSettings)
    {
        return Task.Run(() =>
        {
            char[,] layout = new char[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float worldX = offsetX + x;
                    float worldY = offsetY + y;

                    float noiseValue = 0f;
                    float amplitude = 1f;
                    float frequency = 1f;
                    float maxValue = 0f;

                    for (int octave = 0; octave < noiseSettings.octaves; octave++)
                    {
                        float sampleX = (worldX + noiseSettings.octaveOffsetsX[octave]) * noiseSettings.noiseScale * frequency;
                        float sampleY = (worldY + noiseSettings.octaveOffsetsY[octave]) * noiseSettings.noiseScale * frequency;

                        // Using Mathf.PerlinNoise is thread-safe
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                        noiseValue += perlinValue * amplitude;

                        maxValue += amplitude;
                        amplitude *= noiseSettings.persistence;
                        frequency *= noiseSettings.lacunarity;
                    }

                    noiseValue /= maxValue;
                    noiseValue = (noiseValue + 1f) / 2f;

                    float scaledNoise = noiseValue * noiseSettings.landScale;
                    layout[x, y] = scaledNoise > noiseSettings.threshold ? 'g' : 'w';
                }
            }

            return layout;
        });
    }

    /// <summary>
    /// Smooths room layout on a background thread
    /// </summary>
    public static Task<char[,]> SmoothRoomLayoutAsync(char[,] roomLayout, int iterations = 1)
    {
        return Task.Run(() =>
        {
            int width = roomLayout.GetLength(0);
            int height = roomLayout.GetLength(1);
            char[,] smoothedLayout = (char[,])roomLayout.Clone();

            for (int iter = 0; iter < iterations; iter++)
            {
                char[,] tempLayout = (char[,])smoothedLayout.Clone();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int landNeighbors = CountLandNeighbors(smoothedLayout, x, y);

                        if (landNeighbors > 4)
                            tempLayout[x, y] = 'g';
                        else if (landNeighbors < 4)
                            tempLayout[x, y] = 'w';
                    }
                }

                smoothedLayout = tempLayout;
            }

            return smoothedLayout;
        });
    }

    /// <summary>
    /// Generates Poisson disc samples on a background thread
    /// </summary>
    public static Task<List<Vector2>> GeneratePoissonDiscSamplesAsync(
        Vector2 regionStart,
        float regionSize,
        float minDistance,
        int maxAttempts,
        int seed)
    {
        return Task.Run(() =>
        {
            List<Vector2> points = new List<Vector2>();
            List<Vector2> activePoints = new List<Vector2>();
            
            // Use thread-safe random with seed
            System.Random random = new System.Random(seed);

            // Cell size for spatial hashing
            float cellSize = minDistance / Mathf.Sqrt(2f);
            int gridWidth = Mathf.CeilToInt(regionSize / cellSize);
            int gridHeight = Mathf.CeilToInt(regionSize / cellSize);
            int[,] grid = new int[gridWidth, gridHeight];

            // Initialize grid with -1 (empty)
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    grid[x, y] = -1;
                }
            }

            // Add initial point
            Vector2 initialPoint = new Vector2(
                regionStart.x + (float)random.NextDouble() * regionSize,
                regionStart.y + (float)random.NextDouble() * regionSize
            );
            
            points.Add(initialPoint);
            activePoints.Add(initialPoint);
            
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt((initialPoint.x - regionStart.x) / cellSize),
                Mathf.FloorToInt((initialPoint.y - regionStart.y) / cellSize)
            );
            
            if (gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight)
            {
                grid[gridPos.x, gridPos.y] = 0;
            }

            // Generate points
            while (activePoints.Count > 0)
            {
                int randomIndex = random.Next(0, activePoints.Count);
                Vector2 currentPoint = activePoints[randomIndex];
                bool foundPoint = false;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = minDistance * (1f + (float)random.NextDouble());
                    
                    Vector2 newPoint = currentPoint + new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    );

                    // Check if point is in region
                    if (newPoint.x >= regionStart.x && newPoint.x < regionStart.x + regionSize &&
                        newPoint.y >= regionStart.y && newPoint.y < regionStart.y + regionSize)
                    {
                        Vector2Int newGridPos = new Vector2Int(
                            Mathf.FloorToInt((newPoint.x - regionStart.x) / cellSize),
                            Mathf.FloorToInt((newPoint.y - regionStart.y) / cellSize)
                        );

                        if (IsValidPoissonPoint(newPoint, points, minDistance, grid, gridWidth, gridHeight, 
                            cellSize, regionStart, newGridPos))
                        {
                            points.Add(newPoint);
                            activePoints.Add(newPoint);
                            
                            if (newGridPos.x >= 0 && newGridPos.x < gridWidth && 
                                newGridPos.y >= 0 && newGridPos.y < gridHeight)
                            {
                                grid[newGridPos.x, newGridPos.y] = points.Count - 1;
                            }
                            
                            foundPoint = true;
                            break;
                        }
                    }
                }

                if (!foundPoint)
                {
                    activePoints.RemoveAt(randomIndex);
                }
            }

            return points;
        });
    }

    #region HELPER METHODS

    private static int CountLandNeighbors(char[,] layout, int x, int y)
    {
        int count = 0;
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);

        for (int nx = x - 1; nx <= x + 1; nx++)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (nx == x && ny == y) continue;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (layout[nx, ny] == 'g' || layout[nx, ny] == 'j' || layout[nx, ny] == 'p')
                        count++;
                }
            }
        }

        return count;
    }

    private static bool IsValidPoissonPoint(
        Vector2 point,
        List<Vector2> existingPoints,
        float minDistance,
        int[,] grid,
        int gridWidth,
        int gridHeight,
        float cellSize,
        Vector2 regionStart,
        Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            return false;

        // Check neighboring cells
        int searchRadius = 2;
        for (int x = Mathf.Max(0, gridPos.x - searchRadius); 
             x <= Mathf.Min(gridWidth - 1, gridPos.x + searchRadius); x++)
        {
            for (int y = Mathf.Max(0, gridPos.y - searchRadius); 
                 y <= Mathf.Min(gridHeight - 1, gridPos.y + searchRadius); y++)
            {
                int pointIndex = grid[x, y];
                if (pointIndex != -1)
                {
                    float dist = Vector2.Distance(point, existingPoints[pointIndex]);
                    if (dist < minDistance)
                        return false;
                }
            }
        }

        return true;
    }

    #endregion
}
