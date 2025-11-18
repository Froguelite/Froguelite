using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomFactory : MonoBehaviour
{

    // RoomFactory handles the creation of room instances


    #region VARIABLES


    [SerializeField] private ShopAlternate shopAlternatePrefab;
    [SerializeField] private GameObject bossPortalPrefab;
    [SerializeField] private GameObject totemPrefab;
    [SerializeField] private SubZoneFinalDoor swampSubZoneFinalDoorPrefab;
    [SerializeField] private SubZoneFinalDoor forestSubZoneFinalDoorPrefab;


    #endregion


    #region ROOM SPAWNING


    // Spawns a room at the given grid position with the given room data
    // Uses auto-tiling for tilemap generation
    // Sets the tiles, then returns the new Room component
    public Room SpawnRoom(int zone, Tilemap roomsTilemap, AutoTileSet autoTileSet, Transform roomParent, RoomData roomData, int roomLength)
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
            case Room.RoomType.BossPortal:
                landScale = 0.8f;
                break;
            case Room.RoomType.Shop:
                landScale = 0.8f;
                break;
            case Room.RoomType.Fly:
                landScale = 0.8f;
                break;
            case Room.RoomType.Totem:
                landScale = 1.2f;
                break;
            case Room.RoomType.Normal:
                landScale = 1.5f;
                break;
            case Room.RoomType.SubZoneBoss:
                landScale = 1.2f;
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
        char[,] newRoomLayout = RoomTileHelper.GenRoomTiles(
            width: roomLength,
            height: roomLength,
            offsetX: tileOffset.x,
            offsetY: tileOffset.y,
            noiseSettings: noiseSettings
        );

        // Post process to smooth and ensure connectivity
        newRoomLayout = RoomTileHelper.SmoothRoomLayout(newRoomLayout);
        newRoomLayout = RoomTileHelper.EnsureConnectivity(newRoomLayout);

        // Add restrictions on tiles where the player will arrive 'j' and path between islands 'p'
        newRoomLayout = RoomTileHelper.AddArrivalAndPathTiles(newRoomLayout, roomData);

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

        // If this is a boss room, spawn the boss portal
        if (roomData.roomType == Room.RoomType.BossPortal)
        {
            Vector2 bossPortalSpawnPos = roomData.GetRoomCenterWorldPosition();
            GameObject bossObject = Instantiate(bossPortalPrefab, bossPortalSpawnPos, Quaternion.identity);
            bossObject.transform.SetParent(roomObject.transform);
        }

        // If this is a sub-zone boss, spawn the boss and its final door
        if (roomData.roomType == Room.RoomType.SubZoneBoss)
        {
            // Find the single door in this room (should only have one)
            DoorData singleDoor = null;
            Door.DoorDirection entranceDoorDirection = Door.DoorDirection.Up;
            
            foreach (var doorEntry in roomData.doors)
            {
                if (!doorEntry.Value.isImpassable)
                {
                    singleDoor = doorEntry.Value;
                    entranceDoorDirection = doorEntry.Key;
                    break;
                }
            }

            if (singleDoor != null)
            {
                // Get the opposite direction for the final door
                Door.DoorDirection finalDoorDirection = Door.GetOppositeDirection(entranceDoorDirection);
                
                // Calculate the position where the final door should spawn
                // This is the launch position for a door in the opposite direction
                Vector2Int doorLaunchPos = RoomTileHelper.GetDoorLocation(roomData.tileLayout, finalDoorDirection, true);
                Vector3 finalDoorWorldPos = new Vector3(
                    roomData.roomCoordinate.x * roomData.roomLength + doorLaunchPos.x + 0.5f,
                    roomData.roomCoordinate.y * roomData.roomLength + doorLaunchPos.y + 0.5f,
                    -0.1f
                );

                // Spawn the SubZoneFinalDoor prefab
                SubZoneFinalDoor subZoneFinalDoorPrefab = (zone == 0) ? swampSubZoneFinalDoorPrefab : forestSubZoneFinalDoorPrefab;
                SubZoneFinalDoor finalDoorInstance = Instantiate(subZoneFinalDoorPrefab, finalDoorWorldPos, Quaternion.identity);
                finalDoorInstance.transform.SetParent(roomObject.transform);

                // Initialize the door with the opposite direction
                finalDoorInstance.InitializeDoor(finalDoorDirection);
                
                // Initialize the room with this final door reference
                roomComponent.SetSubZoneFinalDoor(finalDoorInstance);
            }
        }

        return roomComponent;
    }


    #endregion


}
