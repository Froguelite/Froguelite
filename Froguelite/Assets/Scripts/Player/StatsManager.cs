using UnityEngine;

public class StatsManager : MonoBehaviour
{

    // StatsManager holds and manages all player stats


    #region VARIABLES


    public static StatsManager Instance;

    [System.Serializable]
    public class Stat
    {
        public float baseValue;
        public float multiplier;

        public Stat(float baseValue, float multiplier)
        {
            this.baseValue = baseValue;
            this.multiplier = multiplier;
        }

        public void AddToBaseValue(float amount)
        {
            baseValue += amount;
            if (baseValue <= 1.5f)
                baseValue = 1.5f;
        }

        public void AddToMultiplier(float amount)
        {
            multiplier += amount;
            if (multiplier < 0.3f)
                multiplier = 0.3f;
        }

        // Gets the actual value of this stat, factoring in level scaling
        public float GetValue()
        {
            return baseValue * multiplier * Mathf.Pow(1.2f, StatsManager.Instance.playerLevel);
        }

        // Gets this stat value assuming a base value of 5 (for normalization purposes)
        public float GetValueAsMultiplier()
        {
            return GetValue() / 5f;
        }
    }

    public int playerLevel { get; private set; } = 1;

    public PlayerHealth playerHealth;
    private int defaultCurrentHealth = -1;
    private int defaultMaxHealth = -1;

    public Stat playerDamage { get; private set; }
    public Stat playerSpeed { get; private set; }
    public Stat playerRange { get; private set; }
    public Stat playerRate { get; private set; }
    public Stat playerKnockback { get; private set; }
    public Stat playerLuck { get; private set; }

    [SerializeField] private bool godmode = false;


    #endregion


    #region SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (godmode)
        {
            SetStatsToGodmode();
        }
        else
        {
            SaveManager.SaveData += SaveHealth;
            SaveManager.LoadData += LoadHealth; //Load funtion will call set stats
        }
    }

    private void OnDestroy()
    {
        if (!godmode)
        {
            SaveManager.SaveData -= SaveHealth;
            SaveManager.LoadData -= LoadHealth;
        }
    }


    // Sets all stats to their default values
    public void SetStatsToDefault()
    {
        playerLevel = 1;

        //Edited value to be set from a number to a variable
        if(defaultMaxHealth == -1 || defaultCurrentHealth == -1)
        {
            Debug.LogError("[StatsManager] has not loaded player max and current health before setting player stats");
        }
        playerHealth.SetMaxHealth(defaultMaxHealth, false);
        playerHealth.SetCurrentHealth(defaultCurrentHealth, false);

        playerDamage = new Stat(5f, 1f);
        playerSpeed = new Stat(5f, 1f);
        playerRange = new Stat(5f, 1f);
        playerRate = new Stat(5f, 1f);
        playerKnockback = new Stat(5f, 1f);
        playerLuck = new Stat(5f, 1f);
    }


    // Sets all stats to godmode values
    public void SetStatsToGodmode()
    {
        playerLevel = 1;

        playerHealth.SetMaxHealth(20, false);
        playerHealth.SetCurrentHealth(20, false);

        playerDamage = new Stat(100f, 1f);
        playerSpeed = new Stat(10f, 1f);
        playerRange = new Stat(8f, 1f);
        playerRate = new Stat(15f, 1f);
        playerKnockback = new Stat(5f, 1f);
        playerLuck = new Stat(5f, 1f);
    }


    #endregion

    #region SAVE AND LOAD HEALTH
    //Get values from playerHealth script assuming it will be the most up to date
    private void SaveHealth()
    {
        int currentHealth = playerHealth.currentHealth;
        SaveManager.SaveForProfile<int>(SaveVariable.CurrentHealth, currentHealth);
        int maxHealth = playerHealth.maxHealth;
        SaveManager.SaveForProfile<int>(SaveVariable.MaxHealth, maxHealth);
    }

    private void LoadHealth()
    {
        //First load player's current health
        try
        {
            defaultCurrentHealth = SaveManager.LoadForProfile<int>(SaveVariable.CurrentHealth);
            Debug.Log($"[StatsManager] Loaded {defaultCurrentHealth} player current health from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (6)
            defaultCurrentHealth = 6;
            Debug.Log($"[StatsManager] No saved current health found, defaulting to 6 (full health)");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[StatsManager] Failed to load current health: {ex.Message}");
            defaultCurrentHealth = 6;
        }

        //Then load player's max health
        try
        {
            defaultMaxHealth = SaveManager.LoadForProfile<int>(SaveVariable.MaxHealth);
            Debug.Log($"[StatsManager] Loaded {defaultMaxHealth} player max health from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (6)
            defaultMaxHealth = 6;
            Debug.Log($"[StatsManager] No saved max health found, defaulting to 6");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[StatsManager] Failed to load max health: {ex.Message}");
            defaultMaxHealth = 6;
        }

        //Update player stats
        SetStatsToDefault();
    }

    #endregion
}
