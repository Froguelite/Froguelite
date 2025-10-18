using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySpawnGroup", menuName = "Froguelite/EnemySpawnGroup")]
public class RoomEnemySpawnGroup : ScriptableObject
{

    // RoomEnemySpawnGroup defines a group of enemies that can be spawned in a room.


    [System.Serializable]
    public class EnemySpawnEntry
    {
        public int minEnemyCount;
        public int maxEnemyCount;
        public List<EnemyBase> possibleEnemies;
    }

    public EnemySpawnEntry[] enemySpawnEntries;

}
