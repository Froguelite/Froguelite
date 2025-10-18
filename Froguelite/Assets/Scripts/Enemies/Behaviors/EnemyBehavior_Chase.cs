using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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
    [SerializeField] protected NavMeshAgent navAgent;
    [SerializeField] protected EnemyBase enemyBase;
    protected Transform navTarget;
    public bool isChasing { get; private set; } = false;

    public bool stopChaseOverride = false; // If true, stops the chase regardless of other conditions
    public UnityEvent onDestinationReached { get; private set; } = new UnityEvent();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Begins a chase with current chase settings
    public virtual void BeginChase(Transform navTarget)
    {
        this.navTarget = navTarget;
        isChasing = true;
        navAgent.speed = chaseSettings.chaseSpeed;
        navAgent.stoppingDistance = chaseSettings.stoppingDistance;
    }


    // Update, chase if active
    protected virtual void Update()
    {
        if (!isChasing || navTarget == null || stopChaseOverride)
            return;

        if (enemyBase == null)
        {
            ChaseTarget();
            return;
        }

        if (!enemyBase.isKnockedBack && enemyBase.engagedWithPlayer)
        {
            ChaseTarget();
        }
    }


    public void SetNavTarget(Transform newTarget)
    {
        navTarget = newTarget;
    }


    #endregion


    #region CHASE


    // Continuously chases the player directly
    public void ChaseTarget()
    {
        if (navTarget == null) return;

        Vector3 targetPosition = navTarget.position;
        navAgent.SetDestination(targetPosition);

        if (navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            onDestinationReached.Invoke();
        }
    }


    #endregion


}
