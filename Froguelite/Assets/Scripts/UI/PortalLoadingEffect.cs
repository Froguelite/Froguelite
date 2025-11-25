using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PortalLoadingEffect : MonoBehaviour
{
    // PortalLoadingEffect handles the visual effect when loading through a portal

    #region VARIABLES

    [Header("Ring Settings")]
    [SerializeField] private Image ringPrefab; // The ring image prefab to spawn
    [SerializeField] private Transform ringContainer; // Parent container for spawned rings

    [Header("Animation Settings")]
    [SerializeField] private float spawnInterval = 0.3f; // Time between spawning new rings
    [SerializeField] private float growthSpeed = 8f; // How fast rings grow
    [SerializeField] private float maxScale = 5f; // Maximum scale before destroying ring
    [SerializeField] private float initialScale = 0.1f; // Starting scale of rings

    [Header("Visual Settings")]
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f); // Fade to transparent
    [SerializeField] private bool rotateRings = true;
    [SerializeField] private float rotationSpeed = 30f; // Degrees per second
    [SerializeField] private CanvasGroup backgroundBlockerCanvGroup;

    [Header("Player Animation")]
    [SerializeField] private RectTransform playerUiElement; // UI element representing the player
    [SerializeField] private float playerSpinAccelTime = 1f; // Time to reach max spin speed
    [SerializeField] private float playerSpinDecelTime = 1f; // Time to decelerate to stop
    [SerializeField] private float playerMaxSpinSpeed = 720f; // Max rotation speed in degrees per second
    [SerializeField] private float playerMaxScale = 5f; // Maximum scale during spin

    private List<RingData> activeRings = new List<RingData>();
    private Coroutine effectCoroutine;
    private Coroutine playerSpinCoroutine;
    private bool isEffectActive = false;
    private float currentPlayerSpinSpeed = 0f;
    private bool isDecelerating = false;
    private Vector3 originalPlayerScale = Vector3.one;

    private class RingData
    {
        public Image ringImage;
        public float currentScale;
        public float startTime;

        public RingData(Image image, float scale, float time)
        {
            ringImage = image;
            currentScale = scale;
            startTime = time;
        }
    }

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        // If no container is assigned, use this object as container
        if (ringContainer == null)
        {
            ringContainer = transform;
        }
    }

    private void Update()
    {
        if (isEffectActive || activeRings.Count > 0)
        {
            UpdateRings();
        }

        // Update player spin rotation (only during acceleration and constant speed, not deceleration)
        if (!isDecelerating && playerUiElement != null && playerUiElement.gameObject.activeSelf && currentPlayerSpinSpeed > 0f)
        {
            playerUiElement.Rotate(Vector3.forward, currentPlayerSpinSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region START / STOP

    public void StartEffect()
    {
        if (isEffectActive) return;

        PlayerMovement.Instance.SetCanMove(false);
        PlayerAttack.Instance.SetCanAttack(false);

        StartCoroutine(StartEffectCo());
    }

    private IEnumerator StartEffectCo() 
    {
        LeanTween.cancel(backgroundBlockerCanvGroup.gameObject);

        if (ringPrefab == null)
        {
            Debug.LogError("PortalLoadingEffect: Ring prefab is not assigned!");
            yield break;
        }

        isEffectActive = true;

        yield return new WaitForSeconds(1f);

        effectCoroutine = StartCoroutine(SpawnRingsCoroutine());

        // Hide the player sprite and replace with UI element
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.SetSpriteHidden(true);
            PositionPlayerUiElement();

            // Start the spinning animation
            if (playerSpinCoroutine != null)
            {
                StopCoroutine(playerSpinCoroutine);
            }
            playerSpinCoroutine = StartCoroutine(PlayerSpinCoroutine());
        }
        
        yield return new WaitForSeconds(0.5f);
        backgroundBlockerCanvGroup.LeanAlpha(1f, 0.5f);
    }

    public void StopEffect()
    {
        if (!isEffectActive) return;

        isEffectActive = false;

        LeanTween.cancel(backgroundBlockerCanvGroup.gameObject);
        backgroundBlockerCanvGroup.LeanAlpha(0f, 0.5f);

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        // Start decelerating the player spin
        if (playerSpinCoroutine != null)
        {
            StopCoroutine(playerSpinCoroutine);
        }
        playerSpinCoroutine = StartCoroutine(DeceleratePlayerSpinCoroutine());

        StartCoroutine(WaitThenEnablePlayerControl());

        // Don't clear rings immediately - let them finish their animation
        // ClearAllRings();
    }

    // Coroutine to accelerate player spinning to max speed
    private IEnumerator PlayerSpinCoroutine()
    {
        currentPlayerSpinSpeed = 0f;
        float elapsedTime = 0f;

        // Store original scale
        originalPlayerScale = playerUiElement.localScale;

        // Accelerate from 0 to max speed and grow in size
        while (elapsedTime < playerSpinAccelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / playerSpinAccelTime;
            currentPlayerSpinSpeed = Mathf.Lerp(0f, playerMaxSpinSpeed, t);

            // Scale up from original to max scale
            playerUiElement.localScale = Vector3.Lerp(originalPlayerScale, originalPlayerScale * playerMaxScale, t);

            yield return null;
        }

        currentPlayerSpinSpeed = playerMaxSpinSpeed;
        playerUiElement.localScale = originalPlayerScale * playerMaxScale;

        // Keep spinning at max speed and max scale until effect stops
        while (isEffectActive)
        {
            yield return null;
        }
    }
    
    private IEnumerator WaitThenEnablePlayerControl()
    {
        yield return new WaitForSeconds(1f);

        PlayerMovement.Instance.SetCanMove(true);
        PlayerAttack.Instance.SetCanAttack(true);

        InputManager.Instance.PushAnyPendingMovement();
    }

    // Coroutine to decelerate player spinning to a stop
    private IEnumerator DeceleratePlayerSpinCoroutine()
    {
        if (playerUiElement == null)
        {
            yield break;
        }

        isDecelerating = true;

        float startSpeed = currentPlayerSpinSpeed;
        float elapsedTime = 0f;

        // Get current rotation (0-360 range)
        float currentRotation = playerUiElement.localEulerAngles.z;
        float startRotation = currentRotation;

        // Calculate target rotation - always move forward to the next 0/360 degree mark
        // If we're at 350 degrees, we go forward to 360 (which equals 0)
        // If we're at 10 degrees, we go forward to 360 (completing the rotation)
        float targetRotation = Mathf.Ceil(currentRotation / 360f) * 360f;
        
        // If we're already at 0, go to 360 (complete one more full rotation)
        if (Mathf.Approximately(currentRotation, 0f))
        {
            targetRotation = 360f;
        }

        // Decelerate from current speed to 0 while rotating to vertical
        while (elapsedTime < playerSpinDecelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / playerSpinDecelTime;
            
            // Smoothly decelerate speed
            currentPlayerSpinSpeed = Mathf.Lerp(startSpeed, 0f, t);
            
            // Smoothly rotate to vertical position (always moving forward)
            float newRotation = Mathf.Lerp(startRotation, targetRotation, t);
            playerUiElement.localEulerAngles = new Vector3(0, 0, newRotation);
            
            // Scale back down from max scale to original scale
            playerUiElement.localScale = Vector3.Lerp(originalPlayerScale * playerMaxScale, originalPlayerScale, t);
            
            yield return null;
        }

        currentPlayerSpinSpeed = 0f;
        playerUiElement.localEulerAngles = new Vector3(0, 0, 0); // Ensure it's exactly vertical
        playerUiElement.localScale = originalPlayerScale; // Ensure it's back to original scale
        isDecelerating = false;

        // After deceleration is complete, show the player sprite again and hide the UI element
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.SetSpriteHidden(false);
        }

        if (playerUiElement != null)
        {
            playerUiElement.gameObject.SetActive(false);
        }
    }

    #endregion

    #region PLAYER UI ELEMENT

    // Positions the player UI element at the player's sprite position
    private void PositionPlayerUiElement()
    {
        if (playerUiElement == null || PlayerMovement.Instance == null)
        {
            Debug.LogWarning("PortalLoadingEffect: playerUiElement or PlayerMovement.Instance is null!");
            return;
        }

        SpriteRenderer playerSprite = PlayerMovement.Instance.playerSpriteRenderer;

        // Get the player's sprite renderer world position
        Vector3 playerWorldPos = playerSprite.transform.position;

        // Convert world position to screen position
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PortalLoadingEffect: Main camera not found!");
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerWorldPos);

        // Convert screen position to local position in the canvas
        RectTransform canvasRect = MainCanvas.Instance.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("PortalLoadingEffect: MainCanvas RectTransform not found!");
            return;
        }

        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos))
        {
            playerUiElement.anchoredPosition = localPos;
        }

        // Calculate the sprite's size in world units
        if (playerSprite.sprite != null)
        {
            // Get the sprite's bounds in world space
            Bounds spriteBounds = playerSprite.bounds;
            Vector3 spriteSize = spriteBounds.size;

            // Account for the sprite's scale
            spriteSize.x *= playerSprite.transform.lossyScale.x;
            spriteSize.y *= playerSprite.transform.lossyScale.y;

            // Convert world size to screen size
            // We'll measure the distance between the center and the right edge in screen space
            Vector3 centerScreen = mainCamera.WorldToScreenPoint(playerWorldPos);
            Vector3 rightEdgeWorld = playerWorldPos + new Vector3(spriteSize.x / 2f, 0, 0);
            Vector3 topEdgeWorld = playerWorldPos + new Vector3(0, spriteSize.y / 2f, 0);
            
            Vector3 rightEdgeScreen = mainCamera.WorldToScreenPoint(rightEdgeWorld);
            Vector3 topEdgeScreen = mainCamera.WorldToScreenPoint(topEdgeWorld);

            // Calculate screen space size
            float screenWidth = Mathf.Abs(rightEdgeScreen.x - centerScreen.x) * 2f;
            float screenHeight = Mathf.Abs(topEdgeScreen.y - centerScreen.y) * 2f;

            // Account for parent canvas scale to prevent the UI element from being scaled incorrectly
            // If the parent has a scale of (2, 2, 2), we need to divide by 2 to compensate
            Vector3 parentScale = playerUiElement.parent.lossyScale;
            screenWidth /= parentScale.x;
            screenHeight /= parentScale.y;

            // Convert screen size to canvas size
            // For a ScreenSpace-Overlay canvas, screen pixels = canvas units
            // But we need to account for canvas scaler if present
            Canvas canvas = canvasRect.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                playerUiElement.sizeDelta = new Vector2(screenWidth, screenHeight);
            }
            else
            {
                // For other render modes, we might need different calculations
                playerUiElement.sizeDelta = new Vector2(screenWidth, screenHeight);
            }

            // Offset the position upward so the bottom of the image is centered at the player position
            // Move it up by half the height
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos))
            {
                localPos.y += screenHeight / 2f;
                playerUiElement.anchoredPosition = localPos;
            }
        }

        // Enable the player UI element
        playerUiElement.gameObject.SetActive(true);
    }

    #endregion

    #region RING MANAGEMENT

    private IEnumerator SpawnRingsCoroutine()
    {
        while (isEffectActive)
        {
            SpawnRing();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnRing()
    {
        // Instantiate new ring at center
        Image newRing = Instantiate(ringPrefab, ringContainer);

        // Set initial properties
        RectTransform rectTransform = newRing.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one * initialScale;
        rectTransform.localPosition = Vector3.zero;

        // Set initial color
        newRing.color = startColor;

        // Add to active rings list
        RingData ringData = new RingData(newRing, initialScale, Time.time);
        activeRings.Add(ringData);
    }

    private void UpdateRings()
    {
        List<RingData> ringsToRemove = new List<RingData>();

        foreach (RingData ringData in activeRings)
        {
            if (ringData.ringImage == null)
            {
                ringsToRemove.Add(ringData);
                continue;
            }

            // Grow the ring
            ringData.currentScale += growthSpeed * Time.deltaTime;
            ringData.ringImage.rectTransform.localScale = Vector3.one * ringData.currentScale;

            // Calculate fade progress (0 to 1)
            float progress = Mathf.InverseLerp(initialScale, maxScale, ringData.currentScale);

            // Fade color
            ringData.ringImage.color = Color.Lerp(startColor, endColor, progress);

            // Rotate if enabled
            if (rotateRings)
            {
                float rotation = ringData.ringImage.rectTransform.localEulerAngles.z;
                rotation += rotationSpeed * Time.deltaTime;
                ringData.ringImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);
            }

            // Remove ring if it exceeds max scale
            if (ringData.currentScale >= maxScale)
            {
                ringsToRemove.Add(ringData);
            }
        }

        // Clean up rings that are done
        foreach (RingData ringData in ringsToRemove)
        {
            if (ringData.ringImage != null)
            {
                Destroy(ringData.ringImage.gameObject);
            }
            activeRings.Remove(ringData);
        }
    }

    private void ClearAllRings()
    {
        foreach (RingData ringData in activeRings)
        {
            if (ringData.ringImage != null)
            {
                Destroy(ringData.ringImage.gameObject);
            }
        }
        activeRings.Clear();
    }

    #endregion

}
