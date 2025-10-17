using UnityEngine;

public class Enemy_SwarmAndDash : EnemyBase
{

    // Enemy_SwarmAndDash is an enemy type that utilizes swarm behavior and dashing attacks.


    #region VARIABLES


    [SerializeField] private EnemyBehavior_Swarm swarmBehavior;


    #endregion


    #region ENEMYBASE OVERRIDES


    public override void BeginPlayerChase()
    {
        base.BeginPlayerChase();
        swarmBehavior.BeginChase(PlayerMovement.Instance.transform);
    }


    #endregion

    
}
