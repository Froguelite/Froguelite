using UnityEngine;

public class EnemyFactory : MonoBehaviour
{
    
    // EnemyFactory handles the creation and spawning of enemies in rooms


    #region VARIABLES

    
    public static EnemyFactory Instance { get; private set; }

    [SerializeField] private EnemyBase enemyBasePrefab;


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
    }


    #endregion


    #region SPAWNING


    // Spawns a random enemy at the specified position
    public IEnemy SpawnRandomEnemy(Room parentRoom, Vector2 position)
    {
        EnemyBase newEnemy = Instantiate(enemyBasePrefab, position, Quaternion.identity, parentRoom.transform);
        newEnemy.InitializeEnemy(parentRoom);
        return newEnemy;
    }
    

    #endregion


}
