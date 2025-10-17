using UnityEngine;

public class EnemyBehavior_Swarm : EnemyBehavior_Chase
{

    // EnemyBehavior_Swarm is an enemy behavior which allows the enemy to follow a "swarm center".


    #region VARIABLES


    [SerializeField] private SwarmManager.SwarmCenter.SwarmInfo swarmInfo = null; // The swarm info for this enemy's swarm

    private SwarmManager.SwarmCenter swarmCenter = null; // The swarm info for this enemy's swarm


    #endregion


    #region SWARM BEHAVIOR


    // Override of what happens when the chase begins for this enemy type
    public override void BeginChase(Transform navTarget)
    {
        InitializeSwarmInfo();
    }


    // Initializes this enemy's swarm info in the SwarmManager
    private void InitializeSwarmInfo()
    {
        if (SwarmManager.Instance != null)
        {
            string id = enemyBase.GetEnemyId();
            swarmCenter = SwarmManager.Instance.GetSwarmCenter(id);
            if (swarmCenter == null)
            {
                swarmCenter = SwarmManager.Instance.AddSwarmCentered(swarmInfo, id, enemyBase.parentRoom);
            }

            swarmCenter.AddEnemyToSwarm();
            navTarget = swarmCenter.swarmTargetTransform;
            base.BeginChase(navTarget);
        }
        else
        {
            Debug.LogWarning("EnemyBehavior_Swarm: No SwarmManager instance found in scene.");
        }
    }


    #endregion


}
