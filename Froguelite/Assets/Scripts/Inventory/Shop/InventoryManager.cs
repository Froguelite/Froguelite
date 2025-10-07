using System.Collections.Generic;
using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{

    // InventoryManager manages the player's inventory of collectable items.


    #region VARIABLES


    public static InventoryManager Instance;

    public int lotuses { get; private set; }
    public int woodpeckers { get; private set; } = 0;

    public List<PowerFlyData> collectedPowerFlies = new List<PowerFlyData>();

    public event Action<int> OnLotusesChanged;
    public event Action<int> OnWoodpeckersChanged;
    public event Action<int> OnPowerFlyCountChanged;
    public event Action OnInventoryChanged;
    [SerializeField] private ItemDefinition lotusDef;
    [SerializeField] private ItemDefinition woodpeckerDef;
    [SerializeField] private ItemDefinition powerFlyDef;

    public class Entry
    {
        public string id;
        public string display;
        public Sprite icon;
        public int count;
    }

    // id -> entry
    private readonly Dictionary<string, Entry> _items = new Dictionary<string, Entry>();

    // Fires whenever any item changes 
    public event Action<Entry> OnItemChanged;

    // Read-only view for initial HUD paint on enable
    public IReadOnlyDictionary<string, Entry> Items => _items;

    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake, setup singleton
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
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
        OnLotusesChanged?.Invoke(lotuses);
        if (lotusDef) AddItem(lotusDef, amount);
        OnInventoryChanged?.Invoke();
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
        OnWoodpeckersChanged?.Invoke(woodpeckers);
        if (woodpeckerDef) AddItem(woodpeckerDef, amount);
        OnInventoryChanged?.Invoke();
    }


    // Removes a number of woodpeckers ("keys") from the player's inventory.
    public void RemoveWoodpeckers(int amount)
    {
        AddWoodpeckers(-amount);
    }


    #endregion


    #region POWER FLY MANAGEMENT


    // Adds a collected Power Fly to the player's inventory.
    public void AddPowerFly(PowerFlyData powerFly)
    {
        if (powerFly != null)
        {
            // Allow duplicate PowerFlyData - you can collect multiple of the same type
            collectedPowerFlies.Add(powerFly);
            OnPowerFlyCountChanged?.Invoke(collectedPowerFlies.Count);
            if (powerFlyDef) AddItem(powerFlyDef, 1);
            OnInventoryChanged?.Invoke();
        }
    }


    #endregion

    #region DYNAMIC INVENTORY

    //add/update for HUD-driven inventory rows.

    public void AddItem(ItemDefinition def, int amount = 1)
    {
        Debug.Log($"InventoryManager.AddItem: def={def?.id}, amount={amount}");

        if (def == null || string.IsNullOrEmpty(def.id))
            return;

        if (!_items.TryGetValue(def.id, out var e))
        {
            e = new Entry
            {
                id = def.id,
                display = def.displayName,
                icon = def.icon,
                count = 0
            };
            _items[def.id] = e;
        }

        // Keep latest metadata in case designers update the asset
        if (!string.IsNullOrEmpty(def.displayName)) e.display = def.displayName;
        if (def.icon) e.icon = def.icon;

        e.count = Mathf.Max(0, e.count + amount);

        OnItemChanged?.Invoke(e);  // tell HUD to create/update row for this id
        OnInventoryChanged?.Invoke();
    }
        
    public bool TryPurchase(ItemDefinition def, int price)
    {
        if (!def || string.IsNullOrEmpty(def.id)) return false;
        if (lotuses < price) return false;

        RemoveLotuses(price);
        AddItem(def, 1);
        return true;
    }

    #endregion
}
