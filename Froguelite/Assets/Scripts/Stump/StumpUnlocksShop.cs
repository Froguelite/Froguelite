using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StumpUnlocksShop : MonoBehaviour
{

    // StumpUnlocksShop manages the shop interface for stump unlocks.


    #region VARIABLES


    [SerializeField] private Transform flySpawnPosition;
    [SerializeField] private Transform capsuleMidpointPosition;
    [SerializeField] private Transform flyOutputPosition;
    [SerializeField] private TMP_Text buyCostText;

    [SerializeField] private int buyCost = 3;

    private List<PowerFly> fliesInShop = new List<PowerFly>();
    private HashSet<string> purchasedFlyIDs = new HashSet<string>();

    // Lever handling
    [SerializeField] private SpriteRenderer leverRenderer;
    [SerializeField] private Sprite leverUpSprite, leverDownSprite;
    private bool leverInCooldown = false;
    [SerializeField] private const float leverResetTime = 4f;


    #endregion


    #region SETUP


    void Awake()
    {
        // Subscribe to SaveManager events
        SaveManager.SaveData += SavePurchasedFlies;
        SaveManager.LoadData += LoadPurchasedFlies;
    }

    void Start()
    {
        if (SaveManager.Instance != null)
        {
            LoadPurchasedFlies();
        }
        else
        {
            Debug.LogWarning("[StumpUnlocksShop] SaveManager instance not found during Awake. Purchased flies will not be loaded.");
        }
        
        SetupShop();
        UpdateBuyCostDisplay();
    }

    void OnDestroy()
    {
        // Unsubscribe from SaveManager events
        SaveManager.SaveData -= SavePurchasedFlies;
        SaveManager.LoadData -= LoadPurchasedFlies;
    }


    public void SetupShop()
    {
        PowerFlyData[] allFlyDatas = PowerFlyFactory.Instance.GetAllPowerFlyDatas();
        for (int i = 0; i < allFlyDatas.Length; i++)
        {
            // Only add flies that haven't been purchased yet
            if (!purchasedFlyIDs.Contains(allFlyDatas[i].FlyID))
            {
                PowerFly newFly = PowerFlyFactory.Instance.SpawnPowerFly(allFlyDatas[i], flySpawnPosition, flySpawnPosition.position, true);
                newFly.SetCanCollect(false);
                fliesInShop.Add(newFly);
            }
        }
    }


    #endregion


    #region SAVE AND LOAD


    private void SavePurchasedFlies()
    {
        // Convert HashSet to List for serialization
        List<string> purchasedList = new List<string>(purchasedFlyIDs);
        SaveManager.SaveForProfile(SaveVariable.PurchasedPowerFlies, purchasedList);
        Debug.Log($"[StumpUnlocksShop] Saved {purchasedList.Count} purchased flies");
    }


    private void LoadPurchasedFlies()
    {
        try
        {
            List<string> purchasedList = SaveManager.LoadForProfile<List<string>>(SaveVariable.PurchasedPowerFlies);
            purchasedFlyIDs = new HashSet<string>(purchasedList);
            Debug.Log($"[StumpUnlocksShop] Loaded {purchasedList.Count} purchased flies");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            purchasedFlyIDs = new HashSet<string>();
            Debug.LogWarning("[StumpUnlocksShop] No saved purchased flies found. Starting fresh.");
        }

        // Automatically add all base set flies to purchased list
        PowerFlyData[] allFlyDatas = PowerFlyFactory.Instance.GetAllPowerFlyDatas();
        int baseFliesAdded = 0;
        foreach (PowerFlyData flyData in allFlyDatas)
        {
            if (flyData.isBaseSetFly && !purchasedFlyIDs.Contains(flyData.FlyID))
            {
                purchasedFlyIDs.Add(flyData.FlyID);
                baseFliesAdded++;
            }
        }

        if (baseFliesAdded > 0)
        {
            Debug.Log($"[StumpUnlocksShop] Added {baseFliesAdded} base set flies to purchased list");
            // Save the updated list
            SaveManager.WriteToFile();
        }
    }


    #endregion


    #region COST MANAGEMENT


    /// <summary>
    /// Sets the cost to buy a fly and updates the UI display.
    /// </summary>
    public void SetBuyCost(int newCost)
    {
        buyCost = newCost;
        UpdateBuyCostDisplay();
    }


    /// <summary>
    /// Updates the buy cost text display.
    /// </summary>
    private void UpdateBuyCostDisplay()
    {
        if (buyCostText != null)
        {
            buyCostText.text = buyCost.ToString();
        }
    }


    #endregion


    #region BUY


    public void TryBuyFly()
    {
        if (leverInCooldown)
            return;

        if (fliesInShop.Count == 0)
            return;

        // Check if player has enough golden flies
        if (GoldenFlyHUD.Instance == null)
        {
            Debug.LogError("[StumpUnlocksShop] GoldenFlyHUD instance not found!");
            return;
        }

        if (GoldenFlyHUD.Instance.goldenFlies < buyCost)
        {
            // Not enough currency - shake the HUD to indicate this
            GoldenFlyHUD.Instance.ShakeDisplay();
            return;
        }

        // Player has enough currency - proceed with purchase
        GoldenFlyHUD.Instance.RemoveGoldenFlies(buyCost);

        PowerFly flyToBuy = fliesInShop[Random.Range(0, fliesInShop.Count)];
        fliesInShop.Remove(flyToBuy);
        
        // Mark this fly as purchased
        if (flyToBuy.powerFlyData != null)
        {
            purchasedFlyIDs.Add(flyToBuy.powerFlyData.FlyID);
            Debug.Log($"[StumpUnlocksShop] Purchased: {flyToBuy.powerFlyData.powerFlyName} (ID: {flyToBuy.powerFlyData.FlyID})");
            
            // Save immediately after purchase
            SaveManager.WriteToFile();
        }
        
        flyToBuy.ManualMoveToPosition(flyOutputPosition.position, capsuleMidpointPosition.position, 2f);
        flyToBuy.SetCanCollect(true);

        StartCoroutine(LeverCooldownCo());
    }
    

    private IEnumerator LeverCooldownCo()
    {
        leverInCooldown = true;
        leverRenderer.sprite = leverDownSprite;

        yield return new WaitForSeconds(leverResetTime);

        leverRenderer.sprite = leverUpSprite;
        leverInCooldown = false;
    }


    #endregion


}
