using UnityEngine;

public class RoomManager : MonoBehaviour
{

    // RoomManager handles the management and instantiation of different rooms

    #region VARIABLES


    public static RoomManager Instance { get; private set; }

    [SerializeField] private Door doorPrefab;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Initializes the RoomManager, including singleton instance
    public void InitializeRoomManager()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        else
        {
            Instance = this;
        }

    }


    #endregion


    #region HELPERS AND OTHER


    // Returns the door prefab used for instantiating doors in rooms
    public Door GetDoorPrefab()
    {
        return doorPrefab;
    }


    #endregion


}
