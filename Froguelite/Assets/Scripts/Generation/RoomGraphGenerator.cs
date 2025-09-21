using System.Collections.Generic;
using UnityEngine;

public static class RoomGraphGenerator
{

    // RoomGraphGenerator defines the logic for generating a graph of rooms within a zone


    #region GENERATION


    public static RoomData[,] GetRoomGraph(int maxDistFromStarter)
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
        int pathLength = Random.Range(4, 7);
        for (int i = 0; i < pathLength; i++)
        {
            // Place a normal room in a random direction away from the starter room
            AddRandRoomToStarterPath(Room.RoomType.Normal, currentPos, starterPos, roomGraph, out currentPos, out roomGraph);
        }

        // Place the boss room at the end of the path
        AddRandRoomToStarterPath(Room.RoomType.Boss, currentPos, starterPos, roomGraph, out currentPos, out roomGraph);

        return roomGraph;
    }


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
