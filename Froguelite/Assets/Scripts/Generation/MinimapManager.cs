using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{

    // MinimapManager handles all minimap-related functionality


    #region VARIABLES


    public static MinimapManager Instance { get; private set; }

    [SerializeField] private Color landColor = Color.green;
    [SerializeField] private Color waterColor = Color.blue;
    [SerializeField] private Color unexploredColor = Color.gray;

    [SerializeField] private Image minimapDisplayImg;
    [SerializeField] private Image fullMapDisplayImg;
    [SerializeField] private CanvasGroup fullMapCanvasGroup;
    [SerializeField] private float fullMapFadeDuration = 0.2f; // Duration for fade in/out
    [SerializeField] private Transform fullMapTransform;
    [SerializeField] private CanvasGroup minimapParentCanvGroup;
    private Texture2D minimapTexture;

    private float mapHidY = -1000f;
    private float mapShownY = 0f;

    private bool[,] landTileArray; // True = land, False = water
    private bool[,] waterTileArray; // True = water, False = land
    private bool[,] exploredTileArray; // True = explored, False = unexplored

    [SerializeField] private float mapExplorationUpdateInterval = 0.1f; // Interval to update explored area
    [SerializeField] private float mapViewRadius = 20f; // Radius around player to mark as explored

    private bool canToggleFullMap = true;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    void Start()
    {
        StartCoroutine(UpdateExploredAreaCoroutine());
    }


    private void Update()
    {
        PanToPlayerPosition();
    }


    #endregion


    #region CREATE MINIMAP


    private IEnumerator UpdateExploredAreaCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(mapExplorationUpdateInterval);

            // Skip if arrays aren't initialized or player doesn't exist
            if (exploredTileArray == null || landTileArray == null || PlayerMovement.Instance == null)
                continue;

            // Get player position
            Vector3 playerPosition = PlayerMovement.Instance.transform.position;
            Vector2Int playerTilePos = new Vector2Int(Mathf.RoundToInt(playerPosition.x), Mathf.RoundToInt(playerPosition.y));

            // Get array dimensions
            int width = exploredTileArray.GetLength(0);
            int height = exploredTileArray.GetLength(1);

            bool anyNewExploration = false;

            // Update all tiles within mapViewRadius to explored
            int radiusInt = Mathf.RoundToInt(mapViewRadius);

            for (int x = playerTilePos.x - radiusInt; x <= playerTilePos.x + radiusInt; x++)
            {
                for (int y = playerTilePos.y - radiusInt; y <= playerTilePos.y + radiusInt; y++)
                {
                    // Check if coordinates are within array bounds
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        // Check if tile is within circular radius
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(playerTilePos.x, playerTilePos.y));

                        if (distance <= mapViewRadius && !exploredTileArray[x, y])
                        {
                            exploredTileArray[x, y] = true;
                            anyNewExploration = true;
                        }
                    }
                }
            }

            // Only update minimap if there were new areas explored
            if (anyNewExploration)
            {
                UpdateMinimap(playerTilePos, radiusInt);
            }
        }
    }


    public void InitializeMinimap(bool[,] landTileArray)
    {
        this.landTileArray = landTileArray;

        if (landTileArray == null)
        {
            Debug.LogWarning("MinimapGenerator: landTileArray is null, cannot initialize minimap");
            return;
        }

        // Get dimensions of the land tile array
        int width = landTileArray.GetLength(0);
        int height = landTileArray.GetLength(1);

        // Initialize explored area array - all tiles start as unexplored
        exploredTileArray = new bool[width, height];

        // Create a new texture with the same dimensions as the land tile array
        minimapTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        minimapTexture.filterMode = FilterMode.Point; // Use point filtering for pixel-perfect appearance

        // Generate the minimap pixels
        Color[] pixels = new Color[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Calculate the pixel index (Unity textures are bottom-up)
                int pixelIndex = (height - 1 - y) * width + x;

                // Show unexplored color for unexplored areas, otherwise show terrain color
                if (!exploredTileArray[x, y])
                {
                    pixels[pixelIndex] = unexploredColor;
                }
                else
                {
                    pixels[pixelIndex] = landTileArray[x, y] ? landColor : waterColor;
                }
            }
        }

        // Apply the pixels to the texture
        minimapTexture.SetPixels(pixels);
        minimapTexture.Apply();

        // Assign the texture to both minimap display images
        Sprite minimapSprite = Sprite.Create(minimapTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));

        if (minimapDisplayImg != null)
        {
            minimapDisplayImg.sprite = minimapSprite;
            minimapDisplayImg.preserveAspect = true;
        }

        if (fullMapDisplayImg != null)
        {
            fullMapDisplayImg.sprite = minimapSprite;
            fullMapDisplayImg.preserveAspect = true;
        }

        Debug.Log($"Minimap generated successfully with dimensions: {width}x{height}");
    }


    // Updates the minimap. Only updates pixels within a radius around given point.
    public void UpdateMinimap(Vector2Int center, int radius)
    {
        if (minimapTexture == null || exploredTileArray == null || landTileArray == null)
            return;

        // Get texture dimensions
        int width = minimapTexture.width;
        int height = minimapTexture.height;

        // Calculate the bounds of the area to update
        int minX = Mathf.Max(0, center.x - radius);
        int maxX = Mathf.Min(width - 1, center.x + radius);
        int minY = Mathf.Max(0, center.y - radius);
        int maxY = Mathf.Min(height - 1, center.y + radius);

        // Update pixels within the specified radius
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Check if this pixel is within the circular radius
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));

                if (distance <= radius)
                {
                    // Calculate the pixel index (Unity textures are bottom-up)
                    int pixelIndex = (height - 1 - y) * width + x;

                    Color newColor;

                    // Show unexplored color for unexplored areas, otherwise show terrain color
                    if (!exploredTileArray[x, y])
                    {
                        newColor = unexploredColor;
                    }
                    else
                    {
                        newColor = landTileArray[x, y] ? landColor : waterColor;
                    }

                    // Set the pixel color
                    minimapTexture.SetPixel(x, height - 1 - y, newColor);
                }
            }
        }

        // Apply the changes to the texture
        minimapTexture.Apply();
    }


    #endregion


    #region PANNING


    // Pans the minimap to be centered around the player's current position
    private void PanToPlayerPosition()
    {
        if (minimapTexture == null || PlayerMovement.Instance == null)
            return;

        // Get the player's current world position
        Vector3 playerWorldPos = PlayerMovement.Instance.transform.position;

        // Convert world position to texture coordinates (assuming 1:1 mapping as specified)
        Vector2 playerTexturePos = new Vector2(playerWorldPos.x, playerWorldPos.y);

        // Get texture dimensions
        float textureWidth = minimapTexture.width;
        float textureHeight = minimapTexture.height;

        // Convert player texture position to normalized coordinates (0-1)
        float normalizedX = playerTexturePos.x / textureWidth;
        float normalizedY = playerTexturePos.y / textureHeight;

        // Pan the main minimap display image
        if (minimapDisplayImg != null)
        {
            RectTransform imageRectTransform = minimapDisplayImg.rectTransform;
            Vector2 imageSize = imageRectTransform.sizeDelta;

            // Calculate the offset needed to center the player position
            float offsetX = -(normalizedX - 0.5f) * imageSize.x;
            float offsetY = -(normalizedY - 0.5f) * imageSize.y;

            // Apply the offset to the image position
            imageRectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
        }

        // Pan the full map display image
        if (fullMapDisplayImg != null)
        {
            RectTransform fullMapRectTransform = fullMapDisplayImg.rectTransform;
            Vector2 fullMapImageSize = fullMapRectTransform.sizeDelta;

            // Calculate the offset needed to center the player position
            float fullMapOffsetX = -(normalizedX - 0.5f) * fullMapImageSize.x;
            float fullMapOffsetY = -(normalizedY - 0.5f) * fullMapImageSize.y;

            // Apply the offset to the full map image position
            fullMapRectTransform.anchoredPosition = new Vector2(fullMapOffsetX, fullMapOffsetY);
        }
    }


    #endregion


    #region FULL MAP TOGGLE


    public void ToggleFullMap(bool showMap)
    {
        if (!canToggleFullMap) return;
        
        LeanTween.cancel(fullMapTransform.gameObject);
        LeanTween.cancel(fullMapCanvasGroup.gameObject);

        if (showMap)
        {
            fullMapTransform.LeanMoveLocalY(mapShownY, fullMapFadeDuration).setEaseOutQuad();
            fullMapCanvasGroup.LeanAlpha(1, fullMapFadeDuration);
        }
        else
        {
            fullMapTransform.LeanMoveLocalY(mapHidY, fullMapFadeDuration).setEaseOutQuad();
            fullMapCanvasGroup.LeanAlpha(0, fullMapFadeDuration);
        }
    }


    #endregion


    #region HIDE MINIMAP


    public void HideMinimap()
    {
        if (minimapParentCanvGroup != null)
        {
            minimapParentCanvGroup.alpha = 0;
            minimapParentCanvGroup.interactable = false;
            minimapParentCanvGroup.blocksRaycasts = false;

            ToggleFullMap(false);
            canToggleFullMap = false;
        }
    }


    #endregion


}
