using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Booster Effect", menuName = "Froguelite/Power Fly Effects/Stat Booster")]
public class PFE_StatBooster : PowerFlyEffect
{

    public enum StatType { Health, Damage, Speed, Range, Rate, Luck }
    public enum BoostType { Base, Multiplier }

    [System.Serializable]
    public class StatBoost
    {
        public StatType statType;
        public BoostType boostType;
        public float boostAmount;
    }

    public StatBoost[] statBoosts;

    public override void ApplyEffect()
    {
        foreach (var statBoost in statBoosts)
        {
            StatsManager.Stat statToBoost = null;

            switch (statBoost.statType)
            {
                case StatType.Health:
                    int newMaxHealth = Mathf.RoundToInt(StatsManager.Instance.playerHealth.maxHealth + statBoost.boostAmount);
                    StatsManager.Instance.playerHealth.SetMaxHealth(newMaxHealth, true);
                    Debug.Log($"Applied Stat Booster: {statBoost.statType} had its max increased by {statBoost.boostAmount}");
                    continue;
                case StatType.Damage:
                    statToBoost = StatsManager.Instance.playerDamage;
                    break;
                case StatType.Speed:
                    statToBoost = StatsManager.Instance.playerSpeed;
                    break;
                case StatType.Range:
                    statToBoost = StatsManager.Instance.playerRange;
                    break;
                case StatType.Rate:
                    statToBoost = StatsManager.Instance.playerRate;
                    break;
                case StatType.Luck:
                    statToBoost = StatsManager.Instance.playerLuck;
                    break;
                default:
                    Debug.LogWarning("PFE_StatBooster: Unknown stat type" + statBoost.statType + "! Ignoring.");
                    continue;
            }

            if (statBoost.boostType == BoostType.Base)
                statToBoost.AddToBaseValue(statBoost.boostAmount);
            else
                statToBoost.AddToMultiplier(statBoost.boostAmount);

            Debug.Log($"Applied Stat Booster: {statBoost.statType} had its {statBoost.boostType} increased by {statBoost.boostAmount}");
        }
    }
}
