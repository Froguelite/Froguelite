using UnityEngine;

public class Enemy_Chase : EnemyBase
{

    // EnemyChase is a simple "chase and melee" enemy type that pursues the player.


    #region VARIABLES


    [Header("Chase Settings")]
    [SerializeField] private EnemyBehavior_Chase chaseBehavior;


    #endregion


    #region ENEMYBASE OVERRIDES


    // On begin player chase, start the chase behavior
    public override void BeginPlayerChase()
    {
        base.BeginPlayerChase();

        if (chaseBehavior != null && PlayerMovement.Instance != null)
        {
            chaseBehavior.BeginChase(PlayerMovement.Instance.transform);
        }
    }


    #endregion


}
