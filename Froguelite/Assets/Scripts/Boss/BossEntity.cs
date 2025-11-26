using UnityEngine;
using System.Collections;

public class BossEntity : MonoBehaviour
{
    #region VARIABLES

    //reference to bossStats script
    [SerializeField] private BossStats stats;

    //reference to HealthBar script
    [SerializeField] private HealthBar healthBar;

    [SerializeField] private BossController bossController;
    [SerializeField] private SpriteRenderer bossRenderer;
    [SerializeField] private SpriteRenderer shadowSprite;
    [SerializeField] private Color flashColor = Color.red; // Color to flash when hit
    private Color originalColor; // Store the original sprite color
    [SerializeField] private float flashDuration = 0.2f; // How long

    //Tracks current boss health
    private int currentHealth;
    #endregion

    #region SETUP
    //Called when Boss object is first called
    //-----------------------------------//
    void Start()
    //-----------------------------------//
    {

        InitializeBoss();

        if (bossController == null)
        {
            bossController = GetComponent<BossController>();
        }
        //Sets health bar with max health
        healthBar.SetMaxHealth(stats.maxHealth);
        originalColor = bossRenderer.color;

        StatsManager.Instance.playerHealth.onPlayerDie.AddListener(OnPlayerDie);

    } // END Start

    //Initiliazes boss health and logs name and starting hp
    //-----------------------------------//
    public void InitializeBoss()
    //-----------------------------------//
    {
        currentHealth = stats.maxHealth;
        Debug.Log($"{stats.bossName} initialized with {currentHealth} HP.");

    } // END InitializeBoss

    #endregion

    #region DAMAGE
    // Function to implement damage to boss
    //-----------------------------------//
    public void TakeDamage(int damage)
    //-----------------------------------//
    {
        if (currentHealth <= 0)
        {
            // Boss is already dead, ignore further damage
            return;
        }

        AudioManager.Instance.PlaySound(BossSound.BossHit);

        //I included this function incase we want to implement damage reduction
        int effectiveDamage = damage; // int effectiveDamage = Mathf.Max(damage - stats.damageReduction, 0)

        //Depletes current health
        currentHealth -= effectiveDamage;
        currentHealth = Mathf.Max(currentHealth, 0);
        FlashSprite();

        Debug.Log($"{stats.bossName} took {effectiveDamage} damage. Remaining HP = {currentHealth}");

        //Updates Health bar
        healthBar.SetHealth(currentHealth);

        //If boss health reaches zero, trigger death logic
        if (currentHealth <= 0)
        {
            Die();
        }
    } // END TakeDamage

    // Flash the sprite red when taking damage
    private void FlashSprite()
    {
        LeanTween.cancel(bossRenderer.gameObject);
        bossRenderer.color = flashColor;
        LeanTween.value(bossRenderer.gameObject, flashColor, originalColor, flashDuration).setOnUpdate((Color newColor) =>
        {
            bossRenderer.color = newColor;
        });
    }

    //Die logic
    //-----------------------------------//
    private void Die()
    //-----------------------------------//
    {
        Debug.Log($"{stats.bossName} has been defeated.");

        healthBar.HideHealthBar();

        // Fade out shadow sprite if it exists
        if (shadowSprite != null)
        {
            LeanTween.alpha(shadowSprite.gameObject, 0f, 2f);
        }

        // Disable all colliders on the boss
        DisableAllColliders();

        if (bossController != null)
        {
            bossController.Death();
        }
        //Implement animations, loot drops, or cutscene transitions here

        InventoryManager.Instance.SpewGoldenFlies(transform.position, Random.Range(3, 6));

    } // END Die

    // Disable all colliders on this GameObject and its children
    //-----------------------------------//
    private void DisableAllColliders()
    //-----------------------------------//
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        Debug.Log($"Disabled {colliders.Length} collider(s) on {stats.bossName}.");
    } // END DisableAllColliders

    public void OnPlayerDie()
    {
        healthBar.HideHealthBar();
    }
    #endregion
}
