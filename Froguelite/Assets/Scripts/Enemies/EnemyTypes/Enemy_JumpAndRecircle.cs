using System.Collections;
using UnityEngine;

public class Enemy_JumpAndRecircle : EnemyBase
{

    // EnemyJumpAndRecircle is an enemy type that chases, jumps at, recircles, and repeats.


    #region VARIABLES


    [Header("Chase Settings")]
    [SerializeField] private EnemyBehavior_Chase chaseBehavior;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite jumpingSprite;
    [SerializeField] private FaceTargetFlipper faceTargetFlipper;
    [SerializeField] private float jumpDistance = 6f;
    [SerializeField] private float arcJumpDuration = 1f;
    
    private Vector2 arcTarget;

    private GameObject crocTarget;


    #endregion


    #region ENEMYBASE OVERRIDES


    // On begin player chase, start the chase behavior
    public override void BeginPlayerChase()
    {
        base.BeginPlayerChase();
        chaseBehavior.BeginChase(PlayerMovement.Instance.transform);
        StartCoroutine(ChaseAndRecircleCo());
    }


    public override void Die()
    {
        StopAllCoroutines();
        if (crocTarget != null)
            Destroy(crocTarget);
        base.Die();
    }


    private IEnumerator ChaseAndRecircleCo()
    {
        while (!isDead)
        {
            // Step 1: Chase towards the player and get near
            chaseBehavior.SetNavTarget(PlayerMovement.Instance.transform);

            yield return null;

            bool reachedDestination = false;
            chaseBehavior.onDestinationReached.AddListener(() => reachedDestination = true);

            while (!reachedDestination && !isDead)
            {
                yield return null;
            }

            chaseBehavior.onDestinationReached.RemoveAllListeners();

            if (isDead)
                yield break;

            // Step 2: Perform a jump towards the player
            spriteRenderer.sprite = jumpingSprite;

            Vector2 jumpDirection = ((Vector2)PlayerMovement.Instance.transform.position - (Vector2)transform.position).normalized;
            Vector2 jumpTarget = (Vector2)transform.position + jumpDirection * jumpDistance;

            arcTarget = jumpTarget;
            yield return StartCoroutine(PerformArcJump());

            // Step 3: Run away from the player
            spriteRenderer.sprite = defaultSprite;
            faceTargetFlipper.SwapReverseFlip();
            faceTargetFlipper.SetFacingTarget(false);

            Vector2 recircleDirection = ((Vector2)transform.position - (Vector2)PlayerMovement.Instance.transform.position).normalized;
            Vector2 recircleTarget = (Vector2)transform.position + recircleDirection * 16f;

            crocTarget = new GameObject("CrocTarget");
            crocTarget.transform.position = recircleTarget;
            chaseBehavior.SetNavTarget(crocTarget.transform);

            yield return new WaitForSeconds(2.5f);

            Destroy(crocTarget);
            faceTargetFlipper.SwapReverseFlip();
            faceTargetFlipper.SetFacingTarget(true);
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


}
