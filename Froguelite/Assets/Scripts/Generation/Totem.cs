using System.Collections.Generic;
using UnityEngine;

public class Totem : MonoBehaviour
{
    // Totem manages a 3-layered totem that spawns enemy waves when interacted with
    // After defeating all waves, spawns 3 power flies for player to choose from

    #region VARIABLES

    [Header("Totem Layers")]
    [SerializeField] private GameObject layer1;
    [SerializeField] private GameObject layer2;
    [SerializeField] private GameObject layer3;
    
    [Header("Visual Effects")]
    [SerializeField] private Color glowColor = Color.yellow;
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private float glowPulseSpeed = 2f;
    
    [Header("Enemy Spawning")]
    [SerializeField] private int minEnemiesPerWave = 2;
    [SerializeField] private int maxEnemiesPerWave = 4;
    [SerializeField] private float spawnRadius = 3f;
    
    [Header("Power Fly Rewards")]
    [SerializeField] private float flySpreadDistance = 2f;
    [SerializeField] private float flySpawnHeight = 1f;

    // State tracking
    private int currentLayer = 0; 
    private TotemState currentState = TotemState.Idle;
    private Room parentRoom;
    private List<IEnemy> currentWaveEnemies = new List<IEnemy>();
    private List<PowerFly> spawnedPowerFlies = new List<PowerFly>();
    
    // Visual components
    private List<SpriteRenderer> layerRenderers = new List<SpriteRenderer>();
    private List<Color> originalColors = new List<Color>();
    private bool isGlowing = false;

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
        // If layers aren't assigned, create them at runtime
        if (layer1 == null || layer2 == null || layer3 == null)
        {
            CreateLayersAtRuntime();
        }
        
        // Initialize layer renderers and store original colors
        InitializeLayerRenderers();
    }
    
    // Creates visual layers at runtime if not assigned in prefab
    private void CreateLayersAtRuntime()
    {
        // Create Layer 1 (bottom)
        layer1 = CreateLayer("Layer1", new Vector3(0, 0, 0), new Vector3(1, 0.5f, 1), new Color(0.4f, 0.3f, 0.2f, 1f), 0);
        
        // Create Layer 2 (middle)
        layer2 = CreateLayer("Layer2", new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.4f, 1), new Color(0.5f, 0.45f, 0.4f, 1f), 1);
        
        // Create Layer 3 (top)
        layer3 = CreateLayer("Layer3", new Vector3(0, 0.9f, 0), new Vector3(0.6f, 0.3f, 1), new Color(0.6f, 0.55f, 0.5f, 1f), 2);
        
    }
    
    // Helper to create a single layer
    private GameObject CreateLayer(string name, Vector3 localPos, Vector3 scale, Color color, int sortOrder)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(transform);
        layer.transform.localPosition = localPos;
        layer.transform.localScale = scale;
        
        SpriteRenderer sr = layer.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortOrder;
                
        return layer;
    }
    
    // Creates a simple white square sprite
    private Sprite CreateSquareSprite()
    {
        // Create a larger texture for better visibility
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        
        // Fill with white
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private void Start()
    {
        // Set initial state - all layers visible, none glowing
        UpdateLayerVisibility();
    }

    private void Update()
    {
        // Handle glow pulsing effect
        if (isGlowing && currentState == TotemState.WaitingForDefeat)
        {
            PulseGlowEffect();
        }
    }

    #endregion

    #region INITIALIZATION

    // Sets the parent room reference
    public void SetParentRoom(Room room)
    {
        parentRoom = room;
    }

    // Initializes layer renderers and stores original colors
    private void InitializeLayerRenderers()
    {
        layerRenderers.Clear();
        originalColors.Clear();

        if (layer1 != null)
        {
            SpriteRenderer renderer = layer1.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                layerRenderers.Add(renderer);
                originalColors.Add(renderer.color);
            }
        }

        if (layer2 != null)
        {
            SpriteRenderer renderer = layer2.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                layerRenderers.Add(renderer);
                originalColors.Add(renderer.color);
            }
        }

        if (layer3 != null)
        {
            SpriteRenderer renderer = layer3.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                layerRenderers.Add(renderer);
                originalColors.Add(renderer.color);
            }
        }
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

        StartWave(currentLayer);
    }

    #endregion

    #region WAVE MANAGEMENT

    // Starts a wave for the specified layer
    private void StartWave(int layerIndex)
    {
        if (layerIndex >= 3)
        {
            Debug.LogError("Totem: Attempted to start wave for invalid layer index: " + layerIndex);
            return;
        }

        currentState = TotemState.Spawning;
        if (parentRoom != null) parentRoom.SetTotemActive(true);
        
        // Make current layer glow
        SetLayerGlow(layerIndex, true);
        
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

        List<IEnemy> spawnedEnemies = EnemyFactory.Instance.SpawnEnemiesForRoom(parentRoom);

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
        Debug.Log($"Totem: Wave {currentLayer + 1} completed!");
        
        // Break current layer
        BreakLayer(currentLayer);
        
        // Move to next layer or complete totem
        currentLayer++;
        
        if (currentLayer >= 3)
        {
            OnTotemComplete();
        }
        else
        {
            // Start next wave after a brief delay
            Invoke(nameof(StartNextWave), 1f);
        }
    }

    // Starts the next wave
    private void StartNextWave()
    {
        if (currentLayer < 3)
        {
            StartWave(currentLayer);
        }
    }

    #endregion

    #region LAYER MANAGEMENT

    // Sets glow effect for a specific layer
    private void SetLayerGlow(int layerIndex, bool glow)
    {
        if (layerIndex < 0 || layerIndex >= layerRenderers.Count)
            return;

        isGlowing = glow;
        
        if (glow)
        {
            // Start glow effect
            layerRenderers[layerIndex].color = glowColor;
        }
        else
        {
            // Stop glow effect
            layerRenderers[layerIndex].color = originalColors[layerIndex];
        }
    }

    // Breaks (disables) a layer
    private void BreakLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= 3)
            return;

        // Stop glowing
        SetLayerGlow(layerIndex, false);
        
        // Disable the layer GameObject
        switch (layerIndex)
        {
            case 0:
                if (layer1 != null) layer1.SetActive(false);
                break;
            case 1:
                if (layer2 != null) layer2.SetActive(false);
                break;
            case 2:
                if (layer3 != null) layer3.SetActive(false);
                break;
        }
        
        Debug.Log($"Totem: Layer {layerIndex + 1} broken!");
    }

    // Updates layer visibility based on current state
    private void UpdateLayerVisibility()
    {
        // All layers start visible
        if (layer1 != null) layer1.SetActive(true);
        if (layer2 != null) layer2.SetActive(true);
        if (layer3 != null) layer3.SetActive(true);
    }

    // Pulses the glow effect
    private void PulseGlowEffect()
    {
        if (currentLayer < layerRenderers.Count)
        {
            float pulse = Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(originalColors[currentLayer], glowColor, pulse);
            layerRenderers[currentLayer].color = currentColor;
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
        
        // Spawn 3 power flies for player to choose from
        SpawnPowerFlyRewards();
        
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
                newFly = PowerFlyFactory.Instance.RollFlyForFlyRoom();
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

    // Gets the current layer index
    public int GetCurrentLayer()
    {
        return currentLayer;
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
