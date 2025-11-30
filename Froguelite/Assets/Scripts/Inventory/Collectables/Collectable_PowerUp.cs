using Unity.VisualScripting;
using UnityEngine;

public class Collectable_PowerUp : GroundCollectable
{
    private int GoldenFlyCost = 5;


    // Update is called once per frame
    void Update()
    {
        
    }

    // Collectable_PoerUp is a collectable for the player to gain a level up.

    public override void OnCollect()
    {
        Debug.Log("Attempting to collect PowerUp");

        if(InventoryManager.Instance.goldenFlies < GoldenFlyCost)
        {
            // Not enough golden flies
            AudioManager.Instance.PlaySound(FlySlotsSound.FlySlotsInvalid);
            return;
        }

        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;

        AudioManager.Instance.PlaySound(CollectibleSound.GoldenCollect);

        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        InventoryManager.Instance.RemoveGoldenFlies(GoldenFlyCost);
        StatsManager.Instance.UpgradePlayerLevel();
        Destroy(gameObject);
    }
}
