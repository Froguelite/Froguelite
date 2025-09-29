using UnityEngine;

public class Collectable_Lotus : GroundCollectable
{
    [SerializeField] private ItemDefinition itemDef;

    // Collectable_Lotus is a collectable for the player to gain a lotus (coin).

    public override void OnCollect()
    {
        if (itemDef != null)
        {
            InventoryManager.Instance.AddItem(itemDef, 1);
        }
        Destroy(gameObject);
    }
}
