using UnityEngine;

public class Collectable_Woodpecker : GroundCollectable
{
    [SerializeField] private ItemDefinition item; 
    // Collectable_Woodpecker is a collectable for the player to gain a woodpecker (key).

    public override void OnCollect()
    {
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;
        
        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        InventoryManager.Instance.AddWoodpeckers(1);
        if (item) InventoryManager.Instance.AddItem(item, 1); 
        Destroy(gameObject);
    }
}
