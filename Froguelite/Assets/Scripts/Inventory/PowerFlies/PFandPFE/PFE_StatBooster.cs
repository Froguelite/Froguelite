using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Booster Effect", menuName = "Froguelite/Power Fly Effects/Stat Booster")]
public class PFE_StatBooster : PowerFlyEffect
{

    public enum StatType { Health, Speed, Strength }
    public StatType statToBoost;
    public float boostAmount;

    public override void ApplyEffect()
    {
        Debug.Log($"Applying Stat Booster Effect: {statToBoost} +{boostAmount}");
        // TODO
    }
}
