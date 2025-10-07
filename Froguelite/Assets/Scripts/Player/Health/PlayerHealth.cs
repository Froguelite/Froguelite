using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{

    // PlayerHealth manages the player's health


    #region VARIABLES


    public int currentHealth { get; private set; }
    public int maxHealth { get; private set; }

    public bool overrideHealAnims = false;
    public bool needsBigBeat = false;

    public UnityEvent onHealthChanged; // Event triggered when health changes (damage or heal)
    public UnityEvent onHealthDamaged; // Event triggered when the player takes damage
    public UnityEvent onHealthHealed;  // Event triggered when the player is healed


    #endregion


    #region MANUAL SETTING


    public void SetMaxHealth(int newMaxHealth, bool healDifference)
    {
        if (newMaxHealth > maxHealth)
        {
            needsBigBeat = true;
        }

        if (healDifference && newMaxHealth > maxHealth)
        {
            int healthDiff = newMaxHealth - maxHealth;
            currentHealth += healthDiff;
        }

        maxHealth = newMaxHealth;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        overrideHealAnims = true;
        StartCoroutine(ResetOverrideHealAnims());
        onHealthChanged.Invoke();
    }


    public void SetCurrentHealth(int newCurrentHealth, bool animateTransition)
    {
        currentHealth = Mathf.Clamp(newCurrentHealth, 0, maxHealth);
        if (!animateTransition)
        {
            overrideHealAnims = true;
            StartCoroutine(ResetOverrideHealAnims());
        }
        onHealthChanged.Invoke();
    }


    private System.Collections.IEnumerator ResetOverrideHealAnims()
    {
        yield return new WaitForEndOfFrame();
        overrideHealAnims = false;
        needsBigBeat = false;
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
            onHealthDamaged.Invoke();
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
            onHealthHealed.Invoke();
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
