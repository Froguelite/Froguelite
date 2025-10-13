using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthDisplay : MonoBehaviour
{

    // PlayerHealthDisplay displays current / max health


    #region VARIABLES


    [SerializeField] private HealthSingleDisplay singleHealthDisplayPrefab;
    [SerializeField] private Transform healthSpawnTransform;

    private PlayerHealth healthManager;

    public int maxHealth { get; private set; } = 0;
    private int displayedMaxHealth = 0;
    public int remainingHealth { get; private set; } = 0;
    private int displayedRemainingHealth = 0;
    public bool isDead { get; private set; } = false;
    private bool beatTopHeart = false;

    // Track the current top (highest non-empty) health display for beat animation
    private HealthSingleDisplay currentTopHealthDisplay = null;

    // Health displays in order 0 = first, 1 = second, etc.
    private List<HealthSingleDisplay> healthDisplays = new List<HealthSingleDisplay>();


    #endregion


    #region SETUP


    // Start, initialize with health according to PlayerHealth
    private void Start()
    {
        healthManager = StatsManager.Instance.playerHealth;
        if (healthManager == null)
        {
            Debug.LogError("[PlayerHealthDisplay] No PlayerHealth found in scene - is there a Player prefab in the scene?");
            return;
        }

        // Set initial max and remaining health
        SetMaxHealth(healthManager.maxHealth);
        SetRemainingHealth(healthManager.currentHealth, false);

        // Subscribe to health change events
        healthManager.onHealthChanged.AddListener(() =>
        {
            SetMaxHealth(healthManager.maxHealth);
            SetRemainingHealth(healthManager.currentHealth, !StatsManager.Instance.playerHealth.overrideHealAnims);
            if (StatsManager.Instance.playerHealth.needsBigBeat)
            {
                PerformBigBeatOnTopHealth(true);
                StatsManager.Instance.playerHealth.needsBigBeat = false;
            }
        });

        // Begin beating top heart
        SetBeatTopHeart(true);
    }


    // Sets whether the top health display should have a beating animation
    //-------------------------------------//
    public void SetBeatTopHeart(bool shouldBeat)
    //-------------------------------------//
    {
        beatTopHeart = shouldBeat;
        UpdateTopHealthBeatAnimation();

    } // END SetBeatTopHeart


    // Gets the current beatTopHeart state
    //-------------------------------------//
    public bool GetBeatTopHeart()
    //-------------------------------------//
    {
        return beatTopHeart;

    } // END GetBeatTopHeart


    #endregion


    #region MAX HEALTH


    // Sets the maximum health. Heals to full when increased.
    //-------------------------------------//
    public void SetMaxHealth(int maxHealth)
    //-------------------------------------//
    {
        if (this.maxHealth == maxHealth)
            return;

        if (maxHealth % 2 != 0)
        {
            Debug.LogWarning("TeammateHealthHandler: maxHealth is odd, reducing by 1 to make even");
            maxHealth--;
        }

        this.maxHealth = maxHealth;

        // Displayed < max, we need to add more health
        if (displayedMaxHealth < maxHealth)
        {
            // Add new health displays
            while (displayedMaxHealth < maxHealth)
            {
                HealthSingleDisplay newHealthDisplay = Instantiate(singleHealthDisplayPrefab, healthSpawnTransform);
                newHealthDisplay.SetupHealthDisplay(this);

                healthDisplays.Add(newHealthDisplay);
                displayedMaxHealth += 2;
            }
        }
        // Displayed > max, we need to remove some health
        else if (displayedMaxHealth > maxHealth)
        {
            // Remove excess health displays
            while (displayedMaxHealth > maxHealth && healthDisplays.Count > 0)
            {
                HealthSingleDisplay lastHealthDisplay = healthDisplays[healthDisplays.Count - 1];
                if (lastHealthDisplay != null)
                {
                    Destroy(lastHealthDisplay.gameObject);
                }
                healthDisplays.RemoveAt(healthDisplays.Count - 1);
                displayedMaxHealth -= 2;
            }
        }

        // Make sure remaining health amount matches
        remainingHealth = maxHealth;
        displayedRemainingHealth = maxHealth;

        // Update which health display should have the beat animation
        UpdateTopHealthBeatAnimation();

    } // END SetMaxHealth


    #endregion


    #region REMAINING HEALTH


    // Sets the remaining health immediately with no animation
    //-------------------------------------//
    public void SetRemainingHealth(int remainingHealth, bool animateTransition = true)
    //-------------------------------------//
    {
        if (this.remainingHealth == remainingHealth)
            return;

        // If new health is less than current, we need to take damage
        if (remainingHealth < this.remainingHealth)
        {
            int damageAmount = this.remainingHealth - remainingHealth;
            DamageHealth(damageAmount, animateTransition);
        }
        // If greater, we need to heal damage
        else if (remainingHealth > this.remainingHealth)
        {
            int healAmount = remainingHealth - this.remainingHealth;
            HealHealth(healAmount, animateTransition);
        }

    } // END SetRemainingHealthImmediate


    #endregion


    #region HEAL


    // Heals a certain amount of health
    //-------------------------------------//
    public void HealHealth(int healAmount, bool animateTransition = true)
    //-------------------------------------//
    {
        if (healAmount <= 0 || remainingHealth >= maxHealth)
            return;

        if (remainingHealth + healAmount >= maxHealth)
        {
            HealHealthFull(animateTransition);
            return;
        }

        // Heal the specified amount
        remainingHealth += healAmount;

        // If we were dead and now have health, we're no longer dead
        if (isDead && remainingHealth > 0)
        {
            isDead = false;
        }

        // Update the displays to match the new remaining health
        UpdateHealthDisplays(animateTransition);

    } // END HealHealth


    // Heals to maximum amount
    //-------------------------------------//
    public void HealHealthFull(bool animateTransition = true)
    //-------------------------------------//
    {
        remainingHealth = maxHealth;

        // If we were dead and now have health, we're no longer dead
        if (isDead && remainingHealth > 0)
        {
            isDead = false;
        }

        UpdateHealthDisplays(animateTransition);

    } // END HealHealthFull


    #endregion


    #region DAMAGE


    // Damages a certain amount of health
    //-------------------------------------//
    public void DamageHealth(int damageAmount, bool animateTransition = true)
    //-------------------------------------//
    {
        if (damageAmount <= 0 || remainingHealth <= 0)
            return;

        remainingHealth -= damageAmount;
        if (remainingHealth < 0)
            remainingHealth = 0;

        // Update the displays to match the new remaining health
        UpdateHealthDisplays(animateTransition);

        // Check if the character is now dead
        if (remainingHealth <= 0)
        {
            isDead = true;
            // Stop any beat animation when dead
            if (currentTopHealthDisplay != null)
            {
                currentTopHealthDisplay.StopSmallBeatsAnim();
                currentTopHealthDisplay = null;
            }
        }

    } // END DamageHealth


    #endregion


    #region UPDATE DISPLAYS


    // Updates the health displays to match the current remaining health
    //-------------------------------------//
    private void UpdateHealthDisplays(bool animateTransition = true)
    //-------------------------------//
    {
        int targetDisplayedHealth = remainingHealth;

        // Update each health display
        for (int i = 0; i < healthDisplays.Count; i++)
        {
            HealthSingleDisplay healthDisplay = healthDisplays[i];
            if (healthDisplay == null)
                continue;

            // Calculate how much health this display should show
            // Each display represents 2 health (0, 1, or 2)
            int healthForThisDisplay = Mathf.Min(2, targetDisplayedHealth);
            targetDisplayedHealth -= healthForThisDisplay;

            // Set the display state based on health amount
            if (healthForThisDisplay == 0)
            {
                if (animateTransition)
                    healthDisplay.SetToEmpty();
                else
                    healthDisplay.SetToEmptyImmediate();
            }
            else if (healthForThisDisplay == 1)
            {
                if (animateTransition)
                    healthDisplay.SetToHalf();
                else
                    healthDisplay.SetToHalfImmediate();
            }
            else // healthForThisDisplay == 2
            {
                if (animateTransition)
                    healthDisplay.SetToFull();
                else
                    healthDisplay.SetToFullImmediate();
            }
        }

        displayedRemainingHealth = remainingHealth;

        // Update which health display should have the beat animation
        UpdateTopHealthBeatAnimation();

    } // END UpdateHealthDisplays


    // Updates which health display should have the beat animation based on beatTopHeart setting
    //-------------------------------------//
    private void UpdateTopHealthBeatAnimation()
    //-------------------------------------//
    {
        // If beatTopHeart is false, don't start any beat animation; stop current animation if it is going
        if (!beatTopHeart)
        {
            if (currentTopHealthDisplay != null)
            {
                currentTopHealthDisplay.StopSmallBeatsAnim();
                currentTopHealthDisplay = null;
            }
            return;
        }

        // Find the highest non-empty health display (the "top" display)
        HealthSingleDisplay newTopDisplay = null;
        for (int i = healthDisplays.Count - 1; i >= 0; i--)
        {
            HealthSingleDisplay healthDisplay = healthDisplays[i];
            if (healthDisplay == null)
                continue;

            // Calculate how much health this display should show
            int healthStartIndex = i * 2;
            int healthForThisDisplay = Mathf.Min(2, Mathf.Max(0, remainingHealth - healthStartIndex));

            // If this display has any health, it's our top display
            if (healthForThisDisplay > 0)
            {
                newTopDisplay = healthDisplay;
                break;
            }
        }

        // Start beat animation on the new top display; if it's different than the old top display, stop the old one.
        if (newTopDisplay != null)
        {
            if (currentTopHealthDisplay != null && currentTopHealthDisplay != newTopDisplay)
            {
                currentTopHealthDisplay.StopSmallBeatsAnim();
            }
            currentTopHealthDisplay = newTopDisplay;
            currentTopHealthDisplay.StartSmallBeatsAnim();
        }

    } // END UpdateTopHealthBeatAnimation


    // Performs a single big beat animation on the top health display
    //-------------------------------------//
    public void PerformBigBeatOnTopHealth(bool includeEmptyHearts = false)
    //-------------------------------------//
    {
        if (includeEmptyHearts)
        {
            healthDisplays[healthDisplays.Count - 1].PerformBigBeat();
        }
        else if (currentTopHealthDisplay != null)
        {
            currentTopHealthDisplay.PerformBigBeat();
        }
    }


    #endregion


} // END PlayerHealthDisplay.cs
