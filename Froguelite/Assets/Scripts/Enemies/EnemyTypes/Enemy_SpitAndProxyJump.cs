using System.Collections;
using UnityEngine;

public class Enemy_SpitAndProxyJump : EnemyBase
{

    // Enemy_SpitAndProxyJump is an enemy type that utilizes spitting attacks and proximity jumping.


    #region VARIABLES


    [SerializeField] private Sprite spittingSprite;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite jumpingSprite;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Projectile.ProjectileData spitProjectileData;
    [SerializeField] private Transform spitSpawnTransform;
    [SerializeField] private float durationBetweenSpits = 1f;
    [SerializeField] private float durationBetweenJumps = 1f;

    private bool overlapped = false;
    private bool jumping = false;
    private Vector2 arcTarget; 
    private float arcJumpDuration;

    private IEnumerator spitCoroutine;
    private IEnumerator jumpCoroutine;


    #endregion


    #region ENEMYBASE OVERRIDES


    // Begin swarming behavior when starting player chase
    public override void BeginPlayerChase()
    {
        base.BeginPlayerChase();
        spitCoroutine = SpitCo();
        jumpCoroutine = JumpCo();
        StartCoroutine(spitCoroutine);
    }


    public override void Die()
    {
        StopCoroutine(spitCoroutine);
        StopCoroutine(jumpCoroutine);
        StopCoroutine(PerformArcJump());
        base.Die();
    }


    #endregion


    #region SPITTING


    // Coroutine to handle spitting behavior
    private IEnumerator SpitCo()
    {
        while (true)
        {
            yield return new WaitForSeconds(durationBetweenSpits);

            spriteRenderer.sprite = spittingSprite;

            Vector2 spitDirection = (PlayerMovement.Instance.GetPlayerCenter() - spitSpawnTransform.position).normalized;

            Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
            newProjectile.InitializeProjectile(spitProjectileData, spitDirection);

            yield return new WaitForSeconds(0.4f);
            spriteRenderer.sprite = defaultSprite;
        }
    }


    #endregion


    #region JUMPING


    // Coroutine to handle jumping behavior
    private IEnumerator JumpCo()
    {
        while (true)
        {
            // Change to jumping sprite
            spriteRenderer.sprite = jumpingSprite;

            // Calculate jump direction towards player
            Vector2 jumpDirection = ((Vector2)PlayerMovement.Instance.transform.position - (Vector2)transform.position).normalized;
            Vector2 jumpTarget = (Vector2)transform.position + jumpDirection * 4f;

            // Perform the jump with arc
            arcTarget = jumpTarget;
            arcJumpDuration = 0.5f;
            yield return StartCoroutine(PerformArcJump());

            // Revert to default sprite
            spriteRenderer.sprite = defaultSprite;

            // Wait before next jump
            yield return new WaitForSeconds(durationBetweenJumps);
        }
    }

    // Coroutine to perform an arcing jump to the target position
    private IEnumerator PerformArcJump()
    {
        Vector2 startPosition = transform.position;
        float elapsedTime = 0f;
        
        // Calculate the arc height (you can adjust this value)
        float arcHeight = 2f;

        while (elapsedTime < arcJumpDuration)
        {
            float progress = elapsedTime / arcJumpDuration;
            
            // Linear interpolation for horizontal movement
            Vector2 currentPosition = Vector2.Lerp(startPosition, arcTarget, progress);

            // Parabolic arc for vertical movement
            // This creates an arc that goes up and then down
            float arcOffset = arcHeight * 4 * progress * (1 - progress);
            currentPosition.y += arcOffset;
            
            transform.position = currentPosition;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure we end exactly at the target position
        transform.position = arcTarget;
    }


    #endregion


    #region TRIGGER


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            overlapped = true;
            if (!jumping)
            {
                jumping = true;
                StopCoroutine(spitCoroutine);
                StartCoroutine(jumpCoroutine);
            }
        }
    }


    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            overlapped = false;
        }
    }


    #endregion


}
