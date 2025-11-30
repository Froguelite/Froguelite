using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    #region VARIABLES
    public static PowerUpManager Instance;

    [SerializeField] private GameObject powerUpPrefab;
    [SerializeField] private float spawnRange = 1f;
    [SerializeField] private int maxPowerUps = 3;
    [SerializeField] private int currentPowerUps;
    private List<GameObject> activePowerUps = new List<GameObject>();
    #endregion

    #region SETUP

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        SaveManager.LoadData += LoadPowerUps;
    }

    private void OnDestroy()
    {
        SaveManager.LoadData -= LoadPowerUps;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    #endregion

    #region LOAD CURRENT POWERUP NUMBER

    public void LoadPowerUps()
    {
        Debug.Log("[PowerUpManager] Loading power ups");
        try
        {
            currentPowerUps = SaveManager.LoadForProfile<int>(SaveVariable.PlayerLevel) - 1;
            Debug.Log($"[PowerUpManager] Loaded current power up {currentPowerUps} from profile {SaveManager.activeProfile}");
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // No saved data yet, use default value (1)
            currentPowerUps = 0;
            Debug.Log($"[PowerUpManager] No saved power up found, defaulting to 0");
        }
        catch (System.Exception ex)
        {
            // Handle other exceptions (e.g., no active profile set)
            Debug.LogWarning($"[PowerUpManager] Failed to load power up: {ex.Message}");
            currentPowerUps = 0;
        }
        GeneratePowerUps();
    }

    #endregion

    #region POWER UP MANAGEMENT

    private void GeneratePowerUps()
    {
        //LoadPowerUps();
        int powerUpsToGenerate = maxPowerUps - currentPowerUps;
        int generatedPowerUps = activePowerUps.Count;
        Debug.Log($"[PowerUpManager] Generating power ups. To generate: {powerUpsToGenerate}, already generated: {generatedPowerUps}");
        for (int i = generatedPowerUps; i < powerUpsToGenerate; i++)
        {

            float x = Random.Range(-spawnRange, spawnRange);
            float y = Random.Range(-spawnRange, spawnRange);
            Vector3 randomPos = transform.position + new Vector3(x, y, 0);
            GameObject powerUp = Instantiate(powerUpPrefab, randomPos, Quaternion.identity);
            activePowerUps.Add(powerUp);
        }
    }

    public void PowerUpCollected()
    {
        currentPowerUps++;
        Debug.Log($"[PowerUpManager] Power up collected. Current power ups: {currentPowerUps}");
        // Additional logic for when a power-up is collected can be added here
    }

    #endregion
}
