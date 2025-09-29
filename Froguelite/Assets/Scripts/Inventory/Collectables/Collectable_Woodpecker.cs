using UnityEngine;

public class Collectable_Woodpecker : GroundCollectable
{
    [SerializeField] private ItemDefinition item; 
    // Collectable_Woodpecker is a collectable for the player to gain a woodpecker (key).

    public override void OnCollect()
    {
        InventoryManager.Instance.AddWoodpeckers(1);
        if (item) InventoryManager.Instance.AddItem(item, 1); 
        Destroy(gameObject);
    }
}
