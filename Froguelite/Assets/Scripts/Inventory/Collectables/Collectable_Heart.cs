using Unity.VisualScripting;
using UnityEngine;

public class Collectable_Heart : GroundCollectable
{

    // Collectable_Heart is a collectable for the player to regain health.


    #region VARIABLES


    [SerializeField] private SpriteRenderer displayImage;


    #endregion


    #region SETUP AND COLLECTION


    // On collect, heal the player
    public override void OnCollect()
    {
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;

        AudioManager.Instance.PlaySound(CollectibleSound.HeartCollect);
        
        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        StatsManager.Instance.playerHealth.HealPlayer(2);
        Destroy(gameObject);
    }


    #endregion


}
