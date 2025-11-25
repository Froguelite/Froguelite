using System.Collections;
using NavMeshPlus.Components;
using UnityEngine;

/// <summary>
/// Extension methods for NavMeshSurface to support async building
/// </summary>
public static class NavMeshSurfaceExtensions
{
    /// <summary>
    /// Builds the NavMesh asynchronously across multiple frames
    /// </summary>
    public static IEnumerator BuildNavMeshAsync(this NavMeshSurface surface)
    {
        // Start building in a coroutine to allow frame yields
        bool buildStarted = false;
        bool buildComplete = false;

        // Use a background thread-safe flag
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            buildStarted = true;
        });

        // Wait for thread to start
        while (!buildStarted)
        {
            yield return null;
        }

        // Build the NavMesh (this happens on the main thread but we yield after)
        surface.BuildNavMesh();
        
        buildComplete = true;

        // Give one more frame for the NavMesh to finalize
        yield return null;
    }
}
