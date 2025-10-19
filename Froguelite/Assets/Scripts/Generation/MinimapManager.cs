using System.Collections;
using System.Collections.Generic;
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

    private char[,] landTileArray; // True = land, False = water
    private char[,] waterTileArray; // True = water, False = land
    private bool[,] exploredTileArray; // True = explored, False = unexplored

    [SerializeField] private float mapExplorationUpdateInterval = 0.1f; // Interval to update explored area
    [SerializeField] private float mapViewRadius = 20f; // Radius around player to mark as explored

    private bool canToggleFullMap = true;

    [SerializeField] private MinimapRoomConnection roomConnectionPrefab;
    private Dictionary<Room, Dictionary<Door.DoorDirection, MinimapRoomConnection>> minimapRoomConnections = new Dictionary<Room, Dictionary<Door.DoorDirection, MinimapRoomConnection>>();
    private Dictionary<Room, Dictionary<Door.DoorDirection, MinimapRoomConnection>> fullMapRoomConnections = new Dictionary<Room, Dictionary<Door.DoorDirection, MinimapRoomConnection>>();


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


    public void InitializeMinimap(char[,] landTileArray)
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
                    pixels[pixelIndex] = (landTileArray[x, y] == 'l' || landTileArray[x, y] == 'j') ? landColor : waterColor;
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
                        newColor = (landTileArray[x, y] == 'l' || landTileArray[x, y] == 'j') ? landColor : waterColor;
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


    #region COORDINATE CONVERSION


    // Converts a world position to a local position on the minimap display image
    public Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        if (minimapTexture == null || minimapDisplayImg == null)
        {
            Debug.LogWarning("MinimapManager: Cannot convert world position - minimap not initialized");
            return Vector2.zero;
        }

        // Get texture dimensions
        float textureWidth = minimapTexture.width;
        float textureHeight = minimapTexture.height;

        // Convert world position to texture coordinates (assuming 1:1 mapping)
        Vector2 texturePos = new Vector2(worldPosition.x, worldPosition.y);

        // Convert to normalized coordinates (0-1)
        float normalizedX = texturePos.x / textureWidth;
        float normalizedY = texturePos.y / textureHeight;

        // Get the minimap display image rect transform and size
        RectTransform imageRectTransform = minimapDisplayImg.rectTransform;
        Vector2 imageSize = imageRectTransform.sizeDelta;

        // Convert normalized coordinates to local position on the minimap image
        // The minimap image origin is at its center, so we need to offset from center
        float localX = (normalizedX - 0.5f) * imageSize.x;
        float localY = (normalizedY - 0.5f) * imageSize.y;

        return new Vector2(localX, localY);
    }


    // Converts a world position to a local position on the full map display image
    public Vector2 WorldToFullMapPosition(Vector3 worldPosition)
    {
        if (minimapTexture == null || fullMapDisplayImg == null)
        {
            Debug.LogWarning("MinimapManager: Cannot convert world position - full map not initialized");
            return Vector2.zero;
        }

        // Get texture dimensions
        float textureWidth = minimapTexture.width;
        float textureHeight = minimapTexture.height;

        // Convert world position to texture coordinates (assuming 1:1 mapping)
        Vector2 texturePos = new Vector2(worldPosition.x, worldPosition.y);

        // Convert to normalized coordinates (0-1)
        float normalizedX = texturePos.x / textureWidth;
        float normalizedY = texturePos.y / textureHeight;

        // Get the full map display image rect transform and size
        RectTransform imageRectTransform = fullMapDisplayImg.rectTransform;
        Vector2 imageSize = imageRectTransform.sizeDelta;

        // Convert normalized coordinates to local position on the full map image
        // The full map image origin is at its center, so we need to offset from center
        float localX = (normalizedX - 0.5f) * imageSize.x;
        float localY = (normalizedY - 0.5f) * imageSize.y;

        return new Vector2(localX, localY);
    }


    #endregion


    #region DOORS AND CONNECTIONS


    // Called when clearing a room, update doors / connections
    public void OnClearRoom(Room clearedRoom)
    {
        // Initialize dictionaries for this room if they don't exist
        if (!minimapRoomConnections.ContainsKey(clearedRoom))
        {
            minimapRoomConnections[clearedRoom] = new Dictionary<Door.DoorDirection, MinimapRoomConnection>();
        }
        if (!fullMapRoomConnections.ContainsKey(clearedRoom))
        {
            fullMapRoomConnections[clearedRoom] = new Dictionary<Door.DoorDirection, MinimapRoomConnection>();
        }

        // Loop through each door in the cleared room
        foreach (var entry in clearedRoom.roomData.doors)
        {
            // Get the door direction and corresponding connection
            Door.DoorDirection doorDir = entry.Key;
            DoorData doorData = entry.Value;

            // If the door is a real door (not impassable), and is not connected to an explored room, create a new connection
            if (!doorData.isImpassable && clearedRoom.GetAdjacentRoom(doorDir) != null &&
                !clearedRoom.GetAdjacentRoom(doorDir).isExplored)
            {
                Vector3 connectionCenter = clearedRoom.GetRoomConnectionCenter(doorDir);

                // Setup the connection based on door direction
                MinimapRoomConnection.ConnectionOrientation orientation = (doorDir == Door.DoorDirection.Up || doorDir == Door.DoorDirection.Down) ?
                    MinimapRoomConnection.ConnectionOrientation.Vertical : MinimapRoomConnection.ConnectionOrientation.Horizontal;

                // Determine active connection point based on door direction
                MinimapRoomConnection.ConnectionPoint activePoint = (doorDir == Door.DoorDirection.Up || doorDir == Door.DoorDirection.Right) ?
                    MinimapRoomConnection.ConnectionPoint.PointA : MinimapRoomConnection.ConnectionPoint.PointB;

                // Create connection on the minimap
                MinimapRoomConnection minimapConnection = Instantiate(roomConnectionPrefab, minimapDisplayImg.transform);
                Vector2 minimapPos = WorldToMinimapPosition(connectionCenter);
                minimapPos.y = -minimapPos.y;
                minimapConnection.transform.localPosition = minimapPos;
                minimapConnection.SetupConnection(orientation, activePoint);

                // Create connection on the full map
                MinimapRoomConnection fullMapConnection = Instantiate(roomConnectionPrefab, fullMapDisplayImg.transform);
                Vector2 fullMapPos = WorldToFullMapPosition(connectionCenter);
                fullMapPos.y = -fullMapPos.y;
                fullMapConnection.transform.localPosition = fullMapPos;
                fullMapConnection.SetupConnection(orientation, activePoint);

                // Add connections to dictionaries
                minimapRoomConnections[clearedRoom][doorDir] = minimapConnection;
                fullMapRoomConnections[clearedRoom][doorDir] = fullMapConnection;
            }    
        }
    }


    // Called when player travels through a door from one room to another
    public void OnPlayerTravelThroughDoor(Room fromRoom, Door.DoorDirection doorDirection)
    {
        // Check if we have connections for this room and door direction
        if (minimapRoomConnections.ContainsKey(fromRoom) && minimapRoomConnections[fromRoom].ContainsKey(doorDirection))
        {
            // Play transfer animation on the minimap connection
            MinimapRoomConnection minimapConnection = minimapRoomConnections[fromRoom][doorDirection];
            if (minimapConnection != null)
            {
                minimapConnection.PlayTransferAnimation();
            }
        }

        if (fullMapRoomConnections.ContainsKey(fromRoom) && fullMapRoomConnections[fromRoom].ContainsKey(doorDirection))
        {
            // Play transfer animation on the full map connection
            MinimapRoomConnection fullMapConnection = fullMapRoomConnections[fromRoom][doorDirection];
            if (fullMapConnection != null)
            {
                fullMapConnection.PlayTransferAnimation();
            }
        }
    }


    #endregion


}
