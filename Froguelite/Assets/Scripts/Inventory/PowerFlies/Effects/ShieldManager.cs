using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all rotating shield lilypad objects around the player.
/// </summary>
public class ShieldManager : MonoBehaviour
{
    public static ShieldManager Instance { get; private set; }

    [Header("Shield Prefab")]
    [SerializeField] private GameObject shieldLilypadPrefab;

    [Header("Shield Settings")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float rotationSpeed = 1.5f; // Radians per second

    private List<ShieldLilypad> activeShields = new List<ShieldLilypad>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Adds a new shield to the rotation. Called when PowerFly is collected.
    /// </summary>
    public void AddShield()
    {
        if (shieldLilypadPrefab == null)
        {
            Debug.LogError("ShieldManager: shieldLilypadPrefab is not assigned!");
            return;
        }

        // Spawn shield as child of player
        GameObject shieldObj = Instantiate(shieldLilypadPrefab, transform);
        ShieldLilypad shield = shieldObj.GetComponent<ShieldLilypad>();

        if (shield != null)
        {
            activeShields.Add(shield);
            RecalculateShieldPositions();
        }
    }

    /// <summary>
    /// Distributes shields evenly in a circle around player.
    /// </summary>
    private void RecalculateShieldPositions()
    {
        int shieldCount = activeShields.Count;
        if (shieldCount == 0) return;

        float angleStep = (Mathf.PI * 2f) / shieldCount;

        for (int i = 0; i < shieldCount; i++)
        {
            if (activeShields[i] != null)
            {
                float startAngle = angleStep * i;
                activeShields[i].Initialize(orbitRadius, rotationSpeed, startAngle);
            }
        }
    }

    /// <summary>
    /// Removes a destroyed shield from the list.
    /// </summary>
    public void RemoveShield(ShieldLilypad shield)
    {
        activeShields.Remove(shield);
        RecalculateShieldPositions();
    }
}

