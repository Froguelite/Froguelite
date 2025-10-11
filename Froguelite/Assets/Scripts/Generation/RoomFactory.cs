using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomFactory : MonoBehaviour
{

    // RoomFactory handles the creation of room instances


    #region VARIABLES


    [SerializeField] private BoxCollider2D enemyNavPlane;


    #endregion


    #region ROOM SPAWNING


    // Spawns a room at the given grid position with the given room data
    // Uses auto-tiling for tilemap generation
    // Sets the tiles, then returns the new Room component
    public Room SpawnRoom(Tilemap roomsTilemap, AutoTileSet autoTileSet, Transform roomParent, RoomData roomData, int roomLength)
    {
        // Get the offset of this room in tile coordinates
        Vector2Int tileOffset = new Vector2Int(
            roomData.roomCoordinate.x * roomLength,
            roomData.roomCoordinate.y * roomLength
        );

        // Determine land scale based on room type
        float landScale = 1f;
        switch (roomData.roomType)
        {
            case Room.RoomType.Starter:
                landScale = 1.0f;
                break;
            case Room.RoomType.Boss:
                landScale = 1.95f;
                break;
            case Room.RoomType.Shop:
                landScale = 0.5f;
                break;
            case Room.RoomType.Fly:
                landScale = 0.5f;
                break;
            case Room.RoomType.Normal:
                landScale = 1.5f;
                break;
        }

        // Set up the Perlin noise settings and make sure the room has a reference to its original used values
        PerlinNoiseSettings noiseSettings = new PerlinNoiseSettings
        {
            octaves = 3,
            persistence = 0.5f,
            lacunarity = 2f,
            noiseScale = 0.1f,
            threshold = 0.4f
        };
        noiseSettings.landScale = landScale;
        noiseSettings.octaveOffsetsX = new float[noiseSettings.octaves];
        noiseSettings.octaveOffsetsY = new float[noiseSettings.octaves];

        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            noiseSettings.octaveOffsetsX[i] = Random.Range(-1000f, 1000f);
            noiseSettings.octaveOffsetsY[i] = Random.Range(-1000f, 1000f);
        }

        roomData.originalNoiseSettings = noiseSettings;

        // Generate the room layout using Perlin noise
        bool[,] newRoomLayout = RoomTileHelper.GenRoomTiles(
            width: roomLength,
            height: roomLength,
            offsetX: tileOffset.x,
            offsetY: tileOffset.y,
            noiseSettings: noiseSettings
        );

        // Post process to smooth and ensure connectivity
        newRoomLayout = RoomTileHelper.SmoothRoomLayout(newRoomLayout);
        newRoomLayout = RoomTileHelper.EnsureConnectivity(newRoomLayout);

        roomData.tileLayout = newRoomLayout;
        roomData.roomLength = roomLength;

        // Set the tilemap to match the new room layout using auto-tiling
        RoomTileHelper.SetTilemapToLayoutWithAutoTiling(
            newRoomLayout,
            roomsTilemap,
            autoTileSet,
            tileOffset.x,
            tileOffset.y
        );

        // Create a new GameObject to hold the Room component
        GameObject roomObject = new GameObject($"Room_{roomData.roomCoordinate.x}_{roomData.roomCoordinate.y}_{roomData.roomType}");
        Room roomComponent = roomObject.AddComponent<Room>();
        roomComponent.Initialize(roomData);
        roomObject.transform.SetParent(roomParent);

        // Create and configure the enemy navigation plane
        SpawnEnemyNavPlane(roomObject, roomData, roomLength);

        return roomComponent;
    }


    #endregion


    #region ENEMY NAVIGATION PLANE


    // Spawns an enemy navigation plane that covers the full area of the room
    private void SpawnEnemyNavPlane(GameObject roomObject, RoomData roomData, int roomLength)
    {
        if (enemyNavPlane == null)
        {
            Debug.LogWarning("Enemy nav plane prefab is not assigned in RoomFactory");
            return;
        }

        // Create the enemy nav plane as a child of the room
        BoxCollider2D navPlaneInstance = Instantiate(enemyNavPlane, roomObject.transform);
        navPlaneInstance.name = "EnemyNavPlane";

        // Calculate the room bounds in world coordinates
        Vector3 roomWorldOffset = new Vector3(
            roomData.roomCoordinate.x * roomLength,
            roomData.roomCoordinate.y * roomLength,
            0
        );

        // Position the nav plane at the center of the room
        Vector3 roomCenter = roomWorldOffset + new Vector3(roomLength * 0.5f, roomLength * 0.5f, 0);
        navPlaneInstance.transform.position = roomCenter;

        // Set the size of the BoxCollider to cover the entire room
        navPlaneInstance.size = new Vector3(roomLength, roomLength, 1f);

        // Ensure it's positioned slightly above the ground for proper navigation
        Vector3 navPlanePosition = navPlaneInstance.transform.position;
        navPlanePosition.z = 0.5f; // Position it slightly above the tilemap
        navPlaneInstance.transform.position = navPlanePosition;
    }


    #endregion


}
