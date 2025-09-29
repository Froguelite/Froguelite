using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomFactory : MonoBehaviour
{

    // RoomFactory handles the creation of room instances


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

        // Get the room layout using Perlin with octaves (use default noise params)
        bool[,] newRoomLayout = RoomTileHelper.GenRoomTiles(
            width: roomLength,
            height: roomLength,
            offsetX: tileOffset.x,
            offsetY: tileOffset.y,
            landScale: landScale
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
        return roomComponent;
    }


    #endregion


}
