using UnityEngine;

public class PlayerHealth : MonoBehaviour
{

    // PlayerHealth manages the player's health


    #region VARIABLES


    public int currentHealth { get; private set; }
    public int maxHealth { get; private set; }


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Start()
    {
        // TODO: Load player health here

        maxHealth = 100;
        currentHealth = maxHealth;
    }


    #endregion


    #region DAMAGE AND HEAL


    // Damages player by given amount; if health drops to 0 or below, player dies
    public void DamagePlayer(int dmgAmount)
    {
        if (currentHealth > 0)
        {
            currentHealth -= dmgAmount;
        }
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            KillPlayer();
        }
    }


    #endregion


    #region DEATH


    // Kills the player and initializes death sequence
    public void KillPlayer()
    {
        currentHealth = 0;
        Debug.Log("Player Died :(");
    }


    #endregion
}
