using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Performance diagnostic helper for zone generation
/// Tracks timing and memory usage to identify bottlenecks
/// </summary>
public class GenerationDiagnostics : MonoBehaviour
{
    [Header("Monitoring Settings")]
    [SerializeField] private bool enableDiagnostics = true;
    [SerializeField] private bool logFrameTimes = true;
    [SerializeField] private bool logMemoryUsage = true;
    [SerializeField] private float frameTimeWarningThreshold = 33f; // ms (30 FPS threshold)
    
    private Stopwatch frameTimer = new Stopwatch();
    private long lastMemoryUsage = 0;
    private int frameCount = 0;
    private float maxFrameTime = 0f;
    private float totalFrameTime = 0f;

    public static GenerationDiagnostics Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Start monitoring a generation phase
    /// </summary>
    public void StartPhase(string phaseName)
    {
        if (!enableDiagnostics) return;

        frameTimer.Restart();
        frameCount = 0;
        maxFrameTime = 0f;
        totalFrameTime = 0f;
        
        if (logMemoryUsage)
        {
            lastMemoryUsage = System.GC.GetTotalMemory(false);
            Debug.Log($"<color=cyan>[Diagnostics] Starting phase: {phaseName}</color>");
            Debug.Log($"  Memory: {FormatBytes(lastMemoryUsage)}");
        }
    }

    /// <summary>
    /// Log a frame checkpoint during generation
    /// </summary>
    public void CheckFrame(string context = "")
    {
        if (!enableDiagnostics) return;

        frameTimer.Stop();
        float frameTime = (float)frameTimer.Elapsed.TotalMilliseconds;
        frameTimer.Restart();

        frameCount++;
        totalFrameTime += frameTime;
        if (frameTime > maxFrameTime)
            maxFrameTime = frameTime;

        if (logFrameTimes && frameTime > frameTimeWarningThreshold)
        {
            Debug.LogWarning($"<color=yellow>[Performance] Frame {frameCount} took {frameTime:F1}ms {context}</color>");
        }

        // Check for memory spikes
        if (logMemoryUsage)
        {
            long currentMemory = System.GC.GetTotalMemory(false);
            long memoryDelta = currentMemory - lastMemoryUsage;
            
            if (memoryDelta > 1024 * 1024 * 10) // 10 MB spike
            {
                Debug.LogWarning($"<color=orange>[Memory] Spike detected: +{FormatBytes(memoryDelta)} {context}</color>");
            }
            
            lastMemoryUsage = currentMemory;
        }
    }

    /// <summary>
    /// End monitoring and log summary
    /// </summary>
    public void EndPhase(string phaseName)
    {
        if (!enableDiagnostics) return;

        frameTimer.Stop();
        
        float avgFrameTime = frameCount > 0 ? totalFrameTime / frameCount : 0;
        
        Debug.Log($"<color=cyan>[Diagnostics] Phase complete: {phaseName}</color>");
        Debug.Log($"  Frames: {frameCount}");
        Debug.Log($"  Avg Frame Time: {avgFrameTime:F1}ms");
        Debug.Log($"  Max Frame Time: {maxFrameTime:F1}ms");
        Debug.Log($"  Total Time: {totalFrameTime:F0}ms");
        
        if (logMemoryUsage)
        {
            long finalMemory = System.GC.GetTotalMemory(false);
            Debug.Log($"  Final Memory: {FormatBytes(finalMemory)}");
        }
    }

    /// <summary>
    /// Log a specific operation timing
    /// </summary>
    public IEnumerator TimeOperation(string operationName, System.Action operation)
    {
        if (!enableDiagnostics)
        {
            operation?.Invoke();
            yield break;
        }

        Stopwatch sw = Stopwatch.StartNew();
        long memBefore = System.GC.GetTotalMemory(false);
        
        operation?.Invoke();
        
        sw.Stop();
        long memAfter = System.GC.GetTotalMemory(false);
        
        Debug.Log($"<color=green>[Operation] {operationName}: {sw.Elapsed.TotalMilliseconds:F1}ms, Memory: {FormatBytes(memAfter - memBefore)}</color>");
        
        yield return null;
    }

    /// <summary>
    /// Get current memory usage
    /// </summary>
    public string GetMemoryInfo()
    {
        long totalMemory = System.GC.GetTotalMemory(false);
        return $"Memory: {FormatBytes(totalMemory)}";
    }

    /// <summary>
    /// Format bytes to human readable string
    /// </summary>
    private string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        else
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    /// <summary>
    /// Force a detailed memory report
    /// </summary>
    public void LogDetailedMemoryReport()
    {
        Debug.Log("=== MEMORY REPORT ===");
        Debug.Log($"Total Memory: {FormatBytes(System.GC.GetTotalMemory(false))}");
        Debug.Log($"Gen 0 Collections: {System.GC.CollectionCount(0)}");
        Debug.Log($"Gen 1 Collections: {System.GC.CollectionCount(1)}");
        Debug.Log($"Gen 2 Collections: {System.GC.CollectionCount(2)}");
        
        #if UNITY_EDITOR
        Debug.Log($"Unity Total Allocated: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0):F1} MB");
        Debug.Log($"Unity Total Reserved: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024.0 * 1024.0):F1} MB");
        Debug.Log($"Unity Total Unused: {UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / (1024.0 * 1024.0):F1} MB");
        #endif
    }
}
