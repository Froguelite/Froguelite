using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{

    // MinimapManager handles all minimap-related functionality


    #region VARIABLES


    public static MinimapManager Instance { get; private set; }

    [SerializeField] private Image minimapDisplayImg;
    private Texture2D minimapTexture;


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
        if (minimapDisplayImg == null || minimapTexture == null || PlayerMovement.Instance == null)
            return;

        // Get the player's current world position
        Vector3 playerWorldPos = PlayerMovement.Instance.transform.position;
        
        // Convert world position to texture coordinates (assuming 1:1 mapping as specified)
        Vector2 playerTexturePos = new Vector2(playerWorldPos.x, playerWorldPos.y);
        
        // Get texture dimensions
        float textureWidth = minimapTexture.width;
        float textureHeight = minimapTexture.height;
        
        // Get the image's RectTransform
        RectTransform imageRectTransform = minimapDisplayImg.rectTransform;
        
        // Calculate the offset needed to center the player position in the masked view area
        // The image should be moved so that the player's texture position aligns with the center of the mask
        
        // Convert player texture position to normalized coordinates (0-1)
        float normalizedX = playerTexturePos.x / textureWidth;
        float normalizedY = playerTexturePos.y / textureHeight;
        
        // Get the full size of the image (before masking)
        Vector2 imageSize = imageRectTransform.sizeDelta;
        
        // Calculate the offset needed to center the player position
        // Move the image so that the player's position appears at the center of the masked area
        float offsetX = -(normalizedX - 0.5f) * imageSize.x;
        float offsetY = -(normalizedY - 0.5f) * imageSize.y;
        
        // Apply the offset to the image position
        imageRectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
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

        // Define colors
        Color landColor = Color.green;  // True values (land)
        Color waterColor = Color.blue;  // False/null values (water)

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

        // Assign the texture to the minimap display image
        minimapDisplayImg.sprite = Sprite.Create(minimapTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        minimapDisplayImg.preserveAspect = true;

        Debug.Log($"Minimap generated successfully with dimensions: {width}x{height}");
    }


    #endregion
    

}
