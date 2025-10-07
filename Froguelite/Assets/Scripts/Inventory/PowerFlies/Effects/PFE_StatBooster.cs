using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Booster Effect", menuName = "Froguelite/Power Fly Effects/Stat Booster")]
public class PFE_StatBooster : PowerFlyEffect
{

    public enum StatType { Health, Damage, Speed, Range, Rate }
    public StatType statToBoost;
    public float boostAmount;

    public override void ApplyEffect()
    {
        switch (statToBoost)
        {
            case StatType.Health:
                int newMaxHealth = Mathf.RoundToInt(StatsManager.Instance.playerHealth.maxHealth + boostAmount);
                StatsManager.Instance.playerHealth.SetMaxHealth(newMaxHealth, true);
                break;
            case StatType.Damage:
                StatsManager.Instance.playerDamage.baseValue += boostAmount;
                break;
            case StatType.Speed:
                StatsManager.Instance.playerSpeed.baseValue += boostAmount;
                break;
            case StatType.Range:
                StatsManager.Instance.playerRange.baseValue += boostAmount;
                break;
            case StatType.Rate:
                StatsManager.Instance.playerRate.baseValue += boostAmount;
                break;
            default:
                Debug.LogWarning("PFE_StatBooster: Unknown stat type!");
                break;
        }

        Debug.Log($"Applied Stat Booster: Base {statToBoost} increased by {boostAmount}");
    }
}
