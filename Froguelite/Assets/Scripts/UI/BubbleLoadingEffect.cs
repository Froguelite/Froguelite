using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class BubbleLoadingEffect : MonoBehaviour
{
    // BubbleLoadingEffect handles the visual effect when loading between zones

    #region VARIABLES

    [Header("Bubble Settings")]
    [SerializeField] private Image bubblePrefab; // The bubble image prefab to spawn
    [SerializeField] private RectTransform bubbleContainer; // Parent container for spawned bubbles
    [SerializeField] private CanvasGroup backgroundBlockerCanvGroup;
    [SerializeField] private Image background; // Background image to tint

    [Header("Spawn Settings")]
    [SerializeField] private float bubblesPerSecond = 60f; // Rate of bubble spawning
    [SerializeField] private float spawnWidthMultiplier = 1.2f; // Multiplier for container width (to spawn wider than screen)
    [SerializeField] private float spawnYOffset = -100f; // How far below container bottom to spawn bubbles
    
    [Header("Movement Settings")]
    [SerializeField] private float minBubbleSpeed = 300f; // Minimum upward speed
    [SerializeField] private float maxBubbleSpeed = 600f; // Maximum upward speed
    [SerializeField] private float horizontalDrift = 50f; // Random horizontal movement
    [SerializeField] private float floatDistance = 2000f; // How far bubbles travel upward
    
    [Header("Bubble Appearance")]
    [SerializeField] private float minBubbleSize = 50f; // Minimum bubble scale
    [SerializeField] private float maxBubbleSize = 150f; // Maximum bubble scale
    [SerializeField] private float minAlpha = 0.6f; // Minimum bubble transparency
    [SerializeField] private float maxAlpha = 1f; // Maximum bubble transparency
    
    private List<BubbleData> activeBubbles = new List<BubbleData>();
    private bool isEffectActive = false;
    private float spawnTimer = 0f;

    private bool usingLeaves = false;
    [SerializeField] private Sprite bubbleSprite;
    [SerializeField] private Color bubbleBgColor;
    [SerializeField] private Sprite leafSprite;
    [SerializeField] private Color leafBgColor;

    private class BubbleData
    {
        public Image bubbleImage;
        public float speed;
        public float horizontalSpeed;
        public Vector2 startPosition;
    }

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        // If no container is assigned, use this object as container
        if (bubbleContainer == null)
        {
            bubbleContainer = GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        // Spawn bubbles continuously only while effect is active
        if (isEffectActive)
        {
            spawnTimer += Time.deltaTime;
            float spawnInterval = 1f / bubblesPerSecond;
            
            while (spawnTimer >= spawnInterval)
            {
                SpawnSingleBubble();
                spawnTimer -= spawnInterval;
            }
        }

        // Always update existing bubbles even after effect stops, to let them float away
        for (int i = activeBubbles.Count - 1; i >= 0; i--)
        {
            BubbleData bubble = activeBubbles[i];
            if (bubble.bubbleImage == null)
            {
                activeBubbles.RemoveAt(i);
                continue;
            }

            RectTransform rectTransform = bubble.bubbleImage.rectTransform;
            Vector2 currentPos = rectTransform.anchoredPosition;

            // Move bubble upward and drift horizontally
            currentPos.y += bubble.speed * Time.deltaTime;
            currentPos.x += bubble.horizontalSpeed * Time.deltaTime;

            rectTransform.anchoredPosition = currentPos;

            // Remove bubbles that have traveled far enough
            if (currentPos.y > bubble.startPosition.y + floatDistance)
            {
                Destroy(bubble.bubbleImage.gameObject);
                activeBubbles.RemoveAt(i);
            }
        }
    }

    #endregion

    #region START / STOP

    public void StartEffect(bool useLeaves)
    {
        usingLeaves = useLeaves;
        if (useLeaves)
        {
            background.color = leafBgColor;
        }
        else
        {
            background.color = bubbleBgColor;
        }

        isEffectActive = true;
        spawnTimer = 0f;
        
        // Fade in background blocker
        if (backgroundBlockerCanvGroup != null)
        {
            LeanTween.cancel(backgroundBlockerCanvGroup.gameObject);
            backgroundBlockerCanvGroup.LeanAlpha(1f, 1f).setDelay(1f);
        }
    }

    public void StopEffect()
    {
        isEffectActive = false;
        
        // Fade out background blocker
        if (backgroundBlockerCanvGroup != null)
        {
            LeanTween.cancel(backgroundBlockerCanvGroup.gameObject);
            backgroundBlockerCanvGroup.LeanAlpha(0f, 1f);
        }

        // Don't clean up bubbles - let them float away naturally
        // They'll be destroyed automatically when they reach floatDistance
    }

    #endregion

    #region BUBBLE SPAWNING

    private void SpawnSingleBubble()
    {
        if (bubblePrefab == null || bubbleContainer == null)
        {
            return;
        }

        // Get container dimensions
        Rect containerRect = bubbleContainer.rect;
        float containerWidth = containerRect.width;
        float containerHeight = containerRect.height;
        
        // Calculate spawn area
        float spawnWidth = containerWidth * spawnWidthMultiplier;
        float spawnYStart = -containerHeight / 2f + spawnYOffset; // Start below bottom of container

        // Create bubble instance
        Image bubble = Instantiate(bubblePrefab, bubbleContainer);
        RectTransform rectTransform = bubble.rectTransform;
        if (usingLeaves)
        {
            bubble.sprite = leafSprite;
        }
        else
        {
            bubble.sprite = bubbleSprite;
        }

        // Random size
        float size = Random.Range(minBubbleSize, maxBubbleSize);
        rectTransform.sizeDelta = new Vector2(size, size);

        // Random alpha
        Color color = bubble.color;
        color.a = Random.Range(minAlpha, maxAlpha);
        bubble.color = color;

        // Random position at spawn line (no vertical staggering for continuous spawn)
        float xPos = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        float yPos = spawnYStart;
        Vector2 startPos = new Vector2(xPos, yPos);
        rectTransform.anchoredPosition = startPos;

        // Random speeds
        float upwardSpeed = Random.Range(minBubbleSpeed, maxBubbleSpeed);
        float horizontalSpeed = Random.Range(-horizontalDrift, horizontalDrift);

        // Store bubble data
        BubbleData bubbleData = new BubbleData
        {
            bubbleImage = bubble,
            speed = upwardSpeed,
            horizontalSpeed = horizontalSpeed,
            startPosition = startPos
        };

        activeBubbles.Add(bubbleData);
    }

    private void ClearBubbles()
    {
        foreach (var bubble in activeBubbles)
        {
            if (bubble.bubbleImage != null)
            {
                Destroy(bubble.bubbleImage.gameObject);
            }
        }
        activeBubbles.Clear();
    }

    #endregion

    #region HELPERS

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    #endregion

}
