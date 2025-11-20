using System.Collections.Generic;
using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{

    // InventoryManager manages the player's inventory of collectable items.


    #region VARIABLES


    public static InventoryManager Instance;

    public int lotuses { get; private set; } //save and load variable
    public int woodpeckers { get; private set; } //save and load variable
    
    // Bridge property to get golden flies from GoldenFlyHUD
    public int goldenFlies 
    { 
        get 
        { 
            return GoldenFlyHUD.Instance != null ? GoldenFlyHUD.Instance.goldenFlies : 0; 
        } 
    }

    public List<PowerFlyData> collectedPowerFlies; //save and load variable
    private PowerFlyData[] allPowerFlyData;
    //private HashSet<string> collectedFlyIDs = new HashSet<string>();


    public event Action<int> OnLotusesChanged;
    public event Action<int> OnWoodpeckersChanged;
    public event Action<int> OnPowerFlyCountChanged;
    public event Action OnInventoryChanged;
    [SerializeField] private ItemDefinition lotusDef;
    [SerializeField] private ItemDefinition woodpeckerDef;
    [SerializeField] private ItemDefinition powerFlyDef;
    [SerializeField] private GroundCollectable goldenFlyPrefab;

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

        LoadAllPowerFlyData();

        //Subscribe to save and load actions
        SaveManager.SaveData += SaveLotuses;
        SaveManager.SaveData += SaveWoodpeckers;
        SaveManager.SaveData += SaveCollectedPowerFlies;

        SaveManager.LoadData += LoadLotuses;
        SaveManager.LoadData += LoadWoodpeckers;
        SaveManager.LoadData += LoadCollectedPowerFlies;
    }

    public void OnDestroy()
    {
        //Unsubscribe to save and load actions
        SaveManager.SaveData -= SaveLotuses;
        SaveManager.SaveData -= SaveWoodpeckers;
        SaveManager.SaveData -= SaveCollectedPowerFlies;

        SaveManager.LoadData -= LoadLotuses;
        SaveManager.LoadData -= LoadWoodpeckers;
        SaveManager.LoadData -= LoadCollectedPowerFlies;
    }

    // Loads all power fly data from resources
    private void LoadAllPowerFlyData()
    {
        allPowerFlyData = Resources.LoadAll<PowerFlyData>("");

        //// Organize power flies by rarity tier
        //powerFlyDatasByRarityTier = new Dictionary<PowerFlyData.FlyRarity, List<PowerFlyData>>();
        //for (int rarityTier = 0; rarityTier < 3; rarityTier++)
        //{
        //    PowerFlyData.FlyRarity currentRarity = (PowerFlyData.FlyRarity)rarityTier;
        //    powerFlyDatasByRarityTier[currentRarity] = new List<PowerFlyData>();
        //    foreach (PowerFlyData data in allPowerFlyDatas)
        //    {
        //        if (data.flyRarity == currentRarity)
        //        {
        //            powerFlyDatasByRarityTier[currentRarity].Add(data);
        //        }
        //    }
        //}
    }

    #endregion

    #region INVENTORY SAVE AND LOAD

    private void SaveLotuses()
    {
        SaveManager.SaveForProfile<int>(SaveVariable.Lotus, lotuses);
        Debug.Log("Saved lotus to profile data");
    }

    private void LoadLotuses()
    {
        int amount = lotuses; //get current/old number of lotuses
        try
        {
            lotuses = SaveManager.LoadForProfile<int>(SaveVariable.Lotus);
            Debug.Log($"[InventoryManager] Loaded {lotuses} lotuses from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (0)
            lotuses = 0;
            Debug.Log($"[InventoryManager] No saved lotuses found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[InventoryManager] Failed to load lotuses: {ex.Message}");
            lotuses = 0;
        }

        //Update Displays
        OnLotusesChanged?.Invoke(lotuses);
        amount = lotuses - amount; //Get difference between old and new num of lotuses
        if (lotusDef) AddItem(lotusDef, amount); //Update dsplay with change
        OnInventoryChanged?.Invoke();
    }

    private void SaveWoodpeckers()
    {
        SaveManager.SaveForProfile<int>(SaveVariable.Woodpeckers, woodpeckers);
        Debug.Log("Saved woodpeckers to profile data");
    }

    private void LoadWoodpeckers()
    {
        int amount = woodpeckers; //get current/old number of woodpeckers
        try
        {
            woodpeckers = SaveManager.LoadForProfile<int>(SaveVariable.Woodpeckers);
            Debug.Log($"[InventoryManager] Loaded {woodpeckers} woodpeckers from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (0)
            woodpeckers = 0;
            Debug.Log($"[InventoryManager] No saved woodpeckers found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[InventoryManager] Failed to load woodpeckers: {ex.Message}");
            woodpeckers = 0;
        }

        //Update Displays
        OnWoodpeckersChanged?.Invoke(woodpeckers);
        amount = woodpeckers - amount; //Get difference between old and new num of woodpeckers
        if (woodpeckerDef) AddItem(woodpeckerDef, amount); //Update dsplay with change
        OnInventoryChanged?.Invoke();
    }

    private void SaveCollectedPowerFlies()
    {
        List<string> collectedList = new List<string>();
        foreach(PowerFlyData powerfly in collectedPowerFlies)
        {
            collectedList.Add(powerfly.FlyID);
        }

        SaveManager.SaveForProfile<List<string>>(SaveVariable.CollectedPowerflies, collectedList);
        Debug.Log("Saved collectedpowerflies to profile data");
    }

    //private void LoadCollectedPowerFlies()
    //{
    //    try
    //    {
    //        collectedPowerFlies = SaveManager.LoadForProfile<List<PowerFlyData>>(SaveVariable.CollectedPowerflies);
    //        Debug.Log($"[InventoryManager] Loaded {collectedPowerFlies.Count} power flies from profile {SaveManager.activeProfile}");
    //    }
    //    catch (System.Collections.Generic.KeyNotFoundException)
    //    {
    //        // No saved data yet, use default value (0)
    //        collectedPowerFlies = new List<PowerFlyData>();
    //        Debug.Log($"[InventoryManager] No saved powerflies found, defaulting to 0");
    //    }
    //    catch (System.Exception ex)
    //    {
    //        // Handle other exceptions (e.g., no active profile set)
    //        Debug.LogWarning($"[InventoryManager] Failed to load powerflies: {ex.Message}");
    //        collectedPowerFlies = new List<PowerFlyData>();
    //    }

    //    //collectedPowerFlies.Add(powerFly);
    //    OnPowerFlyCountChanged?.Invoke(collectedPowerFlies.Count);
    //    //if (powerFlyDef) AddItem(powerFlyDef, 1);
    //    OnInventoryChanged?.Invoke();
    //}

    // Loads the list of purchased flies from SaveManager
    private void LoadCollectedPowerFlies()
    {
        List<string> collectedList;
        collectedPowerFlies = new List<PowerFlyData>();
        //HashSet<string> collectedFlyIDs;
        try
        {
            collectedList = SaveManager.LoadForProfile<List<string>>(SaveVariable.CollectedPowerflies);
            //collectedFlyIDs = new HashSet<string>(collectedList);
            Debug.Log($"[InventoryManager] Loaded {collectedList.Count} power flies from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            //collectedFlyIDs = new HashSet<string>();
            collectedList = new List<string>();
            Debug.Log($"[InventoryManager] No saved powerflies found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[InventoryManager] Failed to load powerflies: {ex.Message}");
            collectedList = new List<string>();
        }

        //Add powerflies in the list to collectedpowerflies list
        if (collectedList.Count > 0)
        {
            foreach(string flyID in collectedList)
            {
                //Check which fly it is and add it to collected flies
                foreach(PowerFlyData flyData in allPowerFlyData)
                {
                    if(flyData.FlyID == flyID)
                    {
                        AddPowerFly(flyData);
                        PowerFlyFactory.Instance.MarkPowerFlyAsCollected(flyData);
                        //Apply powerfly's effect
                        foreach (PowerFlyEffect effect in flyData.effects)
                        {
                            effect.ApplyEffect();
                        }
                        break;
                    }
                }
            }
        }
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

    #region GOLDEN FLIES


    // Spews a number of golden flies at a given position.
    public void SpewGoldenFlies(Vector3 position, int amount)
    {
        if (goldenFlyPrefab == null)
        {
            Debug.LogWarning("InventoryManager: goldenFlyPrefab is not assigned!");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            // Instantiate the golden fly
            GameObject flyObject = Instantiate(goldenFlyPrefab.gameObject, position, Quaternion.identity);

            // Calculate random direction and distance (bottom semicircle only: 180° to 360°)
            float angle = UnityEngine.Random.Range(180f, 360f);
            float distance = UnityEngine.Random.Range(3f, 4.5f);
            Vector3 targetPosition = position + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                0f
            );

            // Calculate arc parameters
            float arcHeight = UnityEngine.Random.Range(2f, 4f);
            float duration = UnityEngine.Random.Range(0.6f, 1f);

            // Delay each fly slightly for a cascading effect
            float delay = i * 0.05f;

            // Animate the fly along an arc path
            LeanTween.move(flyObject, targetPosition, duration)
                .setEase(LeanTweenType.easeOutQuad)
                .setDelay(delay);

            // Separate Y animation for the arc effect (gravity)
            Vector3 startPos = flyObject.transform.position;
            LeanTween.value(flyObject, 0f, 1f, duration)
                .setDelay(delay)
                .setOnUpdate((float t) =>
                {
                    if (flyObject != null)
                    {
                        // Parabolic arc: peaks at t=0.5
                        float yOffset = arcHeight * Mathf.Sin(t * Mathf.PI);
                        Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, t);
                        currentPos.y += yOffset;
                        flyObject.transform.position = currentPos;
                    }
                });
        }
    }


    // Adds a number of golden flies to the player's inventory.
    // This is a bridge method that delegates to GoldenFlyHUD.
    public void AddGoldenFlies(int amount)
    {
        if (GoldenFlyHUD.Instance != null)
        {
            GoldenFlyHUD.Instance.AddGoldenFlies(amount);
        }
        else
        {
            Debug.LogWarning("InventoryManager.AddGoldenFlies: GoldenFlyHUD.Instance is null!");
        }
        
        OnInventoryChanged?.Invoke();
    }


    public void RemoveGoldenFlies(int amount)
    {
        AddGoldenFlies(-amount);
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
