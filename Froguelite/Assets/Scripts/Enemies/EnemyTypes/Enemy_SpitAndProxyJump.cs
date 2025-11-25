using System.Collections;
using UnityEngine;

public class Enemy_SpitAndProxyJump : EnemyBase
{

    // Enemy_SpitAndProxyJump is an enemy type that utilizes spitting attacks and proximity jumping.

    public enum RapidFireMode
    {
        BackToBack,      // Fire projectiles in sequence with delay
        Simultaneous     // Fire all projectiles at once with spread angle
    }

    #region VARIABLES


    [SerializeField] private Sprite spittingSprite;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite jumpingSprite;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Projectile.ProjectileData spitProjectileData;
    [SerializeField] private Transform spitSpawnTransform;
    [SerializeField] private float durationBetweenSpits = 1f;
    [SerializeField] private float durationBetweenJumps = 1f;
    [SerializeField] private float jumpDistance = 3f;
    [SerializeField] private float arcJumpDuration = 0.5f;
    [SerializeField] private bool triggerProxyJump = true;
    [SerializeField] private bool useRapidFire = false;
    [SerializeField] private int projectilesToSpit = 3;
    [SerializeField] private RapidFireMode rapidFireMode = RapidFireMode.BackToBack;
    [SerializeField] private float timeBetweenRapidShots = 0.2f;
    [SerializeField] private float spreadAngleDegrees = 15f;

    private bool overlapped = false;
    private bool jumping = false;
    private Vector2 arcTarget; 

    private IEnumerator spitCoroutine;
    private IEnumerator jumpCoroutine;


    #endregion


    #region ENEMYBASE OVERRIDES


    // Begin swarming behavior when starting player chase
    protected override void OnEngagePlayer()
    {
        base.OnEngagePlayer();
        spitCoroutine = SpitCo();
        jumpCoroutine = JumpCo();

        if (overlapped)
        {
            jumping = true;
            StartCoroutine(jumpCoroutine);
        }
        else
        {
            StartCoroutine(spitCoroutine);
        }
    }


    public override void Die()
    {
        if (spitCoroutine != null)
            StopCoroutine(spitCoroutine);
        if (jumpCoroutine != null)
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

            if (useRapidFire)
            {
                if (rapidFireMode == RapidFireMode.BackToBack)
                {
                    // Fire projectiles one after another with delay
                    for (int i = 0; i < projectilesToSpit; i++)
                    {
                        Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
                        newProjectile.InitializeProjectile(spitProjectileData, spitDirection);
                        
                        if (i < projectilesToSpit - 1) // Don't wait after the last shot
                        {
                            yield return new WaitForSeconds(timeBetweenRapidShots);
                        }
                    }
                }
                else // RapidFireMode.Simultaneous
                {
                    // Fire all projectiles at once with spread
                    for (int i = 0; i < projectilesToSpit; i++)
                    {
                        // Calculate angle offset for this projectile
                        float angleOffset = 0f;
                        
                        if (projectilesToSpit > 1)
                        {
                            // Distribute projectiles evenly across the spread angle
                            float totalSpread = spreadAngleDegrees * 2;
                            angleOffset = -spreadAngleDegrees + (totalSpread / (projectilesToSpit - 1)) * i;
                        }
                        
                        // Rotate the direction by the angle offset
                        float angleInRadians = angleOffset * Mathf.Deg2Rad;
                        float cos = Mathf.Cos(angleInRadians);
                        float sin = Mathf.Sin(angleInRadians);
                        Vector2 rotatedDirection = new Vector2(
                            spitDirection.x * cos - spitDirection.y * sin,
                            spitDirection.x * sin + spitDirection.y * cos
                        );
                        
                        Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
                        newProjectile.InitializeProjectile(spitProjectileData, rotatedDirection);
                    }
                }
            }
            else
            {
                Projectile newProjectile = Instantiate(projectilePrefab, spitSpawnTransform.position, Quaternion.identity);
                newProjectile.InitializeProjectile(spitProjectileData, spitDirection);
            }

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
            Vector2 jumpTarget = (Vector2)transform.position + jumpDirection * jumpDistance;

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

            if (!engagedWithPlayer) return;

            if (!triggerProxyJump) return;

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
