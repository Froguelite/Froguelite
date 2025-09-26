using UnityEngine;

public class Collectable_Lotus : GroundCollectable
{

    // Collectable_Lotus is a collectable for the player to gain a lotus (coin).

    public override void OnCollect()
    {
        InventoryManager.Instance.AddLotuses(1);
        Destroy(gameObject);
    }
}
