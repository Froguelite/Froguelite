using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Example loading screen that displays generation progress
/// Attach this to a Canvas with a loading UI
/// </summary>
public class ZoneLoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private int subZoneToGenerate = 0;
    [SerializeField] private bool showLoadingOnStart = true;

    private void Start()
    {
        if (showLoadingOnStart)
        {
            StartCoroutine(LoadZoneWithUI());
        }
    }

    /// <summary>
    /// Main loading coroutine that shows UI and tracks progress
    /// </summary>
    public IEnumerator LoadZoneWithUI()
    {
        // Show loading screen
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        UpdateStatus("Initializing zone generation...");
        UpdateProgress(0f);

        // Subscribe to progress events
        if (ZoneGeneratorAsync.Instance != null)
        {
            ZoneGeneratorAsync.Instance.OnGenerationProgress += OnProgressUpdate;
        }
        else
        {
            Debug.LogError("ZoneGeneratorAsync.Instance is null!");
            yield break;
        }

        // Start generation
        yield return StartCoroutine(ZoneGeneratorAsync.Instance.GenerateZoneAsync(subZoneToGenerate, OnGenerationComplete));

        // Unsubscribe from events
        if (ZoneGeneratorAsync.Instance != null)
        {
            ZoneGeneratorAsync.Instance.OnGenerationProgress -= OnProgressUpdate;
        }
    }

    /// <summary>
    /// Called when generation progress updates
    /// </summary>
    private void OnProgressUpdate(float progress)
    {
        UpdateProgress(progress);

        // Update status text based on progress
        if (progress < 0.1f)
            UpdateStatus("Generating room graph...");
        else if (progress < 0.7f)
            UpdateStatus("Creating rooms and foliage...");
        else if (progress < 0.8f)
            UpdateStatus("Initializing minimap...");
        else if (progress < 0.9f)
            UpdateStatus("Setting up player...");
        else if (progress < 1.0f)
            UpdateStatus("Building navigation mesh...");
        else
            UpdateStatus("Complete!");
    }

    /// <summary>
    /// Called when generation is complete
    /// </summary>
    private void OnGenerationComplete()
    {
        UpdateProgress(1f);
        UpdateStatus("Zone ready!");

        // Hide loading screen after a short delay
        StartCoroutine(HideLoadingScreen());
    }

    /// <summary>
    /// Hides the loading screen with a fade effect
    /// </summary>
    private IEnumerator HideLoadingScreen()
    {
        yield return new WaitForSeconds(0.5f);

        if (loadingPanel != null)
        {
            // Optional: Add fade out animation here
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the progress bar
    /// </summary>
    private void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    /// <summary>
    /// Updates the status text
    /// </summary>
    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }

        Debug.Log($"[Zone Loading] {status}");
    }

    /// <summary>
    /// Public method to trigger zone loading from other scripts
    /// </summary>
    public void LoadZone(int subZone)
    {
        subZoneToGenerate = subZone;
        StartCoroutine(LoadZoneWithUI());
    }
}
