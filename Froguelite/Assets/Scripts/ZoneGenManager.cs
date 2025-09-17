using UnityEngine;

public class ZoneGenManager : MonoBehaviour
{
    [SerializeField] private GameObject normalRoom;
    [SerializeField] private GameObject bossRoom;
    [SerializeField] private GameObject shopRoom;

    [SerializeField] private Room[] rooms;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateRooms()
    {
        foreach(Room room in rooms)
        {
            //Instatiate room using position
        }
    }

    private void GenerateBridge(int room1, int room2)
    {
        //Get distance between center of room 1 and 2

        //Orient and expand bridge sprite to connect both centers

        //Ensure bridge layer is under room layer
    }
}

class Room
{
    Vector2 position;
    GameObject roomObj;
}