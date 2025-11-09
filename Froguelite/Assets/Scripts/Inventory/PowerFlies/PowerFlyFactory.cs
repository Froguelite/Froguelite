using System.Collections.Generic;
using UnityEngine;

public class PowerFlyFactory : MonoBehaviour
{

    // PowerFlyFactory spawns Power Flies in the game world.


    #region VARIABLES


    [SerializeField] private PowerFly powerFlyPrefab;

    private PowerFlyData[] allPowerFlyDatas;
    private List<PowerFlyData> collectedPowerFlies = new List<PowerFlyData>();
    private Dictionary<PowerFlyData.FlyRarity, List<PowerFlyData>> powerFlyDatasByRarityTier;

    public static PowerFlyFactory Instance { get; private set; }


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;

        LoadAllPowerFlyData();
    }


    // Loads all power fly data from resources
    private void LoadAllPowerFlyData()
    {
        allPowerFlyDatas = Resources.LoadAll<PowerFlyData>("");

        // Organize power flies by rarity tier
        powerFlyDatasByRarityTier = new Dictionary<PowerFlyData.FlyRarity, List<PowerFlyData>>();
        for (int rarityTier = 0; rarityTier < 3; rarityTier++)
        {
            PowerFlyData.FlyRarity currentRarity = (PowerFlyData.FlyRarity)rarityTier;
            powerFlyDatasByRarityTier[currentRarity] = new List<PowerFlyData>();
            foreach (PowerFlyData data in allPowerFlyDatas)
            {
                if (data.flyRarity == currentRarity)
                {
                    powerFlyDatasByRarityTier[currentRarity].Add(data);
                }
            }
        }
    }


    #endregion


    #region ROLLING


    // Gets all power fly datas
    public PowerFlyData[] GetAllPowerFlyDatas()
    {
        return allPowerFlyDatas;
    }


    // Rolls randomly for a power fly based on spawn chances for a standard power fly room
    public PowerFlyData RollFlyForFlyRoom()
    {
        float commonWeight = 0.55f;
        float uncommonWeight = .3f + (StatsManager.Instance.playerLuck.GetValueAsMultiplier() - 1f) * 0.2f;
        uncommonWeight = Mathf.Min(uncommonWeight, 0f);
        float rareWeight = 0.15f + (StatsManager.Instance.playerLuck.GetValueAsMultiplier() - 1f) * 0.2f;
        rareWeight = Mathf.Min(rareWeight, 0f);

        return RollFlyWithWeights(commonWeight, uncommonWeight, rareWeight);
    }


    // Rolls randomly for a common power fly
    public PowerFlyData RollCommonFly()
    {
        return RollFlyWithWeights(1f, 0f, 0f);
    }


    // Rolls randomly for a power fly based on given weights
    private PowerFlyData RollFlyWithWeights(float commonWeight, float uncommonWeight, float rareWeight)
    {
        float totalWeight = commonWeight + uncommonWeight + rareWeight;
        float roll = Random.Range(0f, totalWeight);

        PowerFlyData.FlyRarity selectedRarity;
        if (roll <= commonWeight)
        {
            selectedRarity = PowerFlyData.FlyRarity.Common;
        }
        else if (roll <= commonWeight + uncommonWeight)
        {
            selectedRarity = PowerFlyData.FlyRarity.Uncommon;
        }
        else
        {
            selectedRarity = PowerFlyData.FlyRarity.Rare;
        }

        List<PowerFlyData> possibleFlies = powerFlyDatasByRarityTier[selectedRarity];
        if (possibleFlies.Count == 0)
        {
            // Try common flies, then uncommon, then rare
            if (powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Common].Count > 0)
            {
                possibleFlies = powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Common];
            }
            else if (powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Uncommon].Count > 0)
            {
                possibleFlies = powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Uncommon];
            }
            else if (powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Rare].Count > 0)
            {
                possibleFlies = powerFlyDatasByRarityTier[PowerFlyData.FlyRarity.Rare];
            }
            else
            {
                Debug.LogError("No power flies available to spawn. Ignoring spawn entirely.");
                return null;
            }
        }

        int randomIndex = Random.Range(0, possibleFlies.Count);
        return possibleFlies[randomIndex];
    }


    #endregion


    #region SPAWNING


    // Spawns a power fly at the given position with the given data
    public PowerFly SpawnPowerFly(PowerFlyData data, Transform spawnParent, Vector3 worldPosition, bool isCapsuleFly = false)
    {
        PowerFly newPowerFly = Instantiate(powerFlyPrefab, spawnParent);
        newPowerFly.transform.position = worldPosition;
        newPowerFly.SetupFly(data, isCapsuleFly);
        return newPowerFly;
    }


    #endregion


    #region COLLECTION MANAGEMENT


    // Marks a power fly as collected
    public void MarkPowerFlyAsCollected(PowerFlyData data)
    {
        if (!collectedPowerFlies.Contains(data))
        {
            collectedPowerFlies.Add(data);

            // If this power fly is only allowed once, remove it from possible spawns
            if (data.onlyOneAllowed)
            {
                powerFlyDatasByRarityTier[data.flyRarity].Remove(data);
            }
        }
    }


    #endregion


}
