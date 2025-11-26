using System.Collections.Generic;
using UnityEngine;

public class Totem : MonoBehaviour
{
    // Totem manages a 3-layered totem that spawns enemy waves when interacted with
    // After defeating all waves, spawns 3 power flies for player to choose from

    #region VARIABLES

    [Header("Totem Sprite")]
    [SerializeField] private SpriteRenderer totemSprite;
    [SerializeField] private Collider2D totemCollision;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem dirtParticles;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f, 1f);
    [SerializeField] private float glowPulseSpeed = 2f;
    
    [Header("Sinking Animation")]
    [SerializeField] private float sinkDistance = 0.5f; // Distance to sink per wave
    [SerializeField] private float sinkDuration = 1f; // Time for sinking animation
    [SerializeField] private float shakeIntensity = 0.1f; // Shake intensity during sinking
    [SerializeField] private float shakeSpeed = 20f; // Shake speed during sinking
    
    [Header("Enemy Spawning")]
    [SerializeField] private int minEnemiesPerWave = 2;
    [SerializeField] private int maxEnemiesPerWave = 4;
    [SerializeField] private float spawnRadius = 3f;
    
    [Header("Power Fly Rewards")]
    [SerializeField] private float flySpreadDistance = 2f;
    [SerializeField] private float flySpawnHeight = 1f;

    // State tracking
    private int currentWave = 0; 
    private TotemState currentState = TotemState.Idle;
    private Room parentRoom;
    private List<IEnemy> currentWaveEnemies = new List<IEnemy>();
    private List<PowerFly> spawnedPowerFlies = new List<PowerFly>();
    
    // Visual components
    private Vector3 initialPosition;
    private bool isGlowing = false;
    private bool isSinking = false;

    public enum TotemState
    {
        Idle,           // Totem is inactive
        Spawning,       // Currently spawning enemies for current wave
        WaitingForDefeat, // Enemies spawned, waiting for all to be defeated
        Completed       // All waves defeated, power flies spawned
    }

    #endregion

    #region MONOBEHAVIOUR AND SETUP

    private void Awake()
    {
        // Validate totem sprite reference
        if (totemSprite == null)
        {
            Debug.LogError("Totem: totemSprite is not assigned!");
        }
        else
        {
            // Store initial local position and normal color from sprite
            initialPosition = totemSprite.transform.localPosition;
            normalColor = totemSprite.color;
        }
    }
    


    private void Start()
    {
        // Totem starts in idle state with normal color
        if (totemSprite != null)
        {
            totemSprite.color = normalColor;
        }
    }

    private void Update()
    {
        // Handle golden glow pulsing effect
        if (isGlowing && currentState == TotemState.WaitingForDefeat && !isSinking)
        {
            PulseGoldenGlow();
        }
    }

    #endregion

    #region INITIALIZATION

    // Sets the parent room reference
    public void SetParentRoom(Room room)
    {
        parentRoom = room;
    }



    #endregion

    #region INTERACTION

    // Called when player's tongue hits the totem
    public void OnInteract()
    {
        if (currentState != TotemState.Idle)
        {
            return; // Already active or completed
        }

        StartWave(currentWave);
        AudioManager.Instance.PlayOverrideMusic(MusicType.SubBoss);
        AudioManager.Instance.PlaySound(CombatSound.TotemImpact, 1.4f);
    }

    #endregion

    #region WAVE MANAGEMENT

    // Starts a wave
    private void StartWave(int waveIndex)
    {
        if (waveIndex >= 3)
        {
            Debug.LogError("Totem: Attempted to start wave for invalid index: " + waveIndex);
            return;
        }

        currentState = TotemState.Spawning;
        if (parentRoom != null) parentRoom.SetTotemActive(true);
        
        // Make totem glow golden
        StartGoldenGlow();
        
        // Spawn enemies for this wave
        int enemyCount = Random.Range(minEnemiesPerWave, maxEnemiesPerWave + 1);
        SpawnEnemies(enemyCount);
        RegisterWaveEnemiesWithRoom();
        
        currentState = TotemState.WaitingForDefeat;
    }

    // Spawns enemies around the totem
    private void SpawnEnemies(int count)
    {
        currentWaveEnemies.Clear();

        List<IEnemy> spawnedEnemies = EnemyFactory.Instance.SpawnEnemiesForRoom(parentRoom.roomData.zone, parentRoom);

        foreach (IEnemy newEnemy in spawnedEnemies)
        {
            if (newEnemy != null)
            {
                currentWaveEnemies.Add(newEnemy);
                newEnemy.BeginPlayerChase();
            }
        }
    }

    // Gets a random spawn position around the totem
    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 totemPosition = transform.position;
        
        // Try to find a valid spawn position within spawn radius
        for (int attempts = 0; attempts < 10; attempts++)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 spawnPos = totemPosition + (randomDirection * Random.Range(1f, spawnRadius));
            
            // Check if position is valid (not in water, not too close to totem)
            if (IsValidSpawnPosition(spawnPos))
            {
                return spawnPos;
            }
        }
        
        // Fallback: spawn at a fixed distance from totem
        Vector2 fallbackDirection = Random.insideUnitCircle.normalized;
        return totemPosition + (fallbackDirection * spawnRadius);
    }

    // Checks if a spawn position is valid
    private bool IsValidSpawnPosition(Vector2 position)
    {
        // Basic validation - could be enhanced with tile checking
        float distanceFromTotem = Vector2.Distance(position, transform.position);
        return distanceFromTotem >= 1f && distanceFromTotem <= spawnRadius;
    }

    // Called by Room when enemies die OR by enemies we spawn
    public void OnEnemyDefeated(IEnemy defeatedEnemy)
    {
        if (currentWaveEnemies.Contains(defeatedEnemy))
        {
            currentWaveEnemies.Remove(defeatedEnemy);
            
            // Check if all enemies in current wave are defeated
            if (currentWaveEnemies.Count == 0)
            {
                OnWaveComplete();
            }
        }
    }

    // Register our spawned enemies into the Room's list to preserve global behavior
    private void RegisterWaveEnemiesWithRoom()
    {
        if (parentRoom == null) return;
        foreach (var enemy in currentWaveEnemies)
        {
            //if (enemy is MeleeEnemy melee && !parentRoom.enemies.Contains(enemy))
            //{
            //    parentRoom.enemies.Add(enemy);
            //}
            Debug.LogWarning("Commented RegisterWaveEnemiesWithRoom function due to compilation error");
        }
    }

    // Called when current wave is complete
    private void OnWaveComplete()
    {
        Debug.Log($"Totem: Wave {currentWave + 1} completed!");
        
        // Stop glowing
        StopGoldenGlow();
        
        // Sink the totem
        StartCoroutine(SinkTotemCoroutine());
        
        // Move to next wave or complete totem
        currentWave++;
        
        if (currentWave >= 3)
        {
            // Totem will complete after final sink animation
        }
        else
        {
            // Next wave will start after sinking animation completes
        }
    }

    // Starts the next wave
    private void StartNextWave()
    {
        if (currentWave < 3)
        {
            StartWave(currentWave);
        }
    }

    #endregion

    #region VISUAL EFFECTS

    // Starts the golden glow effect
    private void StartGoldenGlow()
    {
        isGlowing = true;
    }

    // Stops the golden glow effect
    private void StopGoldenGlow()
    {
        isGlowing = false;
        if (totemSprite != null)
        {
            totemSprite.color = normalColor;
        }
    }

    // Pulses between normal and golden color
    private void PulseGoldenGlow()
    {
        if (totemSprite == null) return;
        
        float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
        totemSprite.color = Color.Lerp(normalColor, goldenColor, pulse);
    }

    // Coroutine to animate totem sinking into the ground
    private System.Collections.IEnumerator SinkTotemCoroutine()
    {
        isSinking = true;
        
        if (totemSprite == null)
        {
            Debug.LogError("Totem: Cannot sink - totemSprite is null!");
            isSinking = false;
            yield break;
        }
        
        Vector3 startPosition = totemSprite.transform.localPosition;
        Vector3 targetPosition = startPosition - new Vector3(0, sinkDistance, 0);
        float elapsedTime = 0f;
        
        // Play dirt particles if available
        if (dirtParticles != null)
        {
            dirtParticles.Play();
        }

        AudioManager.Instance.PlaySoundIndefinite(CombatSound.TotemMove, 1.6f);
        
        // Animate sinking with shake effect
        while (elapsedTime < sinkDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / sinkDuration;
            
            // Smooth interpolation for sinking
            Vector3 sinkPosition = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Add shake effect
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity * (1 - t); // Reduce shake over time
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity * (1 - t);
            
            totemSprite.transform.localPosition = sinkPosition + new Vector3(shakeX, shakeY, 0);
            
            yield return null;
        }
        
        // Ensure final position is exact
        totemSprite.transform.localPosition = targetPosition;
        
        // Stop dirt particles
        if (dirtParticles != null)
        {
            dirtParticles.Stop();
        }

        AudioManager.Instance.StopIndefiniteSound(CombatSound.TotemMove);
        AudioManager.Instance.PlaySound(CombatSound.TotemImpact, 1.4f);
        
        isSinking = false;
        
        Debug.Log($"Totem: Sunk segment {currentWave} into ground");
        
        // Handle next action based on wave count
        if (currentWave >= 3)
        {
            OnTotemComplete();
        }
        else
        {
            // Start next wave after a brief delay
            Invoke(nameof(StartNextWave), 1f);
        }
    }

    #endregion

    #region COMPLETION AND REWARDS

    // Called when all waves are defeated
    private void OnTotemComplete()
    {
        Debug.Log("Totem: All waves defeated! Spawning power flies...");
        
        currentState = TotemState.Completed;
        isGlowing = false;
        
        // Disable collision
        if (totemCollision != null)
        {
            totemCollision.enabled = false;
        }
        
        // Spawn 3 power flies for player to choose from
        SpawnPowerFlyRewards();
        AudioManager.Instance.ClearOverrideMusic();

        AudioManager.Instance.PlaySound(TravelSound.RoomClear);
        
        // Notify parent room that totem is complete
        if (parentRoom != null) parentRoom.OnTotemCompleted();
    }

    // Spawns 3 power flies in a spread pattern
    private void SpawnPowerFlyRewards()
    {
        spawnedPowerFlies.Clear();
        
        // Roll 3 DIFFERENT power flies (avoid duplicates)
        PowerFlyData[] flyData = GetThreeDifferentPowerFlies();
        
        // Spawn flies in a horizontal spread
        Vector3 totemPosition = transform.position;
        Vector3[] spawnPositions = new Vector3[3]
        {
            totemPosition + new Vector3(-flySpreadDistance, flySpawnHeight, 0),
            totemPosition + new Vector3(0, flySpawnHeight, 0),
            totemPosition + new Vector3(flySpreadDistance, flySpawnHeight, 0)
        };
        
        for (int i = 0; i < 3; i++)
        {
            if (flyData[i] != null)
            {
                PowerFly newFly = PowerFlyFactory.Instance.SpawnPowerFly(flyData[i], transform, spawnPositions[i]);
                if (newFly != null)
                {
                    spawnedPowerFlies.Add(newFly);
                    
                    // Subscribe to fly collection to destroy other flies
                    SubscribeToFlyCollection(newFly, i);
                }
            }
        }
        
        Debug.Log($"Totem: Spawned {spawnedPowerFlies.Count} different power flies for player choice");
    }
    
    // Ensures we get 3 different power flies (no duplicates)
    private PowerFlyData[] GetThreeDifferentPowerFlies()
    {
        PowerFlyData[] flyData = new PowerFlyData[3];
        List<PowerFlyData> usedFlies = new List<PowerFlyData>();
        
        for (int i = 0; i < 3; i++)
        {
            PowerFlyData newFly = null;
            int attempts = 0;
            const int maxAttempts = 20; // Prevent infinite loop
            
            // Keep rolling until we get a different fly
            do
            {
                newFly = PowerFlyFactory.Instance.RollFlyUnweighted();
                attempts++;
                
                // If we've tried too many times, just use what we got
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"Totem: Could not find unique power fly after {maxAttempts} attempts, using duplicate");
                    break;
                }
            }
            while (newFly != null && usedFlies.Contains(newFly));
            
            flyData[i] = newFly;
            if (newFly != null)
            {
                usedFlies.Add(newFly);
            }
        }
        
        return flyData;
    }

    // Subscribes to a power fly's collection event
    private void SubscribeToFlyCollection(PowerFly fly, int flyIndex)
    {
        // Subscribe to the pre-collection callback
        fly.onPreCollect += OnPowerFlyCollected;
    }

    // Destroys all other power flies when one is collected
    private void OnPowerFlyCollected(PowerFly collectedFly)
    {
        foreach (PowerFly fly in spawnedPowerFlies)
        {
            if (fly != null && fly != collectedFly)
            {
                Destroy(fly.gameObject);
            }
        }
        
        spawnedPowerFlies.Clear();
        Debug.Log("Totem: Player chose a power fly, others destroyed");
    }

    #endregion

    #region PUBLIC GETTERS

    // Gets the current state of the totem
    public TotemState GetCurrentState()
    {
        return currentState;
    }

    // Gets the current wave index
    public int GetCurrentWave()
    {
        return currentWave;
    }

    // Checks if the totem is active =
    public bool IsActive()
    {
        return currentState == TotemState.Spawning || currentState == TotemState.WaitingForDefeat;
    }

    // Gets the number of remaining enemies in current wave
    public int GetRemainingEnemies()
    {
        return currentWaveEnemies.Count;
    }

    #endregion

    #region DEBUG

    private void OnDrawGizmosSelected()
    {
        // Draw spawn radius (2D circle using multiple line segments)
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(spawnRadius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * spawnRadius, Mathf.Sin(angle) * spawnRadius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
        
        // Draw power fly spawn positions
        Gizmos.color = Color.cyan;
        Vector3 totemPos = transform.position;
        Gizmos.DrawWireSphere(totemPos + new Vector3(-flySpreadDistance, flySpawnHeight, 0), 0.3f);
        Gizmos.DrawWireSphere(totemPos + new Vector3(0, flySpawnHeight, 0), 0.3f);
        Gizmos.DrawWireSphere(totemPos + new Vector3(flySpreadDistance, flySpawnHeight, 0), 0.3f);
    }

    #endregion
}
