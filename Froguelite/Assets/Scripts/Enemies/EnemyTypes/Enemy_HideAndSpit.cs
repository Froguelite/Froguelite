using System.Collections;
using UnityEngine;

public class Enemy_HideAndSpit : EnemyBase
{

    // Enemy_HideAndSpit hides in the foliage and spits projectiles at the player.


    #region VARIABLES


    [Header("Hide and Spit Settings")]
    [SerializeField] private EnemyBehavior_Hide hideBehavior;
    [SerializeField] private Transform spitSpawnTransform;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Projectile.ProjectileData spitProjectileData;
    private bool canSpit = true;


    #endregion


    #region SETUP


    protected override void Awake()
    {
        base.Awake();
        
        // Freeze position to prevent player from pushing this enemy
        GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
    }


    #endregion


    #region ENEMYBASE OVERRIDES


    // On begin player chase, start the chase behavior
    protected override void OnEngagePlayer()
    {
        base.OnEngagePlayer();

        StartCoroutine(HideAndSpitRoutine());
    }


    private IEnumerator HideAndSpitRoutine()
    {
        while (engagedWithPlayer)
        {
            // Hide the enemy
            hideBehavior.Hide();

            // Wait for a few seconds, then pop up again
            yield return new WaitForSeconds(2f);

            hideBehavior.TeleportToPopPos();
            hideBehavior.PlayGroundParticles();

            yield return new WaitForSeconds(1f);

            hideBehavior.PopUp();
            hideBehavior.StopGroundParticles();

            yield return new WaitForSeconds(1.5f);

            if (!canSpit) yield break;
            // Spit a projectile at the player
            Vector2 spitDirection = (PlayerMovement.Instance.GetPlayerCenter() - spitSpawnTransform.position).normalized;

            Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
            newProjectile.InitializeProjectile(spitProjectileData, spitDirection);

            // Wait a bit, then repeat (hide)
            yield return new WaitForSeconds(1.5f);
        }
        
    }


    public override void Die()
    {
        canSpit = false;
        StopCoroutine(HideAndSpitRoutine());
        hideBehavior.SetCanPlayGroundParticles(false);
        hideBehavior.StopGroundParticles();
        base.Die();
    }


    #endregion


}
