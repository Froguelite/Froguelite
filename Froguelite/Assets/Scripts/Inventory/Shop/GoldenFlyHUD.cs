using TMPro;
using UnityEngine;

public class GoldenFlyHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform displayTransform; // The UI parent that will move on/off screen
    [SerializeField] private TMP_Text countText; // The text showing the number of golden flies

    [Header("Animation Settings")]
    private Vector3 onScreenPosition = Vector3.zero; // Position when visible
    private Vector3 offScreenPosition = new Vector3(300f, 0f, 0f); // Position when hidden (off to the right)
    [SerializeField] private float slideInDuration = 0.5f; // Duration of slide-in animation
    [SerializeField] private float slideOutDuration = 0.4f; // Duration of slide-out animation
    [SerializeField] private float displayDuration = 3f; // How long to show before hiding
    private LeanTweenType slideInEase = LeanTweenType.easeOutQuad; // Easing for slide in
    private LeanTweenType slideOutEase = LeanTweenType.easeInQuad; // Easing for slide out
    [SerializeField] private float scaleBounceFactor = 1.5f; // How much to scale when a fly is collected/spent
    [SerializeField] private float scaleBounceDuration = 0.3f; // Duration of the scale bounce animation

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 10f; // How far the display shakes
    [SerializeField] private float shakeDuration = 0.5f; // How long the shake lasts

    private bool isShowing = false; // Track if currently showing
    private int hideAnimationId = -1; // Track the delayed hide animation
    private bool forceShow = false; // When true, keeps the display visible and ignores hide requests
    private Vector3 originalScale; // Store the original scale to return to

    public int goldenFlies { get; private set; } = 0;

    public static GoldenFlyHUD Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        offScreenPosition = displayTransform.localPosition;
        onScreenPosition = offScreenPosition + new Vector3(300f, 0f, 0f);

        // Store original scale
        if (displayTransform != null)
        {
            originalScale = displayTransform.localScale;
        }
    }

    void Start()
    {
        LoadGoldenFlies();
    }

    void OnEnable()
    {
        // Subscribe to save/load events
        SaveManager.SaveData += SaveGoldenFlies;
        SaveManager.LoadData += LoadGoldenFlies;

        // Start off screen
        if (displayTransform != null)
        {
            displayTransform.localPosition = offScreenPosition;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from save/load events
        SaveManager.SaveData -= SaveGoldenFlies;
        SaveManager.LoadData -= LoadGoldenFlies;

        // Cancel any pending animations
        CancelHideAnimation();
        if (displayTransform != null)
        {
            LeanTween.cancel(displayTransform.gameObject);
        }
    }

    void UpdateDisplay(int count)
    {
        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }

    void AnimateScaleBounce()
    {
        if (displayTransform == null) return;

        // Cancel any existing scale animations on this object
        LeanTween.cancel(displayTransform.gameObject, false);

        // Scale up to the bounce factor
        LeanTween.scale(displayTransform.gameObject, originalScale * scaleBounceFactor, scaleBounceDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                // Scale back down to original size
                LeanTween.scale(displayTransform.gameObject, originalScale, scaleBounceDuration * 0.6f)
                    .setEase(LeanTweenType.easeOutQuad);
            });
    }

    void ShowDisplay()
    {
        if (displayTransform == null) return;

        // Cancel any existing hide animation
        CancelHideAnimation();

        // If already showing, just reset the timer without restarting animation
        if (isShowing)
        {
            // Schedule hide again with new delay
            ScheduleHide();
            return;
        }

        // Cancel any ongoing animations on this transform
        LeanTween.cancel(displayTransform.gameObject);

        // Slide in from off screen
        isShowing = true;
        LeanTween.moveLocal(displayTransform.gameObject, onScreenPosition, slideInDuration)
            .setEase(slideInEase)
            .setOnComplete(() =>
            {
                // Schedule the hide animation after display duration
                ScheduleHide();
            });
    }

    void ScheduleHide()
    {
        // Don't schedule hide if in force show mode
        if (forceShow) return;

        // Cancel previous hide schedule if exists
        CancelHideAnimation();

        // Schedule hide after display duration
        hideAnimationId = LeanTween.delayedCall(displayDuration, () =>
        {
            HideDisplay();
        }).id;
    }

    void HideDisplay()
    {
        // Don't hide if in force show mode
        if (forceShow) return;

        if (displayTransform == null) return;

        // Cancel any ongoing animations on this transform
        LeanTween.cancel(displayTransform.gameObject);

        // Slide out off screen
        isShowing = false;
        hideAnimationId = -1;
        LeanTween.moveLocal(displayTransform.gameObject, offScreenPosition, slideOutDuration)
            .setEase(slideOutEase);
    }

    void CancelHideAnimation()
    {
        if (hideAnimationId != -1)
        {
            LeanTween.cancel(hideAnimationId);
            hideAnimationId = -1;
        }
    }

    /// <summary>
    /// Forces the display to stay visible and prevents automatic hiding.
    /// Use this when entering a scene where the golden fly count should always be shown.
    /// </summary>
    public void ForceShow()
    {
        forceShow = true;
        CancelHideAnimation();
        
        if (displayTransform == null) return;

        // Cancel any ongoing animations
        LeanTween.cancel(displayTransform.gameObject);

        // If not already showing, animate in
        if (!isShowing)
        {
            isShowing = true;
            LeanTween.moveLocal(displayTransform.gameObject, onScreenPosition, slideInDuration)
                .setEase(slideInEase);
        }
        else
        {
            // Already showing, just make sure it's at the right position
            displayTransform.localPosition = onScreenPosition;
        }
    }

    /// <summary>
    /// Releases the force show mode and hides the display.
    /// Call this when leaving a scene where the golden fly count was being force shown.
    /// </summary>
    public void ReleaseForceShow()
    {
        forceShow = false;
        HideDisplay();
    }

    /// <summary>
    /// Shakes the display transform to indicate insufficient funds or an error.
    /// </summary>
    public void ShakeDisplay()
    {
        if (displayTransform == null) return;

        // Make sure display is visible before shaking
        if (!isShowing)
        {
            ShowDisplay();
        }

        // Cancel any existing position animations (but not scale animations)
        LeanTween.cancel(displayTransform.gameObject, false);

        // Store the current target position (on screen or off screen)
        Vector3 targetPosition = isShowing ? onScreenPosition : offScreenPosition;
        
        // Create a shake effect using a series of quick movements
        LTSeq shakeSequence = LeanTween.sequence();
        
        // Number of shakes
        int shakeCount = 4;
        float individualShakeDuration = shakeDuration / (shakeCount * 2);
        
        for (int i = 0; i < shakeCount; i++)
        {
            // Shake to the right
            shakeSequence.append(() =>
            {
                LeanTween.moveLocal(displayTransform.gameObject, targetPosition + new Vector3(shakeIntensity, 0, 0), individualShakeDuration)
                    .setEase(LeanTweenType.easeInOutQuad);
            });
            shakeSequence.append(individualShakeDuration);
            
            // Shake to the left
            shakeSequence.append(() =>
            {
                LeanTween.moveLocal(displayTransform.gameObject, targetPosition + new Vector3(-shakeIntensity, 0, 0), individualShakeDuration)
                    .setEase(LeanTweenType.easeInOutQuad);
            });
            shakeSequence.append(individualShakeDuration);
            
            // Reduce intensity for next shake (dampening effect)
            shakeIntensity *= 0.7f;
        }
        
        // Return to target position
        shakeSequence.append(() =>
        {
            LeanTween.moveLocal(displayTransform.gameObject, targetPosition, individualShakeDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    // Reset shake intensity to original value
                    shakeIntensity = 10f;
                    
                    // If we're showing and not in force show mode, reschedule hide
                    if (isShowing && !forceShow)
                    {
                        ScheduleHide();
                    }
                });
        });
    }

    #region GOLDEN FLIES MANAGEMENT

    /// <summary>
    /// Adds a number of golden flies to the player's inventory.
    /// </summary>
    public void AddGoldenFlies(int amount)
    {
        goldenFlies += amount;
        if (goldenFlies < 0) goldenFlies = 0;
        
        UpdateDisplay(goldenFlies);
        AnimateScaleBounce();
        
        if (!forceShow)
        {
            ShowDisplay();
        }

        // Immediately save the new golden fly count
        SaveGoldenFlies();
    }

    /// <summary>
    /// Removes a number of golden flies from the player's inventory.
    /// </summary>
    public void RemoveGoldenFlies(int amount)
    {
        AddGoldenFlies(-amount);
    }

    /// <summary>
    /// Save golden flies count to SaveManager
    /// </summary>
    private void SaveGoldenFlies()
    {
        SaveManager.SaveForProfile(SaveVariable.GoldenFlies, goldenFlies);
    }

    /// <summary>
    /// Load golden flies count from SaveManager
    /// </summary>
    private void LoadGoldenFlies()
    {
        try
        {
            goldenFlies = SaveManager.LoadForProfile<int>(SaveVariable.GoldenFlies);
            UpdateDisplay(goldenFlies);
            Debug.Log($"[GoldenFlyHUD] Loaded {goldenFlies} golden flies from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (0)
            goldenFlies = 0;
            UpdateDisplay(goldenFlies);
            Debug.Log($"[GoldenFlyHUD] No saved golden flies found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[GoldenFlyHUD] Failed to load golden flies: {ex.Message}");
            goldenFlies = 0;
            UpdateDisplay(goldenFlies);
        }
    }

    #endregion
}
