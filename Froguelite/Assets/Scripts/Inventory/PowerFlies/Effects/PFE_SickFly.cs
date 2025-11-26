using UnityEngine;

[CreateAssetMenu(fileName = "New Sick Fly Effect", menuName = "Froguelite/Power Fly Effects/Sick Fly")]
public class PFE_SickFly : PowerFlyEffect
{

    public override void ApplyEffect()
    {
        if (PlayerAttack.Instance != null)
        {
            PlayerAttack.Instance.AddTongueTag("sickFly");
            StatsManager.Instance.playerSpeed.AddToBaseValue(-0.75f);
        }
        else
        {
            Debug.LogWarning("[PFE_SickFly] PlayerAttack.Instance is null. Cannot add sickFly tag.");
        }
    }
}

