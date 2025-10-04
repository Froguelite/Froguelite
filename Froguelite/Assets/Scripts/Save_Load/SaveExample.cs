using System.Collections.Generic;
using UnityEngine;

public class SaveExample : MonoBehaviour
{
    // Player's current health
    [SerializeField] private int healthLevel = 100;

    // Number of coins the player has
    [SerializeField] private int _coinNum = 5;

    // Player's position in the game world
    [SerializeField] private Vector3 playerPosition;

    // Player's inventory items
    [SerializeField] private List<string> inventory;

    // Example enemy data
    [SerializeField] private EnemyDataExample enemy;

    // Property that saves automatically whenever CoinNum changes
    private int CoinNum
    {
        get { return _coinNum; }
        set
        {
            _coinNum = Mathf.Max(0, value); // Ensure coins are non-negative
            SaveManager.SaveForProfile(SaveVariable.Coins, _coinNum);
        }
    }

    private void Awake()
    {
        // Subscribe to SaveManager events to save/load data automatically
        SaveManager.SaveData += SaveHealth;
        //SaveManager.SaveData += SaveCoins; // Alternative using property
        SaveManager.SaveData += SavePlayerPosition;
        SaveManager.SaveData += SaveInventory;
        SaveManager.SaveData += SaveEnemy;

        SaveManager.LoadData += LoadHealth;
        SaveManager.LoadData += LoadCoins;
        SaveManager.LoadData += LoadPlayerPosition;
        SaveManager.LoadData += LoadInventory;
        SaveManager.LoadData += LoadEnemy;
    }

    private void OnDestroy()
    {
        // Unsubscribe from SaveManager events to prevent memory leaks
        SaveManager.SaveData -= SaveHealth;
        //SaveManager.SaveData -= SaveCoins;
        SaveManager.SaveData -= SavePlayerPosition;
        SaveManager.SaveData -= SaveInventory;
        SaveManager.SaveData -= SaveEnemy;

        SaveManager.LoadData -= LoadHealth;
        SaveManager.LoadData -= LoadCoins;
        SaveManager.LoadData -= LoadPlayerPosition;
        SaveManager.LoadData -= LoadInventory;
        SaveManager.LoadData -= LoadEnemy;
    }

    // Save the player's health to profile
    private void SaveHealth()
    {
        SaveManager.SaveForProfile(SaveVariable.PlayerHealth, healthLevel);
        Debug.Log("Health saved: " + healthLevel);
    }

    // Save the player's coins to profile (alternative using property)
    //private void SaveCoins()
    //{
    //    SaveManager.SaveForProfile(SaveVariable.Coins, _coinNum);
    //    Debug.Log("Coins saved: " + _coinNum);
    //}

    // Save the player's position to profile
    private void SavePlayerPosition()
    {
        SaveManager.SaveForProfile(SaveVariable.PlayerPosition, playerPosition);
        Debug.Log("Player position saved: " + playerPosition);
    }

    // Save the player's inventory to profile
    private void SaveInventory()
    {
        SaveManager.SaveForProfile(SaveVariable.Inventory, inventory);
        Debug.Log("Inventory saved: " + string.Join(", ", inventory));
    }

    // Save enemy data to profile
    private void SaveEnemy()
    {
        SaveManager.SaveForProfile(SaveVariable.EnemyStats, enemy);
        Debug.Log("Enemy data saved: " + enemy.enemyType);
    }

    // Load the player's health from profile
    private void LoadHealth()
    {
        try
        {
            healthLevel = SaveManager.LoadForProfile<int>(SaveVariable.PlayerHealth);
            Debug.Log("Health loaded: " + healthLevel);
        }
        catch (KeyNotFoundException)
        {
            healthLevel = 100; // default value
            Debug.LogWarning("No saved health found. Using default: " + healthLevel);
        }
    }

    // Load the player's coins from profile
    private void LoadCoins()
    {
        try
        {
            CoinNum = SaveManager.LoadForProfile<int>(SaveVariable.Coins);
            Debug.Log("Coins loaded: " + CoinNum);
        }
        catch (KeyNotFoundException)
        {
            CoinNum = 0; // default value
            Debug.LogWarning("No saved coins found. Using default: " + CoinNum);
        }
    }

    // Load the player's position from profile
    private void LoadPlayerPosition()
    {
        try
        {
            playerPosition = SaveManager.LoadForProfile<Vector3>(SaveVariable.PlayerPosition);
            Debug.Log("Player position loaded: " + playerPosition);
        }
        catch (KeyNotFoundException)
        {
            playerPosition = Vector3.zero; // default value
            Debug.LogWarning("No saved player position found. Using default: " + playerPosition);
        }
    }

    // Load the player's inventory from profile
    private void LoadInventory()
    {
        try
        {
            inventory = SaveManager.LoadForProfile<List<string>>(SaveVariable.Inventory);
            Debug.Log("Inventory loaded: " + string.Join(", ", inventory));
        }
        catch (KeyNotFoundException)
        {
            inventory = new List<string>(); // default empty inventory
            Debug.LogWarning("No saved inventory found. Using default empty inventory.");
        }
    }

    // Load enemy data from profile
    private void LoadEnemy()
    {
        try
        {
            enemy = SaveManager.LoadForProfile<EnemyDataExample>(SaveVariable.EnemyStats);
            Debug.Log("Enemy data loaded: " + enemy.enemyType);
        }
        catch (KeyNotFoundException)
        {
            enemy = new EnemyDataExample(); // default value
            Debug.LogWarning("No saved enemy data found. Using default");
        }
    }

    void Start() { }

    void Update() { }
}

// Struct to store example enemy data
[System.Serializable]
public struct EnemyDataExample
{
    public string enemyType;  // Type of enemy
    public int level;         // Enemy level
    public Vector3 position;  // Enemy position in the game world
}
