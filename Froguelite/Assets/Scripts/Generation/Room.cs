using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{

    // Room represents a single room in a zone, managing its type, position, doors, spawning, etc.


    #region VARIABLES


    public enum RoomType
    {
        Normal,
        Starter,
        BossPortal,
        SubZoneBoss,
        Shop,
        Fly,
        Totem,
    }

    public RoomData roomData { get; protected set; }

    public List<Door> doors { get; private set; } = new List<Door>();

    public List<IEnemy> enemies { get; private set; } = new List<IEnemy>();

    private SubZoneFinalDoor subZoneFinalDoor;
    public SubZoneFinalDoor SubZoneFinalDoor => subZoneFinalDoor;

    public bool isExplored { get; private set; } = false;

    // Totem integration
    private Totem totemInstance;
    private bool isTotemActive = false;


    #endregion


    #region SETUP


    // Initializes the room based on given roomData
    public void Initialize(RoomData roomData)
    {
        this.roomData = roomData;

        // Perform any room-specific setup
        switch (roomData.roomType)
        {
            case RoomType.Starter:
                break;
            case RoomType.BossPortal:
                // TODO: Boss room specific setup
                break;
            case RoomType.SubZoneBoss:
                // TODO: Sub-zone boss specific setup
                break;
            case RoomType.Shop:
                // TODO: Spawn shopkeeper and shop items
                break;
            case RoomType.Fly:
                PowerFlyData rolledFly = PowerFlyFactory.Instance.RollFlyForFlyRoom();
                if (rolledFly != null)
                {
                    Vector3 flyPosition = roomData.GetRoomCenterWorldPosition();
                    PowerFlyFactory.Instance.SpawnPowerFly(rolledFly, transform, flyPosition);
                }
                else
                {
                    Debug.LogWarning("No Power Fly available to spawn in Fly Room.");
                }
                break;
            case RoomType.Totem:
                // Spawn a totem at the center
                break;
            case RoomType.Normal:
            default:
                // TODO: Spawn enemies
                break;
        }
    }


    // Adds a door to the room's door list
    public void AddDoor(Door door)
    {
        doors.Add(door);
    }


    // Generates a few enemies for this room
    public void GenerateEnemies()
    {
        if (roomData.roomType == RoomType.Normal)
            enemies = EnemyFactory.Instance.SpawnEnemiesForRoom(roomData.zone, this);
        else if (roomData.roomType == RoomType.SubZoneBoss)
            enemies = EnemyFactory.Instance.SpawnSubZoneBossForRoom(roomData.zone, this);
    }


    // Generates shop for this room
    public void GenerateShop(ShopAlternate shop)
    {
        if (roomData.roomType != RoomType.Shop)
            return; // Only generate shop in shop rooms

        // Create a new shop instance
        ShopAlternate newShop = Instantiate(shop, roomData.GetRoomCenterWorldPosition(), Quaternion.identity);
        newShop.transform.SetParent(transform);
        newShop.SetupShop();
    }


    // Returns a random valid spawn position on land within this room
    public Vector2 GetRandomEnemySpawnPosition()
    {
        List<Vector2Int> validTiles = new List<Vector2Int>();

        // Collect all walkable (land) tiles
        for (int x = 0; x < roomData.roomLength; x++)
        {
            for (int y = 0; y < roomData.roomLength; y++)
            {
                if (roomData.tileLayout[x, y] == 'l') // 'l' = land
                {
                    validTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        // If no valid tiles found, return center of room as fallback
        if (validTiles.Count == 0)
        {
            Debug.LogWarning("No valid spawn positions found in room. Using room center as fallback.");
            return roomData.GetRoomCenterWorldPosition();
        }

        // Select a random valid tile
        Vector2Int randomTile = validTiles[UnityEngine.Random.Range(0, validTiles.Count)];

        // Convert tile coordinate to world position
        Vector3 worldPos = roomData.GetTileWorldPosition(randomTile);
        return new Vector2(worldPos.x, worldPos.y);
    }


    #endregion


    #region PLAYER ENTER AND LEAVE CONDITIONS


    // Called when the player enters the room
    public void OnPlayerEnter()
    {
        if (enemies.Count == 0)
            return;

        // Engage enemies with the player
        foreach (IEnemy enemy in enemies)
        {
            enemy.BeginPlayerChase();
        }
    }


    // Called when the player completes a door transition into this room
    // (i.e. lilypad frog is fully back to original island)
    public void OnDoorTransitionComplete()
    {
        // If no enemies are present, we are safe to clear the room
        if (enemies.Count == 0)
        {
            OnRoomCleared();
            return;
        }
    }


    // Called when a particular enemy is defeated
    public void OnEnemyDefeated(EnemyBase defeatedEnemy)
    {
        if (enemies.Contains(defeatedEnemy))
        {
            enemies.Remove(defeatedEnemy);
        }

        // Forward defeat to totem if active
        if (totemInstance != null && isTotemActive)
        {
            totemInstance.OnEnemyDefeated(defeatedEnemy);
        }

        // If all enemies are defeated, mark room as cleared
        if (enemies.Count == 0)
        {
            // During active totem waves, doors should not auto-open on room clear
            if (!isTotemActive)
            {
                if (roomData.roomType == RoomType.Normal)
                    RoomManager.Instance.SpawnRoomClearItems(defeatedEnemy.transform.position, LevelManager.Instance.currentZone);
                else if (roomData.roomType == RoomType.SubZoneBoss)
                    RoomManager.Instance.SpawnSubZoneBossClearItems(defeatedEnemy.transform.position, LevelManager.Instance.currentZone);
                OnRoomCleared();
            }
        }
    }


    // Called when the room is cleared of all enemies
    public void OnRoomCleared()
    {
        isExplored = true;

        // Clear all swarm centers
        SwarmManager.Instance.ClearAllSwarms();

        // Open all doors when room is cleared
        DoorManager.Instance.OpenAllDoors(true);

        // Ping the minimap notifying we have cleared the room
        MinimapManager.Instance.OnClearRoom(this);

        // Perform sub-zone specific actions
        if (roomData.roomType == RoomType.SubZoneBoss)
        {
            OnSubZoneBossDefeated();
        }
    }


    #endregion


    #region SUB-ZONE BOSS ROOM


    public void SetSubZoneFinalDoor(SubZoneFinalDoor finalDoor)
    {
        subZoneFinalDoor = finalDoor;
    }


    private void OnSubZoneBossDefeated()
    {
        Debug.Log("Sub-zone boss defeated!");
        // Open the final door when the sub-zone boss is defeated
        if (subZoneFinalDoor != null)
        {
            subZoneFinalDoor.OpenDoor();
        }
    }


    #endregion


    #region HELPERS


    // Gets a room adjacent to this one in the given direction, or null if none exists
    public Room GetAdjacentRoom(Door.DoorDirection direction)
    {
        Vector2Int adjacentCoord = roomData.roomCoordinate;

        switch (direction)
        {
            case Door.DoorDirection.Up:
                adjacentCoord += new Vector2Int(0, 1);
                break;
            case Door.DoorDirection.Right:
                adjacentCoord += new Vector2Int(1, 0);
                break;
            case Door.DoorDirection.Down:
                adjacentCoord += new Vector2Int(0, -1);
                break;
            case Door.DoorDirection.Left:
                adjacentCoord += new Vector2Int(-1, 0);
                break;
        }

        return RoomManager.Instance.GetRoomAtGridPosition(adjacentCoord);
    }


    // Gets the world space position directly between this room and the adjacent room in the given direction
    public Vector3 GetRoomConnectionCenter(Door.DoorDirection direction)
    {
        Vector3 roomCenter = roomData.GetRoomCenterWorldPosition();
        Vector3 adjacentRoomCenter = GetAdjacentRoom(direction)?.roomData.GetRoomCenterWorldPosition() ?? roomCenter;

        return (roomCenter + adjacentRoomCenter) / 2f;
    }

    // Gets the world space position of the final door connection (position of the final door itself)
    public Vector3 GetFinalDoorConnectionPosition()
    {
        if (subZoneFinalDoor == null)
            return Vector3.zero;

        return subZoneFinalDoor.transform.position;
    }

    // Registers a totem that controls this room's wave flow
    public void SetTotem(Totem totem)
    {
        totemInstance = totem;
    }

    // Called by Totem to indicate waves are active/inactive
    public void SetTotemActive(bool active)
    {
        isTotemActive = active;
    }

    // Called by Totem when all waves are complete
    public void OnTotemCompleted()
    {
        isTotemActive = false;
        OnRoomCleared();
    }


    #endregion


}


[System.Serializable]
public class RoomData
{


    #region VARIABLES


    public Room.RoomType roomType;
    public Vector2Int roomCoordinate;

    // 2D array representing the tiles in this room: 
    // l = land, 
    // w = water, 
    // j = landing zone (land), 
    // p = room path (water)
    public char[,] tileLayout; 
    public int roomLength; // Length of one side of the square room in tiles
    public Dictionary<Door.DoorDirection, DoorData> doors;

    public float genWeight = 1f; // Weight for random selection during generation (higher = more likely)
    public bool isLeaf = false; // Whether this room is a leaf node (dead end) in the graph
    public PerlinNoiseSettings originalNoiseSettings; // Original noise settings used for generating the room layout
    public int zone = 0; // Zone this room belongs to (0 = swamp, 1 = forest, etc.)


    #endregion


    #region CONSTRUCTORS


    // Constructor to initialize base RoomData with type and coordinates. Doors are set as empty.
    public RoomData(Room.RoomType roomType, Vector2Int roomCoordinate, int zone)
    {
        this.roomType = roomType;
        this.roomCoordinate = roomCoordinate;
        this.zone = zone;
        InitializeDoors();
    }


    #endregion


    #region DOOR MANAGEMENT


    // Initializes all four doors for the room with default data (impassable, closed, unlocked)
    public void InitializeDoors()
    {
        doors = new Dictionary<Door.DoorDirection, DoorData>();

        for (int i = 0; i < 4; i++)
        {
            DoorData newDoorData = new DoorData
            {
                isImpassable = true,
                isOpen = false,
                isLocked = false,
                direction = (Door.DoorDirection)i
            };

            doors.Add((Door.DoorDirection)i, newDoorData);
        }
    }

    // Updates door in given direction with new door data
    public void SetDoor(Door.DoorDirection direction, DoorData doorData)
    {
        if (doors.ContainsKey(direction))
        {
            doors[direction] = doorData;
        }
    }


    #endregion


    #region OTHER HELPERS


    // Returns the world space position of the given tile coordinate within the room
    public Vector3 GetTileWorldPosition(Vector2Int tileCoord)
    {
        return new Vector3(roomCoordinate.x * roomLength + tileCoord.x + 0.5f, roomCoordinate.y * roomLength + tileCoord.y + 0.5f, 0);
    }


    // Returns the center of this room in world space
    public Vector3 GetRoomCenterWorldPosition()
    {
        return new Vector3(roomCoordinate.x * roomLength + roomLength / 2f, roomCoordinate.y * roomLength + roomLength / 2f, 0);
    }


    // Returns whether the given tile coordinate is bordering a change in land/water on any cardinal direction
    public bool IsTileBorderingChange(Vector2Int tileCoord)
    {
        bool currentTileIsLand = tileLayout[tileCoord.x, tileCoord.y] == 'l'; // 'l' = land

        // Check all four cardinal directions
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborCoord = tileCoord + dir;

            // Ensure neighbor is within bounds
            if (neighborCoord.x >= 0 && neighborCoord.x < roomLength && neighborCoord.y >= 0 && neighborCoord.y < roomLength)
            {
                if ((tileLayout[neighborCoord.x, neighborCoord.y] == 'l') != currentTileIsLand)
                {
                    return true; // Found a bordering change
                }
            }
        }

        return false; // No bordering changes found
    }


    #endregion


}

