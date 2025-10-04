using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{

    // PlayerHealth manages the player's health


    #region VARIABLES


    public int currentHealth { get; private set; }
    public int maxHealth { get; private set; }

    public UnityEvent onHealthChanged; // Event triggered when health changes


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    private void Start()
    {
        // TODO: Load player health here

        maxHealth = 6;
        currentHealth = maxHealth;

        onHealthChanged.Invoke();
    }


    #endregion


    #region DAMAGE AND HEAL


    // Damages player by given amount; if health drops to 0 or below, player dies
    public void DamagePlayer(int dmgAmount)
    {
        if (currentHealth > 0)
        {
            currentHealth -= dmgAmount;
            if (currentHealth < 0)
            {
                currentHealth = 0;
            }

            onHealthChanged.Invoke();
        }

        if (currentHealth <= 0)
            KillPlayer();
    }


    // Heals player by given amount; if health exceeds max, it caps at max
    public void HealPlayer(int healAmount)
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += healAmount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            onHealthChanged.Invoke();
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
