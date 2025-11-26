using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages orbital flies that rotate around the player and damage enemies.
/// </summary>
public class OrbitalFlyManager : MonoBehaviour
{
    public static OrbitalFlyManager Instance { get; private set; }

    [Header("Orbital Prefab")]
    [SerializeField] private GameObject orbitalFlyPrefab;

    [Header("Orbital Settings")]
    [SerializeField] private float orbitRadius = 4f; // Larger than shield radius
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float spriteSpinSpeed = 360f; // Degrees per second

    private List<OrbitalFly> activeOrbitals = new List<OrbitalFly>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        // Subscribe to reset event
        GameManager.ResetPlayerState += RemoveAllOrbitals;
    }

    private void OnDestroy()
    {
        // Unsubscribe from reset event
        GameManager.ResetPlayerState -= RemoveAllOrbitals;
    }

    /// <summary>
    /// Adds a new orbital fly to the rotation.
    /// </summary>
    public void AddOrbitalFly()
    {
        if (orbitalFlyPrefab == null)
        {
            Debug.LogError("OrbitalFlyManager: orbitalFlyPrefab is not assigned!");
            return;
        }

        GameObject orbitalObj = Instantiate(orbitalFlyPrefab, transform);
        OrbitalFly orbital = orbitalObj.GetComponent<OrbitalFly>();

        if (orbital != null)
        {
            activeOrbitals.Add(orbital);
            RecalculateOrbitalPositions();
        }
    }

    /// <summary>
    /// Distributes orbital flies evenly around player.
    /// </summary>
    private void RecalculateOrbitalPositions()
    {
        int orbitalCount = activeOrbitals.Count;
        if (orbitalCount == 0) return;

        float angleStep = (Mathf.PI * 2f) / orbitalCount;

        for (int i = 0; i < orbitalCount; i++)
        {
            if (activeOrbitals[i] != null)
            {
                float startAngle = angleStep * i;
                activeOrbitals[i].Initialize(orbitRadius, rotationSpeed, spriteSpinSpeed, startAngle);
            }
        }
    }

    /// <summary>
    /// Removes a destroyed orbital from the list.
    /// </summary>
    public void RemoveOrbital(OrbitalFly orbital)
    {
        activeOrbitals.Remove(orbital);
        RecalculateOrbitalPositions();
    }

    /// <summary>
    /// Removes all orbital flies.
    /// </summary>
    public void RemoveAllOrbitals()
    {
        // Destroy all active orbitals
        for (int i = activeOrbitals.Count - 1; i >= 0; i--)
        {
            if (activeOrbitals[i] != null)
            {
                Destroy(activeOrbitals[i].gameObject);
            }
        }
        
        // Clear the list
        activeOrbitals.Clear();
    }
}

