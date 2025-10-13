using UnityEngine;

[CreateAssetMenu(fileName = "New Drunk Movement Effect", menuName = "Froguelite/Power Fly Effects/Drunk Movement")]
public class PFE_DrunkMovement : PowerFlyEffect
{

    public override void ApplyEffect()
    {
        PlayerMovement.Instance.SetUseDrunkMovement(true);
    }
}
