using System.Collections;
using UnityEngine;

public class Enemy_SwarmAndDash : EnemyBase
{

    // Enemy_SwarmAndDash is an enemy type that utilizes swarm behavior and dashing attacks.


    #region VARIABLES


    public enum SwarmState
    {
        Swarming,
        Dashing,
    }

    [SerializeField] private EnemyBehavior_Swarm swarmBehavior;
    [SerializeField] private Sprite chargeupSprite;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private float maxDistForDash = 5f;

    private SwarmState currentSwarmState = SwarmState.Swarming;


    #endregion


    #region ENEMYBASE OVERRIDES


    // Begin swarming behavior when starting player chase
    protected override void OnEngagePlayer()
    {
        base.OnEngagePlayer();
        swarmBehavior.onTriggerSwarmAction.AddListener(StartDashing);
        swarmBehavior.BeginChase(PlayerMovement.Instance.transform);
        currentSwarmState = SwarmState.Swarming;
    }


    public override void Die()
    {
        swarmBehavior.swarmCenter.RemoveEnemyFromSwarm(swarmBehavior);
        base.Die();
    }


    #endregion


    #region DASHING


    // Starts the dashing behavior
    private void StartDashing()
    {
        if (currentSwarmState == SwarmState.Swarming)
        {
            // If we are not close enough to the player, do nothing
            if (Vector2.Distance(transform.position, PlayerMovement.Instance.transform.position) > maxDistForDash)
                return;

            // Otherwise, trigger the dash
            swarmBehavior.triggeringAction = true;
            swarmBehavior.stopChaseOverride = true;

            currentSwarmState = SwarmState.Dashing;
            StartCoroutine(DashCo());
        }
    }


    // Coroutine to handle dashing behavior
    private IEnumerator DashCo()
    {
        // Step 1: Chargeup
        spriteRenderer.sprite = chargeupSprite;
        //yield return new WaitForSeconds(1f);

        // Step 2: Dash towards player
        navAgent.enabled = false;
        
        Vector2 dashDirection = (PlayerMovement.Instance.transform.position - transform.position).normalized;
        float dashDistance = Vector2.Distance(transform.position, PlayerMovement.Instance.transform.position);
        Vector2 dashMoveAmt = dashDirection * dashDistance * 1.5f;

        transform.LeanMove((Vector2)transform.position + dashMoveAmt, 1.3f).setEaseOutQuad();

        yield return new WaitForSeconds(1.3f);

        // Step 3: Resume swarming
        navAgent.enabled = true;
        spriteRenderer.sprite = defaultSprite;
        currentSwarmState = SwarmState.Swarming;
        swarmBehavior.triggeringAction = false;
        swarmBehavior.stopChaseOverride = false;
    }


    #endregion


}
