using System.Collections.Generic;
using UnityEngine;

public class DoorManager : MonoBehaviour
{

    // DoorManager handles all doors in the game, with options for showing/hiding all doors


    #region VARIABLES


    public static DoorManager Instance { get; private set; }

    private List<Door> allDoors = new List<Door>();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    // Register a door with the DoorManager
    public void RegisterDoor(Door door)
    {
        if (!allDoors.Contains(door))
        {
            allDoors.Add(door);
        }
    }


    #endregion


    #region DOOR VISIBILITY


    // Open all doors in the game
    public void OpenAllDoors(bool animated = true)
    {
        foreach (Door door in allDoors)
        {
            door.doorData.isOpen = true;
            door.UpdateDoorVisuals(animated);
        }
    }


    // Close all doors in the game
    public void CloseAllDoors(bool animated = true)
    {
        foreach (Door door in allDoors)
        {
            door.doorData.isOpen = false;
            door.UpdateDoorVisuals(animated);
        }
    }


    // Called when the player starts travelling through a door
    public void OnTravelStarted()
    {
        CloseAllDoors(true);
        if (LevelManager.Instance.currentZone == 1)
            AudioManager.Instance.PlaySoundIndefinite(TravelSound.LeafTravel);
        else
            AudioManager.Instance.PlaySoundIndefinite(TravelSound.WaterTravel, 0.2f);
    }


    // Called when the player completes the launch phase of travelling through a door
    // (i.e. they have arrived on the island)
    public void OnTravelLaunchComplete()
    {
        // Notify the new room that the player has entered it
        RoomManager.Instance.GetRoomAtWorldPosition(PlayerMovement.Instance.transform.position)?.OnPlayerEnter();
    }


    // Called when the player finishes travelling through a door
    public void OnTravelEnded()
    {
        AudioManager.Instance.StopIndefiniteSound(TravelSound.LeafTravel);
        AudioManager.Instance.StopIndefiniteSound(TravelSound.WaterTravel);
        AudioManager.Instance.StopIndefiniteSound(TravelSound.BubbleTravel);
        
        // Notify the new room that the player has entered it
        RoomManager.Instance.GetRoomAtWorldPosition(PlayerMovement.Instance.transform.position)?.OnDoorTransitionComplete();
    }


    #endregion


}
