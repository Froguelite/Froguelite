using System.Collections.Generic;
using UnityEngine;

public static class RoomGraphGenerator
{

    // RoomGraphGenerator defines the logic for generating a graph of rooms within a zone


    #region GENERATION


    public static RoomData[,] GetRoomGraph(int maxDistFromStarter, int roomSizeScaler = 1)
    {
        // STEP 1: Create empty graph with starter room in center
        //-----------------------------------------//
        RoomData[,] roomGraph = new RoomData[maxDistFromStarter * 2 + 1, maxDistFromStarter * 2 + 1];
        Vector2Int starterPos = new Vector2Int(maxDistFromStarter, maxDistFromStarter);
        Vector2Int currentPos = starterPos;
        roomGraph[starterPos.x, starterPos.y] = new RoomData(
            Room.RoomType.Starter,
            new Vector2Int(starterPos.x, starterPos.y)
        );

        // STEP 2: Generate path to boss room; 4-6 rooms long, always "away" from starter room, ends with boss room.
        //-----------------------------------------//
        int pathLength = Random.Range(1, 3) + roomSizeScaler;
        for (int i = 0; i < pathLength; i++)
        {
            // Place a normal room in a random direction away from the starter room
            AddRandRoomToStarterPath(Room.RoomType.Normal, currentPos, starterPos, roomGraph, out currentPos, out roomGraph);
        }

        // Place the boss room at the end of the path
        AddRandRoomToStarterPath(Room.RoomType.Boss, currentPos, starterPos, roomGraph, out currentPos, out roomGraph);

        // STEP 3: Randomly add new normal rooms branching off existing rooms (weighted by genWeight)
        //-----------------------------------------//
        int numAdditionalRooms = Random.Range(2, 5) + roomSizeScaler;
        for (int i = 0; i < numAdditionalRooms; i++)
        {
            AddNormalLeafRoom(roomGraph, out roomGraph, out Vector2Int newRoomPosOut);
        }

        // STEP 4: Replace a random leaf room with a shop, another with a fly.
        //-----------------------------------------//
        Vector2Int shopRoomPos = GetRandomLeaf(roomGraph);
        if (shopRoomPos == Vector2Int.zero)
        {
            AddNormalLeafRoom(roomGraph, out roomGraph, out Vector2Int newRoomPosOut);
            shopRoomPos = newRoomPosOut;
        }
        roomGraph[shopRoomPos.x, shopRoomPos.y].roomType = Room.RoomType.Shop;

        Vector2Int flyRoomPos = GetRandomLeaf(roomGraph);
        if (flyRoomPos == Vector2Int.zero)
        {
            AddNormalLeafRoom(roomGraph, out roomGraph, out Vector2Int newRoomPosOut);
            flyRoomPos = newRoomPosOut;
        }
        roomGraph[flyRoomPos.x, flyRoomPos.y].roomType = Room.RoomType.Fly;

        // STEP 5: Cleanup (make new doors between adjacent rooms, lock leaf doors, etc.)
        //-----------------------------------------//
        RandomlyMakeDoors(roomGraph, out roomGraph);
        RandomlyLockDoors(roomGraph, out roomGraph);

        return roomGraph;
    }


    #endregion


    #region STARTER PATH


    // Adds a room of given type to the graph at a random position stepped away from starter
    // Returns the new currentPos after stepping in random direction
    private static void AddRandRoomToStarterPath(Room.RoomType roomType, Vector2Int currentPos, Vector2Int starterPos, RoomData[,] graph, out Vector2Int currentPosOut, out RoomData[,] graphOut)
    {
        // Get a random direction away from starter
        Vector2Int direction = GetRandDirectionAwayFromStarter(starterPos, currentPos);
        Door.DoorDirection doorDir = direction.x == 1 ? Door.DoorDirection.Right :
                                     direction.x == -1 ? Door.DoorDirection.Left :
                                     direction.y == 1 ? Door.DoorDirection.Up :
                                     Door.DoorDirection.Down;

        // Open door from old room to new room
        graph[currentPos.x, currentPos.y].SetDoor(doorDir, new DoorData
        {
            isImpassable = false,
            isOpen = false,
            isLocked = false,
            direction = doorDir
        });

        // Set up new room
        currentPos += direction;

        // Check bounds to ensure we don't go outside the array
        if (!IsInBounds(currentPos, graph))
        {
            // For now, just return without adding the room
            currentPosOut = currentPos - direction; // Revert to previous position
            graphOut = graph;
            Debug.LogWarning("Attempted to add room out of bounds; skipping addition.");
            return;
        }

        RoomData newRoomData = new RoomData(
            roomType,
            new Vector2Int(currentPos.x, currentPos.y)
        );

        // Open door from new room back to old room
        Door.DoorDirection oppositeDir = direction.x == 1 ? Door.DoorDirection.Left :
                                        direction.x == -1 ? Door.DoorDirection.Right :
                                        direction.y == 1 ? Door.DoorDirection.Down :
                                        Door.DoorDirection.Up;

        newRoomData.SetDoor(oppositeDir, new DoorData
        {
            isImpassable = false,
            isOpen = false,
            isLocked = false,
            direction = oppositeDir
        });

        graph[currentPos.x, currentPos.y] = newRoomData;

        currentPosOut = currentPos;
        graphOut = graph;
    }


    #endregion


    #region ROOM SELECTION


    // Selects a random room from the graph, weighted by each room's genWeight
    // NOTE: Could use some optimization - but, for graphs of ~20x20 rooms, 400 ops per call is okay for now.
    private static Vector2Int SelectRandomRoomByWeight(RoomData[,] graph)
    {
        List<RoomData> allRooms = new List<RoomData>();
        List<float> weights = new List<float>();

        // Gather all rooms and their weights, only including normal rooms
        for (int x = 0; x < graph.GetLength(0); x++)
        {
            for (int y = 0; y < graph.GetLength(1); y++)
            {
                RoomData room = graph[x, y];
                if (room != null)
                {
                    if (room.roomType != Room.RoomType.Normal)
                        continue; // Only include normal rooms for random selection

                    allRooms.Add(room);
                    weights.Add(room.genWeight);
                }
            }
        }

        // Calculate total weight
        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        // Get a random value within the total weight
        float randValue = Random.Range(0, totalWeight);

        // Select a room based on the random value and weights
        float cumulativeWeight = 0f;
        for (int i = 0; i < allRooms.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randValue <= cumulativeWeight)
            {
                return allRooms[i].roomCoordinate;
            }
        }

        Debug.LogError("[RoomGraphGenerator] SelectRandomRoomByWeight: Reached fallback case, this should not happen. Generation will likely be broken.");
        return Vector2Int.zero; // Fallback, should not reach here
    }


    // Gets a random leaf room from the graph (a room with no branching)
    private static Vector2Int GetRandomLeaf(RoomData[,] graph)
    {
        List<RoomData> leafRooms = new List<RoomData>();

        // Gather all leaf rooms
        for (int x = 0; x < graph.GetLength(0); x++)
        {
            for (int y = 0; y < graph.GetLength(1); y++)
            {
                RoomData room = graph[x, y];
                if (room != null && room.isLeaf)
                {
                    leafRooms.Add(room);
                }
            }
        }

        if (leafRooms.Count == 0)
        {
            Debug.LogError("[RoomGraphGenerator] GetRandomLeaf: No leaf rooms found in graph. Generation will likely be broken.");
            return Vector2Int.zero;
        }

        // Select a random leaf room
        int randIndex = Random.Range(0, leafRooms.Count);
        return leafRooms[randIndex].roomCoordinate;
    }


    #endregion


    #region ROOM ADDITION


    // Adds a normal leaf room connected to a randomly selected existing room
    private static void AddNormalLeafRoom(RoomData[,] roomGraph, out RoomData[,] roomGraphOut, out Vector2Int newRoomPosOut)
    {
        bool roomAdded = false;
        int attempts = 0;
        Vector2Int newRoomPos = Vector2Int.zero;

        while (!roomAdded)
        {
            // Select a random room from the existing rooms, weighted by genWeight, and add a new normal room connected to it
            Vector2Int selectedRoomPos = SelectRandomRoomByWeight(roomGraph);
            AddRoomConnectedToCoordinate(Room.RoomType.Normal, selectedRoomPos, roomGraph, out bool success, out roomGraph, out newRoomPos);
            if (success)
            {
                roomAdded = true;
            }
            else
            {
                attempts++;
                if (attempts > 50)
                {
                    // Failed to add a room after several attempts, break to avoid infinite loop
                    Debug.LogError("[RoomGraphGenerator] Failed to add additional room after multiple attempts; stopping further additions. Generation will likely be incomplete.");
                    break;
                }
            }
        }

        roomGraphOut = roomGraph;
        newRoomPosOut = roomAdded ? newRoomPos : Vector2Int.zero;
    }


    // Attempts to add a room of given type connected to the room at given coordinate
    // Returns true if successful, false if no valid position was found
    private static void AddRoomConnectedToCoordinate(Room.RoomType roomType, Vector2Int fromCoord, RoomData[,] graph, out bool successOut, out RoomData[,] graphOut, out Vector2Int newRoomPosOut)
    {
        // Check if fromCoord is valid
        if (!IsInBounds(fromCoord, graph) || graph[fromCoord.x, fromCoord.y] == null)
        {
            Debug.LogWarning("[RoomGraphGenerator] AddRoomConnectedToCoordinate: fromCoord is invalid or has no associated room.");
            successOut = false;
            graphOut = graph;
            newRoomPosOut = Vector2Int.zero;
            return;
        }

        // Get possible directions to add a new room (where there is no existing room)
        List<Vector2Int> possibleDirections = new List<Vector2Int>();
        if (IsInBounds(fromCoord + new Vector2Int(1, 0), graph) && graph[fromCoord.x + 1, fromCoord.y] == null)
            possibleDirections.Add(new Vector2Int(1, 0)); // Right
        if (IsInBounds(fromCoord + new Vector2Int(-1, 0), graph) && graph[fromCoord.x - 1, fromCoord.y] == null)
            possibleDirections.Add(new Vector2Int(-1, 0)); // Left
        if (IsInBounds(fromCoord + new Vector2Int(0, 1), graph) && graph[fromCoord.x, fromCoord.y + 1] == null)
            possibleDirections.Add(new Vector2Int(0, 1)); // Up
        if (IsInBounds(fromCoord + new Vector2Int(0, -1), graph) && graph[fromCoord.x, fromCoord.y - 1] == null)
            possibleDirections.Add(new Vector2Int(0, -1)); // Down

        if (possibleDirections.Count == 0)
        {
            // No valid directions to add a room
            successOut = false;
            graphOut = graph;
            newRoomPosOut = Vector2Int.zero;
            return;
        }

        // Select a random direction to add the new room
        int randIndex = Random.Range(0, possibleDirections.Count);
        Vector2Int direction = possibleDirections[randIndex];
        Vector2Int newRoomPos = fromCoord + direction;

        // Set up new room
        RoomData newRoomData = new RoomData(
            roomType,
            new Vector2Int(newRoomPos.x, newRoomPos.y)
        );

        // Open door from old room to new room
        Door.DoorDirection doorDir = direction.x == 1 ? Door.DoorDirection.Right :
                                     direction.x == -1 ? Door.DoorDirection.Left :
                                     direction.y == 1 ? Door.DoorDirection.Up :
                                     Door.DoorDirection.Down;

        graph[fromCoord.x, fromCoord.y].SetDoor(doorDir, new DoorData
        {
            isImpassable = false,
            isOpen = false,
            isLocked = false,
            direction = doorDir
        });

        // Old room is no longer a leaf, mark it as such
        graph[fromCoord.x, fromCoord.y].isLeaf = false;

        // Open door from new room back to old room
        Door.DoorDirection oppositeDir = direction.x == 1 ? Door.DoorDirection.Left :
                                        direction.x == -1 ? Door.DoorDirection.Right :
                                        direction.y == 1 ? Door.DoorDirection.Down :
                                        Door.DoorDirection.Up;

        newRoomData.SetDoor(oppositeDir, new DoorData
        {
            isImpassable = false,
            isOpen = false,
            isLocked = false,
            direction = oppositeDir
        });

        // Reduce gen weight of all existing rooms by 1.5 (min 1) to encourage spreading out
        for (int x = 0; x < graph.GetLength(0); x++)
        {
            for (int y = 0; y < graph.GetLength(1); y++)
            {
                RoomData room = graph[x, y];
                if (room != null)
                {
                    room.genWeight = Mathf.Max(1f, room.genWeight - 1.5f);
                }
            }
        }

        newRoomData.genWeight = 5f; // Set gen weight very high for this new room, to encourage further branching from it
        newRoomData.isLeaf = true; // New room is a leaf until it has rooms branching from it

        graph[newRoomPos.x, newRoomPos.y] = newRoomData;
        successOut = true;
        graphOut = graph;
        newRoomPosOut = newRoomPos;
    }


    #endregion


    #region CLEANUP


    // Randomly makes some doors in the graph passable (not impassable walls)
    private static void RandomlyMakeDoors(RoomData[,] graph, out RoomData[,] graphOut)
    {
        for (int x = 0; x < graph.GetLength(0); x++)
        {
            for (int y = 0; y < graph.GetLength(1); y++)
            {
                RoomData room = graph[x, y];
                if (room != null && room.roomType == Room.RoomType.Normal)
                {
                    // See if there is a normal room in any direction around us
                    // If there is, randomly make the door to that room passable (35% chance)

                    // Define direction data for all four directions
                    var directions = new[]
                    {
                        new { offset = new Vector2Int(1, 0), doorDir = Door.DoorDirection.Right, oppositeDir = Door.DoorDirection.Left },
                        new { offset = new Vector2Int(-1, 0), doorDir = Door.DoorDirection.Left, oppositeDir = Door.DoorDirection.Right },
                        new { offset = new Vector2Int(0, 1), doorDir = Door.DoorDirection.Up, oppositeDir = Door.DoorDirection.Down },
                        new { offset = new Vector2Int(0, -1), doorDir = Door.DoorDirection.Down, oppositeDir = Door.DoorDirection.Up }
                    };

                    // Check each direction
                    for (int i = 0; i < directions.Length; i++)
                    {
                        var dir = directions[i];
                        Vector2Int adjacentPos = new Vector2Int(x + dir.offset.x, y + dir.offset.y);

                        if (IsInBounds(adjacentPos, graph)
                            && graph[adjacentPos.x, adjacentPos.y] != null
                            && graph[adjacentPos.x, adjacentPos.y].roomType == Room.RoomType.Normal
                            && room.doors[dir.doorDir].isImpassable
                            && Random.value < 0.35f) // 35% chance to make door passable
                        {
                            // Update this room to be passable in the direction
                            room.SetDoor(dir.doorDir, new DoorData
                            {
                                isImpassable = false,
                                isOpen = false,
                                isLocked = false,
                                direction = dir.doorDir
                            });

                            // Update the adjacent room to be passable in the opposite direction
                            graph[adjacentPos.x, adjacentPos.y].SetDoor(dir.oppositeDir, new DoorData
                            {
                                isImpassable = false,
                                isOpen = false,
                                isLocked = false,
                                direction = dir.oppositeDir
                            });

                            // Update this room and adjacent room to no longer be a leaf
                            room.isLeaf = false;
                            graph[adjacentPos.x, adjacentPos.y].isLeaf = false;
                        }
                    }
                }
            }
        }

        graphOut = graph;
    }


    // Randomly lock doors to some leaf rooms
    private static void RandomlyLockDoors(RoomData[,] graph, out RoomData[,] graphOut)
    {
        for (int x = 0; x < graph.GetLength(0); x++)
        {
            for (int y = 0; y < graph.GetLength(1); y++)
            {
                RoomData room = graph[x, y];
                if (room != null && room.roomType != Room.RoomType.Boss && room.isLeaf)
                {
                    // This room is not the boss room, and it is a leaf room. Lock it at 50% chance.

                    if (Random.value < 0.5f) // 50% chance to lock a door in this leaf room
                    {
                        foreach(var doorEntry in room.doors)
                        {
                            DoorData doorData = doorEntry.Value;
                            if (!doorData.isImpassable && !doorData.isLocked)
                            {
                                // Lock this door
                                /* room.SetDoor(doorEntry.Key, new DoorData
                                {
                                    isImpassable = false,
                                    isOpen = false,
                                    isLocked = true,
                                    direction = doorData.direction
                                }); */

                                // Find and lock the corresponding door in the connected room
                                Vector2Int adjacentPos = Vector2Int.zero;
                                Door.DoorDirection oppositeDir = Door.DoorDirection.Up;

                                switch (doorEntry.Key)
                                {
                                    case Door.DoorDirection.Right:
                                        adjacentPos = new Vector2Int(x + 1, y);
                                        oppositeDir = Door.DoorDirection.Left;
                                        break;
                                    case Door.DoorDirection.Left:
                                        adjacentPos = new Vector2Int(x - 1, y);
                                        oppositeDir = Door.DoorDirection.Right;
                                        break;
                                    case Door.DoorDirection.Up:
                                        adjacentPos = new Vector2Int(x, y + 1);
                                        oppositeDir = Door.DoorDirection.Down;
                                        break;
                                    case Door.DoorDirection.Down:
                                        adjacentPos = new Vector2Int(x, y - 1);
                                        oppositeDir = Door.DoorDirection.Up;
                                        break;
                                }

                                // Lock the corresponding door in the adjacent room if it exists
                                if (IsInBounds(adjacentPos, graph) && graph[adjacentPos.x, adjacentPos.y] != null)
                                {
                                    RoomData adjacentRoom = graph[adjacentPos.x, adjacentPos.y];
                                    DoorData adjacentDoorData = adjacentRoom.doors[oppositeDir];

                                    adjacentRoom.SetDoor(oppositeDir, new DoorData
                                    {
                                        isImpassable = false,
                                        isOpen = false,
                                        isLocked = true,
                                        direction = oppositeDir
                                    });
                                }

                                break; // Only one door in a leaf room, so we can quit
                            }
                        }
                    }
                }
            }
        }

        graphOut = graph;
    }


    #endregion


    #region HELPERS


    // Gets a random direction vector that moves away from the starter room based on current position
    // Ensures that the chosen direction does not lead back towards the starter room
    private static Vector2Int GetRandDirectionAwayFromStarter(Vector2Int starterPos, Vector2Int currentPos)
    {
        List<Vector2Int> possibleDirections = new List<Vector2Int>();

        // Determine the direction away from the starter room
        Vector2Int offsetFromStarter = currentPos - starterPos;

        // If we are left of or at the starter's X pos, we can move left
        if (offsetFromStarter.x <= 0)
            possibleDirections.Add(new Vector2Int(-1, 0));
        // If we are right of or at the starter's X pos, we can move right
        if (offsetFromStarter.x >= 0)
            possibleDirections.Add(new Vector2Int(1, 0));
        // If we are below or at the starter's Y pos, we can move down
        if (offsetFromStarter.y <= 0)
            possibleDirections.Add(new Vector2Int(0, -1));
        // If we are above or at the starter's Y pos, we can move up
        if (offsetFromStarter.y >= 0)
            possibleDirections.Add(new Vector2Int(0, 1)); // Up

        // Randomly select one of the possible directions
        int randIndex = Random.Range(0, possibleDirections.Count);
        return possibleDirections[randIndex];
    }


    // Gets whether given position is within bounds of the room graph array
    // Padding is optional, ensures position is at least 'padding' cells away from edges
    private static bool IsInBounds(Vector2Int pos, RoomData[,] graph, int padding = 0)
    {
        return pos.x >= padding && pos.x < (graph.GetLength(0) - padding) &&
               pos.y >= padding && pos.y < (graph.GetLength(1) - padding);
    }


    #endregion


}
