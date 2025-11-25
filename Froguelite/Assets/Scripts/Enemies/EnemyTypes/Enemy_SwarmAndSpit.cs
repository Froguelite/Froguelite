using System.Collections;
using UnityEngine;

public class Enemy_SwarmAndSpit : EnemyBase
{

    // Enemy_SwarmAndSpit is an enemy type that utilizes swarm behavior and spitting attacks.


    #region VARIABLES


    [SerializeField] private EnemyBehavior_Swarm swarmBehavior;
    [SerializeField] private Sprite spittingSprite;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Projectile.ProjectileData spitProjectileData;
    [SerializeField] private Transform spitSpawnTransform;


    #endregion


    #region ENEMYBASE OVERRIDES


    // Begin swarming behavior when starting player chase
    protected override void OnEngagePlayer()
    {
        if (isDead)
            return;
            
        base.OnEngagePlayer();
        swarmBehavior.onTriggerSwarmAction.AddListener(StartSpitting);
        swarmBehavior.BeginChase(PlayerMovement.Instance.transform);
    }


    public override void Die()
    {
        if (swarmBehavior.swarmCenter != null)
            swarmBehavior.swarmCenter.RemoveEnemyFromSwarm(swarmBehavior);
        
        base.Die();
    }


    #endregion


    #region SPITTING


    // Starts the spitting behavior
    private void StartSpitting()
    {
        swarmBehavior.triggeringAction = true;
        StartCoroutine(SpitCo());
    }


    // Coroutine to handle spitting behavior
    private IEnumerator SpitCo()
    {
        spriteRenderer.sprite = spittingSprite;

        Vector2 spitDirection = (PlayerMovement.Instance.GetPlayerCenter() - spitSpawnTransform.position).normalized;

        Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
        newProjectile.InitializeProjectile(spitProjectileData, spitDirection);

        yield return new WaitForSeconds(1f);

        spriteRenderer.sprite = defaultSprite;
        swarmBehavior.triggeringAction = false;
    }


    #endregion


}
