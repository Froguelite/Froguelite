using UnityEngine;

[CreateAssetMenu(fileName = "New Backwards Tongue Effect", menuName = "Froguelite/Power Fly Effects/Backwards Tongue")]
public class PFE_BackwardsTongue : PowerFlyEffect
{

    public override void ApplyEffect()
    {
        if (PlayerAttack.Instance != null)
        {
            PlayerAttack.Instance.AddTongueTag("backwardsTongue");
        }
        else
        {
            Debug.LogWarning("[PFE_BackwardsTongue] PlayerAttack.Instance is null. Cannot add backwardsTongue tag.");
        }
    }
}

