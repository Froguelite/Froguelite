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
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(stats.maxHealth);
        }

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
        //I included this function incase we want to implement damage reduction
        int effectiveDamage = damage; // int effectiveDamage = Mathf.Max(damage - stats.damageReduction, 0)

        //Depletes current health
        currentHealth -= effectiveDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{stats.bossName} took {effectiveDamage} damage. Remaining HP = {currentHealth}");

        //Updates Health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        //If boss health reaches zero, trigger death logic
        if (currentHealth <= 0)
        {
            Die();
        }
    } // END TakeDamage

    //Die logic
    //-----------------------------------//
    private void Die()
    //-----------------------------------//
    {
        Debug.Log($"{stats.bossName} has been defeated.");

        if (healthBar != null )
        {
            healthBar.HideHealthBar();
        }
        if (bossController != null)
        {
            bossController.Death();
        }
        //Implement animations, loot drops, or cutscene transitions here

    } // END Die
    #endregion
}
