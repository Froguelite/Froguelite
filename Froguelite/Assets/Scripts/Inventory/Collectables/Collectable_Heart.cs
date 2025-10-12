using Unity.VisualScripting;
using UnityEngine;

public class Collectable_Heart : GroundCollectable
{

    // Collectable_Heart is a collectable for the player to regain health.


    #region VARIABLES


    [SerializeField] private SpriteRenderer displayImage;

    private Vector3 baseScale;


    #endregion


    #region SETUP AND COLLECTION


    protected override void Start()
    {
        base.Start();

        baseScale = displayImage.transform.localScale;

        // Subscribe to health change event to trigger beating animation
        if (HealthBeatEventHandler.Instance != null)
        {
            HealthBeatEventHandler.Instance.smallBeatEvent1.AddListener(PulseSmallBeat);
        }
    }


    // On collect, heal the player
    public override void OnCollect()
    {
        // Prevent duplicate collection
        if (hasBeenCollected) return;
        hasBeenCollected = true;
        
        // Immediately disable collider and hide visual
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        StatsManager.Instance.playerHealth.HealPlayer(2);
        Destroy(gameObject);
    }


    #endregion


    #region BEATING ANIMATION


    // Pulses a single small beat
    // Should be called automatically via event callbacks
    //-------------------------------------//
    public void PulseSmallBeat()
    //-------------------------------------//
    {
        LeanTween.value(displayImage.gameObject, baseScale * HealthBeatEventHandler.smallBeatScale, baseScale, HealthBeatEventHandler.smallBeatDuration)
        .setEaseInQuad()
        .setOnUpdate((Vector3 val) =>
        {
            displayImage.transform.localScale = val;
        });

    } // END PulseSmallBeat


    #endregion


}
