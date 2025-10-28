using UnityEngine;
using UnityEngine.Events;

public class EnemyBehavior_Swarm : EnemyBehavior_Chase
{

    // EnemyBehavior_Swarm is an enemy behavior which allows the enemy to follow a "swarm center".


    #region VARIABLES


    [SerializeField] private SwarmManager.SwarmCenter.SwarmInfo swarmInfo = null; // The swarm info for this enemy's swarm

    public SwarmManager.SwarmCenter swarmCenter { get; private set; } = null; // The swarm info for this enemy's swarm
    public UnityEvent onTriggerSwarmAction { get; private set; } = new UnityEvent(); // Event triggered when the swarm action is performed
    public bool triggeringAction = false;
    private bool initialized = false;


    #endregion


    #region SWARM BEHAVIOR


    // Override of what happens when the chase begins for this enemy type
    public override void BeginChase(Transform navTarget)
    {
        InitializeSwarmInfo();
    }


    // Initializes this enemy's swarm info in the SwarmManager
    public void InitializeSwarmInfo()
    {
        if (initialized)
            return;
        initialized = true;

        if (SwarmManager.Instance != null)
        {
            string id = enemyBase.GetEnemyId();
            swarmCenter = SwarmManager.Instance.GetSwarmCenter(id);
            if (swarmCenter == null)
            {
                swarmCenter = SwarmManager.Instance.AddSwarmCentered(swarmInfo, id, enemyBase.parentRoom);
            }

            swarmCenter.AddEnemyToSwarm(this);
            navTarget = swarmCenter.swarmTargetTransform;
            base.BeginChase(navTarget);
        }
        else
        {
            Debug.LogWarning("EnemyBehavior_Swarm: No SwarmManager instance found in scene.");
        }
    }


    #endregion


    #region SWARM ACTION


    public bool ReadyToTriggerSwarmAction()
    {
        // Ensure we are close to the swarm center
        float distanceToCenter = Vector2.Distance(transform.position, swarmCenter.swarmTargetTransform.position);
        return distanceToCenter <= 1.5f && !triggeringAction;
    }


    // Triggers this enemy to perform its swarm action
    public void TriggerSwarmAction()
    {
        onTriggerSwarmAction.Invoke();
    }


    #endregion


}
