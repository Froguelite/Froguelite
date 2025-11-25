using UnityEngine;

[CreateAssetMenu(fileName = "New Jitters Effect", menuName = "Froguelite/Power Fly Effects/Jitters")]
public class PFE_Jitters : PowerFlyEffect
{

    public override void ApplyEffect()
    {
        PlayerMovement.Instance.SetUseJitterAnimation(true);
    }
}
