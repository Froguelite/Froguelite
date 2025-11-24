using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyBehavior_Hide : MonoBehaviour
{

    // EnemyBehavior_Hide handles the hiding behavior of an enemy, allowing it to pop underground, teleport, and pop back up.


    #region VARIABLES


    [System.Serializable]
    public class HideSettings
    {
        [Header("Animation Settings")]
        public float hopHeight = 0.5f;          // How high the enemy hops before going down
        public float hideDepth = 1.5f;            // How far down the sprite goes when hidden
        public float hopDuration = 0.3f;        // Duration of the hop animation
        public float hideDuration = 0.4f;       // Duration of the hide (going down) animation
        public float popUpDuration = 0.4f;      // Duration of the pop up animation
        
        [Header("Teleport Settings")]
        public float teleportDistanceFromPlayer = 3f;  // Distance from player to teleport to
        public int maxTeleportAttempts = 10;     // Maximum attempts to find a valid NavMesh position
        public float navMeshSampleDistance = 2f; // How far to search for nearest NavMesh point
        public LayerMask groundCheckLayer;      // Layer to check for valid ground positions
        public float minDistToOtherEnemy = 2.5f;    // Minimum distance to other hiding enemies when teleporting
    }

    [SerializeField] public HideSettings hideSettings = new HideSettings();
    [SerializeField] protected Transform enemySpriteOffset;
    [SerializeField] protected EnemyBase enemyBase;
    [SerializeField] private ParticleSystem groundParticles;  // Particles to play when hiding or popping up
    [SerializeField] private Collider2D enemyCollider;

    protected Transform player;
    protected Vector3 originalSpriteLocalPosition;
    protected bool isHiding = false;
    protected bool isPopping = false;
    private bool canPlayGroundParticles = true;
    
    public UnityEvent onHideComplete { get; private set; } = new UnityEvent();
    public UnityEvent onPopUpComplete { get; private set; } = new UnityEvent();
    public UnityEvent onTeleportComplete { get; private set; } = new UnityEvent();


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    protected virtual void Awake()
    {
        if (enemySpriteOffset != null)
        {
            originalSpriteLocalPosition = enemySpriteOffset.localPosition;
        }
    }

    protected virtual void Start()
    {
        player = PlayerMovement.Instance?.transform;
    }


    #endregion


    #region HIDE BEHAVIOR


    // Makes the enemy hop up and then hide underground
    public virtual void Hide()
    {
        if (isHiding || isPopping) return;
        
        isHiding = true;
        
        if (enemySpriteOffset == null)
        {
            Debug.LogWarning("EnemyBehavior_Hide: enemySpriteOffset is not assigned!");
            isHiding = false;
            return;
        }

        Vector3 startPos = enemySpriteOffset.localPosition;
        Vector3 hopPos = startPos + Vector3.up * hideSettings.hopHeight;
        Vector3 hidePos = originalSpriteLocalPosition + Vector3.down * hideSettings.hideDepth;

        // Cancel any existing tweens on the sprite
        LeanTween.cancel(enemySpriteOffset.gameObject);

        // First hop up with a bounce ease
        LeanTween.moveLocal(enemySpriteOffset.gameObject, hopPos, hideSettings.hopDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                // Then go down underground with ease in
                groundParticles.Play();
                LeanTween.moveLocal(enemySpriteOffset.gameObject, hidePos, hideSettings.hideDuration)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(() =>
                    {
                        isHiding = false;
                        
                        Invoke("StopGroundParticles", 0.5f);
                        // Disable collider when fully hidden underground
                        if (enemyCollider != null)
                            enemyCollider.enabled = false;
                        onHideComplete.Invoke();
                    });
            });
    }


    // Teleports the enemy to a random position near the player
    public virtual void TeleportToPopPos()
    {
        if (player == null)
        {
            Debug.LogWarning("EnemyBehavior_Hide: Player reference is null!");
            return;
        }

        Vector3 randomPosition = GetRandomPositionAroundPlayer();
        transform.position = randomPosition;
        
        onTeleportComplete.Invoke();
    }


    // Makes the enemy pop up from underground
    public virtual void PopUp()
    {
        if (isHiding || isPopping) return;
        
        isPopping = true;
        
        if (enemySpriteOffset == null)
        {
            Debug.LogWarning("EnemyBehavior_Hide: enemySprite is not assigned!");
            isPopping = false;
            return;
        }

        Vector3 startPos = enemySpriteOffset.localPosition;
        Vector3 popHeight = originalSpriteLocalPosition + Vector3.up * hideSettings.hopHeight;
        Vector3 finalPos = originalSpriteLocalPosition;

        // Cancel any existing tweens on the sprite
        LeanTween.cancel(enemySpriteOffset.gameObject);

        // Enable collider as enemy starts popping up
        if (enemyCollider != null)
            enemyCollider.enabled = true;

        // First pop up with ease out
        LeanTween.moveLocal(enemySpriteOffset.gameObject, popHeight, hideSettings.popUpDuration * 0.6f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                // Then settle down with a slight bounce
                LeanTween.moveLocal(enemySpriteOffset.gameObject, finalPos, hideSettings.popUpDuration * 0.4f)
                    .setEase(LeanTweenType.easeOutBounce)
                    .setOnComplete(() =>
                    {
                        isPopping = false;
                        onPopUpComplete.Invoke();
                    });
            });
    }


    #endregion


    #region HELPER FUNCTIONS


    // Gets a random position around the player at the specified distance
    protected virtual Vector3 GetRandomPositionAroundPlayer()
    {
        if (player == null) return transform.position;

        Vector3 validPosition = transform.position;
        bool foundValidPosition = false;

        // Try multiple times to find a valid NavMesh position
        for (int attempt = 0; attempt < hideSettings.maxTeleportAttempts; attempt++)
        {
            // Generate random angle
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            
            // Calculate position at distance from player
            Vector3 offset = new Vector3(
                Mathf.Cos(randomAngle) * hideSettings.teleportDistanceFromPlayer,
                0f,
                Mathf.Sin(randomAngle) * hideSettings.teleportDistanceFromPlayer
            );

            Vector3 targetPosition = player.position + offset;
            targetPosition.y = transform.position.y;

            // Check if this position is on the NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, hideSettings.navMeshSampleDistance, NavMesh.AllAreas))
            {
                // Check if position is too close to any other EnemyBehavior_Hide instance
                bool tooCloseToOther = false;
                EnemyBehavior_Hide[] allHideEnemies = FindObjectsByType<EnemyBehavior_Hide>(FindObjectsSortMode.None);
                
                foreach (EnemyBehavior_Hide otherEnemy in allHideEnemies)
                {
                    // Skip checking against self
                    if (otherEnemy == this) continue;
                    
                    float distance = Vector3.Distance(hit.position, otherEnemy.transform.position);
                    if (distance < hideSettings.minDistToOtherEnemy)
                    {
                        tooCloseToOther = true;
                        break;
                    }
                }
                
                if (!tooCloseToOther)
                {
                    validPosition = hit.position;
                    foundValidPosition = true;
                    break;
                }
            }
        }

        // If no valid position found after all attempts, stay at current position
        if (!foundValidPosition)
        {
            Debug.LogWarning("EnemyBehavior_Hide: Could not find valid NavMesh position after " + hideSettings.maxTeleportAttempts + " attempts. Staying at current position.");
        }

        return validPosition;
    }


    // Resets the sprite to its original position (useful for cleanup or resets)
    public virtual void ResetSpritePosition()
    {
        if (enemySpriteOffset != null)
        {
            LeanTween.cancel(enemySpriteOffset.gameObject);
            enemySpriteOffset.transform.localPosition = originalSpriteLocalPosition;
            isHiding = false;
            isPopping = false;
        }
        
        // Re-enable collider when resetting
        if (enemyCollider != null)
            enemyCollider.enabled = true;
    }


    #endregion


    #region PARTICLE CONTROLS


    public void PlayGroundParticles()
    {
        if (groundParticles != null && canPlayGroundParticles)
        {
            groundParticles.Play();
        }
    }


    public void StopGroundParticles()
    {
        if (groundParticles != null)
        {
            groundParticles.Stop();
        }
    }


    public void SetCanPlayGroundParticles(bool canPlay)
    {
        canPlayGroundParticles = canPlay;
    }


    #endregion


}
