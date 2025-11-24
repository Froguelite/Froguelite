using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{

    // PlayerHealth manages the player's health


    #region VARIABLES


    [SerializeField] public int currentHealth;// { get; private set; }
    [SerializeField] public int maxHealth;// { get; private set; }

    public bool overrideHealAnims = false;
    public bool needsBigBeat = false;

    public UnityEvent onHealthChanged = new UnityEvent(); // Event triggered when health changes (damage or heal)
    public UnityEvent onHealthDamaged = new UnityEvent(); // Event triggered when the player takes damage
    public UnityEvent onHealthHealed = new UnityEvent();  // Event triggered when the player is healed
    public UnityEvent onPlayerDie = new UnityEvent(); // Event triggered when the player dies

    private float timeSinceLastDamage = 0f;
    private const float damageCooldown = 0.3f; // Minimum time between damage instances

    private bool playerDied = false;
    #endregion


    #region MONOBEHAVIOUR


    // Update
    void Update()
    {
        timeSinceLastDamage += Time.deltaTime;
    }


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

        Debug.Log($"[PlayerHealth] SetMaxHealth called, current health: {currentHealth}/{maxHealth}");
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
        Debug.Log($"[PlayerHealth] SetCurrentHealth called, current health: {currentHealth}/{maxHealth}");
    }

    public void SetPlayerAlive()
    {
        playerDied = false;
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
        Debug.Log($"[PlayerHealth] Damaged Player by {dmgAmount}");
        if (playerDied) return;
        // Ignore damage if player is dashing
        if (PlayerMovement.Instance.IsDashing)
            return;

        if (timeSinceLastDamage < damageCooldown)
            return;

        if (currentHealth > 0)
        {
            timeSinceLastDamage = 0f;
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
        if (GameManager.Instance.currentPlayerState == GameManager.PlayerState.Dead)
            return;
            
        currentHealth = 0;
        Debug.Log("Player Died :(");
        playerDied = true;
        onPlayerDie?.Invoke();
        GameManager.Instance.OnDeath();
    }


    #endregion


}
