using UnityEngine;

public class Collectable_Lotus : GroundCollectable
{
    [SerializeField] private ItemDefinition itemDef;

    // Collectable_Lotus is a collectable for the player to gain a lotus (coin).

    public override void OnCollect()
    {
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;
        
        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        InventoryManager.Instance.AddLotuses(1);
        if (itemDef != null)
        {
            InventoryManager.Instance.AddItem(itemDef, 1);
        }
        Destroy(gameObject);
    }
}
