using UnityEngine;

public class Collectable_GoldenFly : GroundCollectable
{

    // Collectable_GoldenFly is a collectable for the player to gain a golden fly.

    public override void OnCollect()
    {
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;
        
        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        InventoryManager.Instance.AddGoldenFlies(1);
        Destroy(gameObject);
    }
}
