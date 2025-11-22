using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyFactory : MonoBehaviour
{

    // EnemyFactory handles the creation and spawning of enemies in rooms


    #region VARIABLES


    public static EnemyFactory Instance { get; private set; }

    private RoomEnemySpawnGroup[] swampRoomEnemySpawnGroups;
    private RoomEnemySpawnGroup[] swampSubBossEnemySpawnGroups;
    private RoomEnemySpawnGroup[] forestRoomEnemySpawnGroups;
    private RoomEnemySpawnGroup[] forestSubBossEnemySpawnGroups;
    [SerializeField] private string swampRoomEnemySpawnGroupResourcePath = "RoomEnemySpawnGroups/Zone1/Basic";
    [SerializeField] private string swampSubBossEnemySpawnGroupResourcePath = "RoomEnemySpawnGroups/Zone1/SubBosses";
    [SerializeField] private string forestRoomEnemySpawnGroupResourcePath = "RoomEnemySpawnGroups/Zone2/Basic";
    [SerializeField] private string forestSubBossEnemySpawnGroupResourcePath = "RoomEnemySpawnGroups/Zone2/SubBosses";


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
        swampRoomEnemySpawnGroups = Resources.LoadAll<RoomEnemySpawnGroup>(swampRoomEnemySpawnGroupResourcePath);
        if (swampRoomEnemySpawnGroups.Length == 0)
            Debug.LogWarning($"No RoomEnemySpawnGroup assets found in {swampRoomEnemySpawnGroupResourcePath}!");
        else
            Debug.Log($"Loaded {swampRoomEnemySpawnGroups.Length} basic spawn groups");

        swampSubBossEnemySpawnGroups = Resources.LoadAll<RoomEnemySpawnGroup>(swampSubBossEnemySpawnGroupResourcePath);
        if (swampSubBossEnemySpawnGroups.Length == 0)
            Debug.LogWarning($"No RoomEnemySpawnGroup assets found in {swampSubBossEnemySpawnGroupResourcePath}!");
        else
            Debug.Log($"Loaded {swampSubBossEnemySpawnGroups.Length} sub-boss spawn groups");

        forestRoomEnemySpawnGroups = Resources.LoadAll<RoomEnemySpawnGroup>(forestRoomEnemySpawnGroupResourcePath);
        if (forestRoomEnemySpawnGroups.Length == 0)
            Debug.LogWarning($"No RoomEnemySpawnGroup assets found in {forestRoomEnemySpawnGroupResourcePath}!");
        else
            Debug.Log($"Loaded {forestRoomEnemySpawnGroups.Length} basic spawn groups");

        forestSubBossEnemySpawnGroups = Resources.LoadAll<RoomEnemySpawnGroup>(forestSubBossEnemySpawnGroupResourcePath);
        if (forestSubBossEnemySpawnGroups.Length == 0)
            Debug.LogWarning($"No RoomEnemySpawnGroup assets found in {forestSubBossEnemySpawnGroupResourcePath}!");
        else
            Debug.Log($"Loaded {forestSubBossEnemySpawnGroups.Length} sub-boss spawn groups");
    }

    #endregion


    #region SPAWNING


    // Spawns all enemies for a given room based on a randomly chosen spawn group
    public List<IEnemy> SpawnEnemiesForRoom(int zone, Room room)
    {
        List<IEnemy> spawnedEnemies = new List<IEnemy>();

        // Choose a random spawn group
        RoomEnemySpawnGroup[] roomEnemySpawnGroups = zone == 0 ? swampRoomEnemySpawnGroups : forestRoomEnemySpawnGroups;
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


    // Spawns a sub-zone boss for the given room
    public List<IEnemy> SpawnSubZoneBossForRoom(int zone, Room room)
    {
        List<IEnemy> spawnedEnemies = new List<IEnemy>();

        // Choose a random spawn group
        RoomEnemySpawnGroup[] subBossEnemySpawnGroups = zone == 0 ? swampSubBossEnemySpawnGroups : forestSubBossEnemySpawnGroups;
        RoomEnemySpawnGroup spawnGroup = subBossEnemySpawnGroups[Random.Range(0, subBossEnemySpawnGroups.Length)];

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
