using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneGenManager : MonoBehaviour
{
    [SerializeField] private GameObject normalRoom;
    [SerializeField] private GameObject bossRoom;
    [SerializeField] private GameObject shopRoom;

    private Dictionary<RoomType, GameObject> roomsDictionary;

    [SerializeField] private Room[] rooms;

    public Action generateZone;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //DeclareRooms();
        generateZone += GenerateRooms;

        //Fill Dictionary
        roomsDictionary = new Dictionary<RoomType, GameObject>
        {
            { RoomType.normal, normalRoom },
            { RoomType.boss, bossRoom },
            { RoomType.shop, shopRoom }
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void DeclareRooms()
    {
        //Manual Declaration
        rooms = new Room[3];
        rooms[0] = new Room(new Vector2(0,0), 0, RoomType.normal);
        rooms[1] = new Room(new Vector2(0,2), 10, RoomType.boss);
        rooms[2] = new Room(new Vector2(2,0), 20, RoomType.shop);
    }

    public void GenerateZone()
    {
        generateZone?.Invoke();
    }

    private void InstantiateRoom(Room roomToInst)
    {
        //Get Position to instantiate room
        Vector2 pos2D = roomToInst.GetPosition();
        Vector3 pos3D = new Vector3(pos2D.x, pos2D.y, 0f);

        //Get Rotation to instantiate room
        float zRot = roomToInst.GetRotation();
        Quaternion rot = Quaternion.Euler(0, 0, zRot);

        //Get Room to instantiate
        RoomType type = roomToInst.GetTypeName();
        GameObject room = roomsDictionary.ContainsKey(type) ? roomsDictionary[type] : null;

        if(room == null)
        {
            Debug.Log("Room type does not exist in Dictionary");
            return;
        }

        //Instantiate Room
        Instantiate(room, pos3D, rot);
        roomToInst.SetRoomCreated(true);
    }

    private void GenerateRooms()
    {
        foreach (Room room in rooms)
        {
            //Instatiate room using position
            InstantiateRoom(room);
        }
        //Debug.Log("Button click and Action event works");
    }

    private void GenerateBridge(int room1, int room2)
    {
        //Get distance between center of room 1 and 2

        //Orient and expand bridge sprite to connect both centers

        //Ensure bridge layer is under room layer
    }
}

public enum RoomType
{
    normal,
    boss,
    shop
}

[System.Serializable]
class Room
{
    [SerializeField] Vector2 position;
    [SerializeField] float zRotation;
    [SerializeField] RoomType typeName;
    bool roomCreated = false;

    public Room(Vector2 pos, float zRot, RoomType type)
    {
        position = pos;
        zRotation = zRot;
        typeName = type;
    }

    public Vector2 GetPosition()
    {
        return position;
    }

    public RoomType GetTypeName()
    {
        return typeName;
    }

    public float GetRotation()
    {
        return zRotation;
    }

    public bool GetRoomCreated()
    {
        return roomCreated;
    }

    public void SetRoomCreated(bool val)
    {
        roomCreated = val;
    }

}