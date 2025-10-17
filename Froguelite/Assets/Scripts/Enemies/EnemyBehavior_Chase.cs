using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior_Chase : MonoBehaviour
{

    // EnemyBehavior_Chase handles the chasing behavior of an enemy towards a target, typically the player.


    #region VARIABLES


    [System.Serializable]
    public class ChaseSettings
    {
        public float chaseSpeed = 3f;
        public float stoppingDistance = 1.5f;
        public float chaseUpdateInterval = 0.5f;
    }

    [SerializeField] public ChaseSettings chaseSettings = new ChaseSettings();
    [SerializeField] private NavMeshAgent navAgent;
    [SerializeField] private EnemyBase enemyBase;
    private Transform navTarget;
    public bool chasingPlayer { get; private set; } = false;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Begins a chase with the given chase settings
    public void BeginChase(Transform navTarget, ChaseSettings chaseSettings)
    {
        this.chaseSettings = chaseSettings;
        BeginChase(navTarget);
    }


    // Begins a chase with current chase settings
    public void BeginChase(Transform navTarget)
    {
        this.navTarget = navTarget;
        chasingPlayer = true;
        navAgent.speed = chaseSettings.chaseSpeed;
        navAgent.stoppingDistance = chaseSettings.stoppingDistance;
    }


    // Update, chase if active
    void Update()
    {
        if (chasingPlayer && !enemyBase.isKnockedBack && enemyBase.engagedWithPlayer)
        {
            NavFullChase();
        }
    }


    #endregion


    #region CHASE


    // Continuously chases the player directly
    public void NavFullChase()
    {
        if (navTarget == null) return;

        Vector3 targetPosition = navTarget.position;
        navAgent.SetDestination(targetPosition);
    }


    #endregion


}
