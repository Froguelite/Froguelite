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

    public Stat playerDamage { get; private set; }
    public Stat playerSpeed { get; private set; }
    public Stat playerRange { get; private set; }
    public Stat playerRate { get; private set; }
    public Stat playerKnockback { get; private set; }
    public Stat playerLuck { get; private set; }


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
        SetStatsToDefault();
    }


    // Sets all stats to their default values
    public void SetStatsToDefault()
    {
        playerLevel = 1;

        playerHealth.SetMaxHealth(6, false);
        playerHealth.SetCurrentHealth(6, false);

        playerDamage = new Stat(5f, 1f);
        playerSpeed = new Stat(5f, 1f);
        playerRange = new Stat(5f, 1f);
        playerRate = new Stat(5f, 1f);
        playerKnockback = new Stat(5f, 1f);
        playerLuck = new Stat(5f, 1f);
    }


    #endregion


}
