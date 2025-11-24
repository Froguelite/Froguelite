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
    private int HealthLevel = 6;
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
            HealthLevel = 20;
            SetStatsToGodmode();
            Debug.Log("[StatsManager] In God Mode");
        }
        else
        {
            //SetStatsToDefault();
            HealthLevel = 6;
            SetStatsToDefault(); //For now
            //SaveManager.SaveData += SaveHealth;
            //SaveManager.LoadData += LoadHealth; //Load funtion will call set stats
        }

        SaveManager.SaveData += SaveHealth;
        SaveManager.LoadData += LoadHealth; //Load funtion will call set stats

        //Subscribe to reset player on win or death
        GameManager.ResetPlayerState += SetHealthToDefault;
        GameManager.ResetPlayerState += SetStatsToDefault;
    }

    private void OnDestroy()
    {
        //if (!godmode)
        //{
        //    SaveManager.SaveData -= SaveHealth;
        //    SaveManager.LoadData -= LoadHealth;
        //}

        SaveManager.SaveData -= SaveHealth;
        SaveManager.LoadData -= LoadHealth;

        //Unsubscribe to reset player on win or death
        GameManager.ResetPlayerState -= SetHealthToDefault;
        GameManager.ResetPlayerState -= SetStatsToDefault;
    }


    // Sets all stats to their default values
    public void SetStatsToDefault()
    {
        playerLevel = 1;

        if (godmode) {
            SetStatsToGodmode();
            Debug.Log("[StatsManager] In God Mode");
            return;
        }

        ////Edited value to be set from a number to a variable
        //if (defaultMaxHealth == -1 || defaultCurrentHealth == -1)
        //{
        //    Debug.LogError("[StatsManager] has not loaded player max and current health before setting player stats");
        //}
        //playerHealth.SetMaxHealth(defaultMaxHealth, false);
        //playerHealth.SetCurrentHealth(defaultCurrentHealth, false);

        playerDamage = new Stat(5f, 1f);
        playerSpeed = new Stat(5f, 1f);
        playerRange = new Stat(5f, 1f);
        playerRate = new Stat(5f, 1f);
        playerKnockback = new Stat(5f, 1f);
        playerLuck = new Stat(5f, 1f);
    }

    public void SetHealthToDefault()
    {
        //Edited value to be set from a number to a variable
        //if (defaultMaxHealth == -1 || defaultCurrentHealth == -1)
        //{
        //    Debug.LogError("[StatsManager] has not loaded player max and current health before setting player stats");
        //}
        //playerHealth.SetMaxHealth(defaultMaxHealth, false);
        //playerHealth.SetCurrentHealth(defaultCurrentHealth, false);
        playerHealth.SetMaxHealth(HealthLevel, true);
        playerHealth.SetCurrentHealth(HealthLevel, true);
        Debug.Log($"[StatManager] Health set to default. Current: {HealthLevel}, Max: {HealthLevel}");
    }

    // Sets all stats to godmode values
    public void SetStatsToGodmode()
    {
        playerLevel = 1;

        //playerHealth.SetMaxHealth(20, false);
        //playerHealth.SetCurrentHealth(20, false);

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
            // No saved data yet, use default value (HealthLevel)
            defaultCurrentHealth = HealthLevel;
            Debug.Log($"[StatsManager] No saved current health found, defaulting to {HealthLevel} (full health)");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[StatsManager] Failed to load current health: {ex.Message}");
            defaultCurrentHealth = HealthLevel;
        }

        //Then load player's max health
        try
        {
            defaultMaxHealth = SaveManager.LoadForProfile<int>(SaveVariable.MaxHealth);
            Debug.Log($"[StatsManager] Loaded {defaultMaxHealth} player max health from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (HealthLevel)
            defaultMaxHealth = HealthLevel;
            Debug.Log($"[StatsManager] No saved max health found, defaulting to {HealthLevel}");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[StatsManager] Failed to load max health: {ex.Message}");
            defaultMaxHealth = HealthLevel;
        }

        //Update player health
        //SetHealthToDefault();
        playerHealth.SetMaxHealth(defaultMaxHealth, false);
        playerHealth.SetCurrentHealth(defaultCurrentHealth, false);
    }

    #endregion
}
