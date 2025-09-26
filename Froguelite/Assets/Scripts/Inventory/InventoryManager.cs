using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{

    // InventoryManager manages the player's inventory of collectable items.


    #region VARIABLES


    public static InventoryManager Instance;

    public int lotuses { get; private set; } = 0;
    public int woodpeckers { get; private set; } = 0;

    public List<PowerFly> collectedPowerFlies = new List<PowerFly>();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake, setup singleton
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }


    #endregion


    #region LOTUS AND WOODPECKER MANAGEMENT


    // Adds a number of lotuses ("coins") to the player's inventory.
    public void AddLotuses(int amount)
    {
        lotuses += amount;
        if (lotuses < 0) lotuses = 0;
    }


    // Removes a number of lotuses ("coins") from the player's inventory.
    public void RemoveLotuses(int amount)
    {
        AddLotuses(-amount);
    }


    // Adds a number of woodpeckers ("keys") to the player's inventory.
    public void AddWoodpeckers(int amount)
    {
        woodpeckers += amount;
        if (woodpeckers < 0) woodpeckers = 0;
    }


    // Removes a number of woodpeckers ("keys") from the player's inventory.
    public void RemoveWoodpeckers(int amount)
    {
        AddWoodpeckers(-amount);
    }


    #endregion


    #region POWER FLY MANAGEMENT


    // Adds a collected Power Fly to the player's inventory.
    public void AddPowerFly(PowerFly powerFly)
    {
        if (powerFly != null && !collectedPowerFlies.Contains(powerFly))
        {
            collectedPowerFlies.Add(powerFly);
        }
    }


    #endregion


}
