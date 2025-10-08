using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{

    // MinimapManager handles all minimap-related functionality


    #region VARIABLES


    public static MinimapManager Instance { get; private set; }

    [SerializeField] private Color landColor = Color.green;
    [SerializeField] private Color waterColor = Color.blue;

    [SerializeField] private Image minimapDisplayImg;
    [SerializeField] private Image fullMapDisplayImg;
    [SerializeField] private CanvasGroup fullMapCanvasGroup;
    [SerializeField] private float fullMapFadeDuration = 0.2f; // Duration for fade in/out
    [SerializeField] private Transform fullMapTransform;
    private Texture2D minimapTexture;

    private float mapHidY = -1000f;
    private float mapShownY = 0f;


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


    private void Update()
    {
        PanToPlayerPosition();
    }

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


    #region CREATE MINIMAP


    // Initializes the minimap based on given roomGraph
    public void InitializeMinimap(bool[,] roomGraph)
    {
        if (roomGraph == null)
        {
            Debug.LogWarning("MinimapGenerator: roomGraph is null, cannot initialize minimap");
            return;
        }

        // Get dimensions of the room graph
        int width = roomGraph.GetLength(0);
        int height = roomGraph.GetLength(1);

        // Create a new texture with the same dimensions as the room graph
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

                // Set pixel color based on room graph value
                // True = green (land), False or null = blue (water)
                pixels[pixelIndex] = roomGraph[x, y] ? landColor : waterColor;
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


    #endregion


    #region FULL MAP TOGGLE


    public void ToggleFullMap(bool showMap)
    {
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


}
