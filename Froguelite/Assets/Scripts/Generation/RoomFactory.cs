using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomFactory : MonoBehaviour
{

    // RoomFactory handles the creation of room instances


    #region VARIABLES


    [SerializeField] private ShopAlternate shopAlternatePrefab;
    [SerializeField] private GameObject bossTempPrefab;


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
                landScale = 0.8f;
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

        // Generate enemies for the room
        roomComponent.GenerateEnemies();

        // If this is a shop, generate shop items
        if (roomData.roomType == Room.RoomType.Shop)
        {
            roomComponent.GenerateShop(shopAlternatePrefab);
        }

        // If this is a boss room, spawn the boss
        if (roomData.roomType == Room.RoomType.Boss)
        {
            Vector2 bossSpawnPosition = roomData.GetRoomCenterWorldPosition();
            GameObject bossObject = Instantiate(bossTempPrefab, bossSpawnPosition, Quaternion.identity);
            bossObject.transform.SetParent(roomObject.transform);
        }

        return roomComponent;
    }


    #endregion


}
