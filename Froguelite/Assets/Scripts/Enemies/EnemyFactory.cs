using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{

    // EnemyFactory handles the creation and spawning of enemies in rooms


    #region VARIABLES


    public static EnemyFactory Instance { get; private set; }

    private RoomEnemySpawnGroup[] roomEnemySpawnGroups;
    [SerializeField] private string roomEnemySpawnGroupResourcePath = "RoomEnemySpawnGroups/Zone1";


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Awake
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        LoadEnemySpawnGroups();
    }


    // Loads all RoomEnemySpawnGroup assets from the Resources folder
    private void LoadEnemySpawnGroups()
    {
        roomEnemySpawnGroups = Resources.LoadAll<RoomEnemySpawnGroup>(roomEnemySpawnGroupResourcePath);
        if (roomEnemySpawnGroups.Length == 0)
        {
            Debug.LogWarning($"No RoomEnemySpawnGroup assets found in {roomEnemySpawnGroupResourcePath}!");
        }
    }


    #endregion


    #region SPAWNING


    // Spawns all enemies for a given room based on a randomly chosen spawn group
    public List<IEnemy> SpawnEnemiesForRoom(Room room)
    {
        List<IEnemy> spawnedEnemies = new List<IEnemy>();

        // Choose a random spawn group
        RoomEnemySpawnGroup spawnGroup = roomEnemySpawnGroups[Random.Range(0, roomEnemySpawnGroups.Length)];

        // Loop through each enemy spawn entry and spawn appropriate enemies
        foreach (RoomEnemySpawnGroup.EnemySpawnEntry entry in spawnGroup.enemySpawnEntries)
        {
            int enemyCount = Random.Range(entry.minEnemyCount, entry.maxEnemyCount + 1);
            for (int i = 0; i < enemyCount; i++)
            {
                // Choose a random enemy type from the possible enemies
                EnemyBase enemyType = entry.possibleEnemies[Random.Range(0, entry.possibleEnemies.Count)];
                Vector2 spawnPosition = room.GetRandomEnemySpawnPosition();
                EnemyBase newEnemy = Instantiate(enemyType, spawnPosition, Quaternion.identity, room.transform);
                newEnemy.InitializeEnemy(room);
                spawnedEnemies.Add(newEnemy);
            }
        }

        return spawnedEnemies;
    }
    

    #endregion


}
