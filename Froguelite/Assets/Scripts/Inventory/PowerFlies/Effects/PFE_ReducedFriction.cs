using UnityEngine;

[CreateAssetMenu(fileName = "New Reduced Friction Effect", menuName = "Froguelite/Power Fly Effects/Reduced Friction")]
public class PFE_ReducedFriction : PowerFlyEffect
{

    public override void ApplyEffect()
    {
        PlayerMovement.Instance.SetReducedFriction(true);
    }
}


