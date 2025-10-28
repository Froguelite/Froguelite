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
    
    // Room-based connection tracking - each connection is stored once per room pair
    private Dictionary<(Room, Room), MinimapRoomConnection> minimapRoomConnections = new Dictionary<(Room, Room), MinimapRoomConnection>();
    private Dictionary<(Room, Room), MinimapRoomConnection> fullMapRoomConnections = new Dictionary<(Room, Room), MinimapRoomConnection>();
    
    // Helper dictionary to quickly find all connections involving a specific room
    private Dictionary<Room, List<(Room, Room)>> roomConnectionKeys = new Dictionary<Room, List<(Room, Room)>>();


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


    // Helper method to create a room pair key (always ordered consistently)
    private (Room, Room) CreateRoomPairKey(Room room1, Room room2)
    {
        // Use a consistent ordering to avoid duplicate keys for the same room pair
        return room1.GetHashCode() < room2.GetHashCode() ? (room1, room2) : (room2, room1);
    }


    // Helper method to add a connection key to the lookup dictionary
    private void AddConnectionKeyToLookup(Room room, (Room, Room) connectionKey)
    {
        if (!roomConnectionKeys.ContainsKey(room))
        {
            roomConnectionKeys[room] = new List<(Room, Room)>();
        }
        
        if (!roomConnectionKeys[room].Contains(connectionKey))
        {
            roomConnectionKeys[room].Add(connectionKey);
        }
    }


    // Helper method to determine connection orientation
    private MinimapRoomConnection.ConnectionOrientation GetConnectionProperties(Room fromRoom, Room toRoom, Door.DoorDirection doorDirection)
    {
        return (doorDirection == Door.DoorDirection.Up || doorDirection == Door.DoorDirection.Down) ?
            MinimapRoomConnection.ConnectionOrientation.Vertical : MinimapRoomConnection.ConnectionOrientation.Horizontal;
    }


    // Helper method to get the opposite door direction
    private Door.DoorDirection GetOppositeDoorDirection(Door.DoorDirection direction)
    {
        switch (direction)
        {
            case Door.DoorDirection.Up:
                return Door.DoorDirection.Down;
            case Door.DoorDirection.Down:
                return Door.DoorDirection.Up;
            case Door.DoorDirection.Left:
                return Door.DoorDirection.Right;
            case Door.DoorDirection.Right:
                return Door.DoorDirection.Left;
            default:
                return direction;
        }
    }


    // Called when clearing a room, update doors / connections
    public void OnClearRoom(Room clearedRoom)
    {
        // Loop through each door in the cleared room
        foreach (var entry in clearedRoom.roomData.doors)
        {
            Door.DoorDirection doorDir = entry.Key;
            DoorData doorData = entry.Value;

            // If the door is a real door (not impassable), and connects to an unexplored room
            if (!doorData.isImpassable)
            {
                Room adjacentRoom = clearedRoom.GetAdjacentRoom(doorDir);
                if (adjacentRoom != null && !adjacentRoom.isExplored)
                {
                    // Create connection key for this room pair
                    var connectionKey = CreateRoomPairKey(clearedRoom, adjacentRoom);
                    
                    // Skip if connection already exists
                    if (minimapRoomConnections.ContainsKey(connectionKey))
                        continue;

                    Vector3 connectionCenter = clearedRoom.GetRoomConnectionCenter(doorDir);

                    // Get connection orientation
                    MinimapRoomConnection.ConnectionOrientation orientation = GetConnectionProperties(clearedRoom, adjacentRoom, doorDir);

                    // Check if the door is locked (check both directions)
                    bool isConnectionLocked = doorData.isLocked;
                    if (!isConnectionLocked && adjacentRoom.roomData.doors.ContainsKey(GetOppositeDoorDirection(doorDir)))
                    {
                        isConnectionLocked = adjacentRoom.roomData.doors[GetOppositeDoorDirection(doorDir)].isLocked;
                    }

                    // Create connection on the minimap
                    MinimapRoomConnection minimapConnection = Instantiate(roomConnectionPrefab, minimapDisplayImg.transform);
                    Vector2 minimapPos = WorldToMinimapPosition(connectionCenter);
                    minimapPos.y = -minimapPos.y;
                    minimapConnection.transform.localPosition = minimapPos;
                    minimapConnection.SetupConnection(orientation, isConnectionLocked);

                    // Create connection on the full map
                    MinimapRoomConnection fullMapConnection = Instantiate(roomConnectionPrefab, fullMapDisplayImg.transform);
                    Vector2 fullMapPos = WorldToFullMapPosition(connectionCenter);
                    fullMapPos.y = -fullMapPos.y;
                    fullMapConnection.transform.localPosition = fullMapPos;
                    fullMapConnection.SetupConnection(orientation, isConnectionLocked);

                    // Store connections
                    minimapRoomConnections[connectionKey] = minimapConnection;
                    fullMapRoomConnections[connectionKey] = fullMapConnection;
                    
                    // Add to lookup dictionaries for both rooms
                    AddConnectionKeyToLookup(clearedRoom, connectionKey);
                    AddConnectionKeyToLookup(adjacentRoom, connectionKey);
                }
            }
        }
    }


    // Called when player travels through a door from one room to another
    public void OnPlayerTravelThroughDoor(Room fromRoom, Door.DoorDirection doorDirection)
    {
        Room toRoom = fromRoom.GetAdjacentRoom(doorDirection);
        if (toRoom == null) return;

        // Animate the specific connection between fromRoom and toRoom
        var connectionKey = CreateRoomPairKey(fromRoom, toRoom);
        
        if (minimapRoomConnections.ContainsKey(connectionKey))
        {
            minimapRoomConnections[connectionKey].PlayTransferAnimation();
        }
        
        if (fullMapRoomConnections.ContainsKey(connectionKey))
        {
            fullMapRoomConnections[connectionKey].PlayTransferAnimation();
        }
    }


    // Called when a door is unlocked, update the associated connection if it exists
    public void OnDoorUnlocked(Room room, Door.DoorDirection doorDirection)
    {
        Room adjacentRoom = room.GetAdjacentRoom(doorDirection);
        if (adjacentRoom == null) return;

        // Create connection key for this room pair
        var connectionKey = CreateRoomPairKey(room, adjacentRoom);
        
        // Check if connection exists and unlock it
        if (minimapRoomConnections.ContainsKey(connectionKey))
        {
            minimapRoomConnections[connectionKey].UnlockConnection();
        }
        
        if (fullMapRoomConnections.ContainsKey(connectionKey))
        {
            fullMapRoomConnections[connectionKey].UnlockConnection();
        }
    }


    #endregion


}
