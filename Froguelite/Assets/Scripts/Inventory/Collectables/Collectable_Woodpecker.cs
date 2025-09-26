using UnityEngine;

public class Collectable_Woodpecker : GroundCollectable
{

    // Collectable_Woodpecker is a collectable for the player to gain a woodpecker (key).

    public override void OnCollect()
    {
        InventoryManager.Instance.AddWoodpeckers(1);
        Destroy(gameObject);
    }
}
