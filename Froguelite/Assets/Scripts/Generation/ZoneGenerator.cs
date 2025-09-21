using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneGenerator : MonoBehaviour
{


    #region VARIABLES


    [SerializeField] private int seed;
    [SerializeField] private Room starterRoom;
    [SerializeField] private Room normalRoom;
    [SerializeField] private Room bossRoom;
    [SerializeField] private Room shopRoom;
    [SerializeField] private Room flyRoom;

    private RoomData[,] roomGraph;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Start()
    {
        UnityEngine.Random.InitState(seed);
        GenerateZone();
    }


    #endregion


    #region GENERATION


    public void GenerateZone()
    {
        roomGraph = RoomGraphGenerator.GetRoomGraph(8);
        LogRoomGraph();
    }

    // Debug helper function to log the room graph in a readable format
    private void LogRoomGraph()
    {
        if (roomGraph == null)
        {
            Debug.Log("Room Graph is null");
            return;
        }

        int width = roomGraph.GetLength(0);
        int height = roomGraph.GetLength(1);
        
        Debug.Log($"=== ROOM GRAPH DEBUG ({width}x{height}) ===");
        
        // Log the grid from top to bottom (reverse Y for readability)
        for (int y = height - 1; y >= 0; y--)
        {
            string row = "";
            for (int x = 0; x < width; x++)
            {
                RoomData room = roomGraph[x, y];
                if (room == null)
                {
                    row += "[ - ] ";
                }
                else
                {
                    // Get room type symbol
                    string roomSymbol = GetRoomTypeSymbol(room.roomType);
                    row += $"[{roomSymbol}] ";
                }
            }
            Debug.Log($"Y{y,2}: {row}");
        }
        
        // Log connections/bridges between rooms
        Debug.Log("=== BRIDGES/CONNECTIONS ===");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                RoomData room = roomGraph[x, y];
                if (room != null && room.doors != null)
                {
                    string connections = "";
                    if (HasOpenConnection(room, Door.DoorDirection.Right)) connections += "→ ";
                    if (HasOpenConnection(room, Door.DoorDirection.Left)) connections += "← ";
                    if (HasOpenConnection(room, Door.DoorDirection.Up)) connections += "↑ ";
                    if (HasOpenConnection(room, Door.DoorDirection.Down)) connections += "↓ ";
                    
                    if (!string.IsNullOrEmpty(connections))
                    {
                        Debug.Log($"Room ({x},{y}) {GetRoomTypeSymbol(room.roomType)}: {connections.Trim()}");
                    }
                }
            }
        }
    }

    // Helper to get a single character symbol for room types
    private string GetRoomTypeSymbol(Room.RoomType roomType)
    {
        return roomType switch
        {
            Room.RoomType.Starter => "S",
            Room.RoomType.Boss => "B",
            Room.RoomType.Normal => "N",
            Room.RoomType.Shop => "$",
            Room.RoomType.Fly => "F",
            _ => "?"
        };
    }

    // Helper to check if a room has an open connection in a given direction
    private bool HasOpenConnection(RoomData room, Door.DoorDirection direction)
    {
        return room.doors != null && 
               room.doors.ContainsKey(direction) && 
               room.doors[direction] != null && 
               !room.doors[direction].isImpassable;
    }


    #endregion
    

}