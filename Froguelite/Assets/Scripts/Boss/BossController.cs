using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    #region Variables
    public enum State { Idle, AttackPhase, DefensePhase, Death }

    [Header("References")]
    [SerializeField] private Transform visualRoot;            // child used for visual movement (jumping)
    [SerializeField] private Transform islandRoot;            // logical position on island (stay here)
    [SerializeField] private Transform offIslandPivot;        // where the frog moves while off-island
    [SerializeField] private GameObject shadowPrefab;         // visual telegraph at player position
    [SerializeField] private GameObject landingAoePrefab;     // AOE prefab (CircleCollider2D trigger + script)
    [SerializeField] private GameObject tonguePrefab;         // prefab used in Attack2 (long tongue)
    [SerializeField] private GameObject stompAoePrefab;       // Optional visual effect
    [SerializeField] private FlipbookAnimator frogFlipbook; // Frog Animator
    [SerializeField] private BossEntity bossEntity;
    [SerializeField] private Collider2D frogHitbox;         // assign the collider that blocks/contacts the player
    [SerializeField] private GameObject aoeCirclePrefab;

    [Header("Animations")]
    [SerializeField] private Sprite[] idleSprites; // Idle animation
    [SerializeField] private Sprite[] jumpSprites; // Jump Animation
    [SerializeField] private Sprite[] landSprites; // land Animation
    [SerializeField] private Sprite[] WaterSprites; //Swimming Animation
    [SerializeField] private Sprite[] tongueAttackSprites; // Tongue Attack Animation
    [SerializeField] private Sprite[] stompSprites; // Stomp animation
    [SerializeField] private Sprite[] deathSprites; // Death Animation

    [Header("Timings")]
    [SerializeField] private float idleDelay = 1.0f; // start delay

    [Header("Phase 1 (Jump) ATTACK")]
    [SerializeField] private float shadowTelegraph = 1.0f;    // fallback telegraph if overrideDropWait <= 0
    [SerializeField] private float overrideDropWait = 5.0f;   // explicit wait before falling
    [SerializeField] private float dropTravelTime = 0.35f;
    [SerializeField] private float shadowMinScale = 0.3f;
    [SerializeField] private float shadowMaxScale = 1f;
    [SerializeField] private float shadowScaleSpeed = 1f;
    [SerializeField] private float shadowFollowSpeed = 2f; // units per second
    [SerializeField] private float launchTime = 0.18f;
    [SerializeField] private float landingRecovery = 0.25f;
    [SerializeField] private float reenableDelay = 0f;      // optional delay after landing before re-enabling
    [SerializeField] private int maxAttackPhase1Repeats = 5;
    [SerializeField] private int jumpDamage = 1;
    [SerializeField] private float jumpElevation = 0.5f;      // local visual elevation during "in-air"

    [Header("Phase 2 (Tongue) ATTACK")]
    [SerializeField] private float preAttack2Delay = 1.5f;    // longer delay before attack2 starts
    [SerializeField] private float attackPhase2Duration = 6f;
    [SerializeField] private float tongueHoldTime = 1.0f;
    [SerializeField] private float tongueMaxLength = 4f;
    [SerializeField] private float tongueSpawnOffsetX = -0.5f;
    [SerializeField] private float yMin = -10f;
    [SerializeField] private float yMax = 8f;
    [SerializeField] private float verticalPaceSpeed = 0.4f;
    [SerializeField] private float strikeTimeMin = 0.8f;
    [SerializeField] private float strikeTimeMax = 1.5f;
    [SerializeField] private float extendTime = 0.25f;
    [SerializeField] private float retractTime = 0.25f;

    [Header("Phase 3 (Stomp) DEFENSE")]
    [SerializeField] private int stompsPerDefense = 10;        // # of stomps
    [SerializeField] private float stompInterval = 5f;        // Time between stomps
    [SerializeField] private float stompTelegraphTime = 1f;   // Wind-up before stomp
    [SerializeField] private float stompRadius = 3f;          // damage radius
    [SerializeField] private float stompRecoveryTime = 2f;
    [SerializeField] private int stompDamage = 1;            // Damage dealt on stomp
    [SerializeField] private float aoeFadeTime = 0.3f;
    [SerializeField] private float aoeHoldTime = 0.2f;

    private State state = State.Idle;
    private Coroutine stateLoopCoroutine;
    private int attackPhaseCount = 0;
    #endregion
    
    #region Initialization
    private void Reset()
    {
        //animator = GetComponent<Animator>();
    }

    private void Start()
    {
        //if (animator == null) animator = GetComponent<Animator>();
        if (visualRoot == null) visualRoot = transform;
        stateLoopCoroutine = StartCoroutine(StateLoop());
    }

    // Sets up attack and defense loop with states
    //--------------------------------------------//
    private IEnumerator StateLoop()
    //--------------------------------------------//
    {
        if (state == State.Death) yield break;
        
        frogFlipbook.SetSprites(idleSprites, 0.5f, FlipbookLoopMethod.PingPong);
        frogFlipbook.Play();
        yield return new WaitForSeconds(idleDelay);
        state = State.AttackPhase;

        while (true)
        {
            if (state == State.Death) yield break;

            if (state == State.AttackPhase)
            {
                if (attackPhaseCount < maxAttackPhase1Repeats)
                {
                    yield return StartCoroutine(PerformAttack1()); // shadow drop on player
                    attackPhaseCount++;
                }
                else
                {
                attackPhaseCount = 0;
                yield return new WaitForSeconds(preAttack2Delay);
                yield return StartCoroutine(PerformAttack2()); // off-island pacing + tongue
                yield return StartCoroutine(LandOnIsland());
                frogFlipbook.ResetAnimation();
                frogFlipbook.SetSprites(idleSprites, 0.5f, FlipbookLoopMethod.PingPong);
                frogFlipbook.Play();
                    state = State.DefensePhase;
                }
            }
            else if (state == State.DefensePhase)
            {
                yield return StartCoroutine(DefenseRoutine());
                state = State.AttackPhase;
            }
            else
            {
                yield return null;
            }
        }
    }
    #endregion

    #region AttackPhases

    // Jump down attack function
    //--------------------------------------------//
    private IEnumerator PerformAttack1()
    //--------------------------------------------//
    {
        if (state == State.Death) yield break;
        frogFlipbook.ResetAnimation();
        frogFlipbook.SetSprites(jumpSprites, 0.1f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();
        yield return new WaitForSeconds(0.5f);
        Vector3 visualStart = visualRoot.position;

        GameObject shadow = null;
        Transform shadowTransform = null;
        if (shadowPrefab != null)
        {
            Vector3 spawnPos = new Vector3(visualStart.x, visualStart.y, 0f);
            shadow = Instantiate(shadowPrefab, spawnPos, Quaternion.identity, islandRoot);
            shadowTransform = shadow.transform;
            StartCoroutine(ShadowRoutine(shadow));
        }

        // Launch upward
        frogHitbox.enabled = false;
        Vector3 launchApex = visualStart + new Vector3(0f, jumpElevation * 3f, 0f);
        float t = 0f;
        while (t < launchTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / launchTime);
            float eased = Mathf.SmoothStep(0f, 1f, u);
            visualRoot.position = Vector3.Lerp(visualStart, launchApex, eased);

            // shrink shadow as frog goes up
            if (shadowTransform != null)
            {
                float scaleProgress = Mathf.Clamp01(eased * shadowScaleSpeed);
                float scale = Mathf.Lerp(shadowMaxScale, shadowMinScale, scaleProgress);
                shadowTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (state == State.Death) yield break;
            yield return null;
        }

        // Wait while shadow tracks player
        float wait = overrideDropWait > 0f ? overrideDropWait : shadowTelegraph;
        float waited = 0f;
        while (waited < wait)
        {
            waited += Time.deltaTime;
            if (shadowTransform != null)
            {
                // keep shadow small during wait
                shadowTransform.localScale = new Vector3(shadowMinScale, shadowMinScale, 1f);
            }
            if (state == State.Death) yield break;
            yield return null;
        }

        // Fall to shadow
        if (shadowTransform != null)
        {
            Vector3 fallStart = launchApex;
            Vector3 fallTarget = new Vector3(shadowTransform.position.x, shadowTransform.position.y, 0f);
            t = 0f;
            float fallTime = dropTravelTime;
            while (t < fallTime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fallTime);
                float eased = Mathf.SmoothStep(0f, 1f, u);

                fallTarget = shadowTransform.position;
                visualRoot.position = Vector3.Lerp(fallStart, fallTarget, eased);

                // grow shadow back as frog falls
                float scaleProgress = Mathf.Clamp01(eased * shadowScaleSpeed);
                float scale = Mathf.Lerp(shadowMinScale, shadowMaxScale, scaleProgress);
                shadowTransform.localScale = new Vector3(scale, scale, 1f);

                if (state == State.Death) yield break;
                yield return null;
            }
            visualRoot.position = fallTarget;
            frogFlipbook.ResetAnimation();
            frogFlipbook.SetSprites(landSprites, 0.1f, FlipbookLoopMethod.Once);
            frogFlipbook.Play();

            frogHitbox.enabled = true;
            ApplyJumpDamage(fallTarget);

            Destroy(shadow);
        }
        frogFlipbook.ResetAnimation();
        frogFlipbook.SetSprites(idleSprites, 0.5f, FlipbookLoopMethod.PingPong);
        frogFlipbook.Play();
        if (state == State.Death) yield break;
        yield return new WaitForSeconds(landingRecovery);
    }

    // Tongue side swipe attack
    //--------------------------------------------//
    private IEnumerator PerformAttack2()
    {
        Vector3 start = visualRoot.position;
        Vector3 offPivotPos = offIslandPivot != null ? offIslandPivot.position : visualRoot.position + Vector3.up * 3f;

        // Jump out
        frogHitbox.enabled = false;
        frogFlipbook.ResetAnimation();
        frogFlipbook.SetSprites(jumpSprites, 0.1f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();
        float arcOutTime = 0.4f;
        if (state == State.Death) yield break;
        yield return StartCoroutine(ParabolicMove(visualRoot, start, offPivotPos, arcOutTime, jumpElevation * 1.8f));

        SetVisualVisible(true);
        shadowPrefab.GetComponent<SpriteRenderer>().enabled = false;

        frogFlipbook.ResetAnimation();
        frogFlipbook.SetSprites(WaterSprites, 2f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();

        float elapsed = 0f;
        float nextStrikeTime = Random.Range(strikeTimeMin, strikeTimeMax);

        while (elapsed < attackPhase2Duration)
        {
            elapsed += Time.deltaTime;

            // Ping-pong vertical movement between yMin and yMax
            float t = Mathf.PingPong(elapsed * verticalPaceSpeed, 1f);
            float targetY = Mathf.Lerp(yMin, yMax, t);
            Vector3 target = new Vector3(offPivotPos.x, targetY, 0f);
            visualRoot.position = Vector3.Lerp(visualRoot.position, target, Time.deltaTime * 6f);

            // Trigger tongue attack at random time
            if (elapsed >= nextStrikeTime)
            {
                nextStrikeTime = elapsed + Random.Range(strikeTimeMin, strikeTimeMax);
                StartCoroutine(TriggerTongueAttack());
            }

            if (state == State.Death) yield break;
            yield return null;
        }

        if (state == State.Death) yield break;
        yield return new WaitForSeconds(1f);

        // Return to island
        frogHitbox.enabled = true;
        shadowPrefab.GetComponent<SpriteRenderer>().enabled = true;
        frogFlipbook.SetSprites(idleSprites, 0.16f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();

        Vector3 landing = islandRoot.position;
        float arcReturnTime = 0.45f;
        if (state == State.Death) yield break;
        yield return StartCoroutine(ParabolicMove(visualRoot, visualRoot.position, landing, arcReturnTime, jumpElevation * 1.2f));
    }

    private IEnumerator StretchTongue(Transform tongue, float holdTime)
    {
        Vector3 baseScale = tongue.localScale;
        float maxLength = tongueMaxLength;

        // Extend
        float t = 0f;
        while (t < extendTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / extendTime);
            UpdateTongue(tongue, progress * maxLength, baseScale);
            if (state == State.Death) yield break;
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Retract
        t = 0f;
        while (t < retractTime)
        {
            t += Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(t / retractTime);
            UpdateTongue(tongue, progress * maxLength, baseScale);
            if (state == State.Death) yield break;
            yield return null;
        }
    }

    private void UpdateTongue(Transform tongue, float length, Vector3 baseScale)
    {
        var col = tongue.GetComponent<BoxCollider2D>();

        // Scale outward from right pivot
        Vector3 originalPos = tongue.localPosition;
        tongue.localScale = new Vector3(length * baseScale.x, baseScale.y, baseScale.z);
        tongue.localPosition = new Vector3(originalPos.x, originalPos.y, originalPos.z);

        if (col != null)
        {
            col.size = new Vector2(length * baseScale.x, col.size.y);
            col.offset = new Vector2(-length * baseScale.x / 2f, 0f); // collider still needs offset
        }
    }

    private IEnumerator TriggerTongueAttack()
    {
        frogFlipbook.SetSprites(tongueAttackSprites, 0.16f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();
        if (state == State.Death) yield break;
        yield return new WaitForSeconds(0.5f);

        if (tonguePrefab != null)
        {
            GameObject tongue = Instantiate(tonguePrefab, visualRoot.position, Quaternion.identity, visualRoot);
            tongue.transform.localPosition += new Vector3(tongueSpawnOffsetX, 0f, 0f);
            if (state == State.Death) yield break;
            yield return StartCoroutine(StretchTongue(tongue.transform, tongueHoldTime));
            Destroy(tongue);
        }
    }

    #endregion

    #region DefensePhase
    // TO BE IMPLEMENTED (DEFENSE PHASE)
    //--------------------------------------------//
    private IEnumerator DefenseRoutine()
    //--------------------------------------------//
    {
        //animator.SetTrigger("DefenseIdle");

        for (int i = 0; i < stompsPerDefense; i++)
        {
            if (state == State.Death) yield break;
            yield return new WaitForSeconds(stompInterval);
            ShowAOECircle(visualRoot.position);
            yield return new WaitForSeconds(aoeFadeTime + aoeHoldTime);
            frogFlipbook.ResetAnimation();
            frogFlipbook.SetSprites(stompSprites, 0.05f, FlipbookLoopMethod.Once);
            frogFlipbook.Play();
            yield return new WaitForSeconds(stompTelegraphTime);

            StompAoe();
            frogFlipbook.ResetAnimation();
            frogFlipbook.SetSprites(idleSprites, 0.5f, FlipbookLoopMethod.PingPong);
            frogFlipbook.Play();
            yield return new WaitForSeconds(stompRecoveryTime);

        }

        if (state == State.Death) yield break;
        yield return new WaitForSeconds(0.2f);
    }
    #endregion

    #region HelperFunctions
    
    // TO BE IMPLEMENTED
    //--------------------------------------------//
    private void StompAoe()
    //--------------------------------------------//
    {
        int damage = stompDamage;
        if (stompAoePrefab != null)
        {
            Instantiate(stompAoePrefab, transform.position, Quaternion.identity, islandRoot);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stompRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"Player hit by stomp, took {damage} damage.");
                StatsManager.Instance.playerHealth.DamagePlayer(stompDamage);
            }
        }
    }

    private void ApplyJumpDamage(Vector3 center)
    {
        float radius = 1.5f; // or match your AOE prefab scale
        int damage = jumpDamage;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log($"Player hit by jump at {center}, took {damage} damage.");
                StatsManager.Instance.playerHealth.DamagePlayer(damage);
            }
        }
    }

    private void ShowAOECircle(Vector3 position)
    {
        GameObject aoe = Instantiate(aoeCirclePrefab, position, Quaternion.identity);
        aoe.GetComponent<AOECircleFade>().Initialize(aoeFadeTime, aoeHoldTime);
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot == null) return;
        var srs = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.enabled = visible;
        //var anim = visualRoot.GetComponent<Animator>();
        //if (anim != null) anim.enabled = visible;
    }

    // Moves the transform along a 2D parabolic arc from 'from' to 'to' over duration seconds.
    // height is peak additional elevation (local Z offset used for visual elevation).
    //--------------------------------------------//
    private IEnumerator ParabolicMove(Transform target, Vector3 from, Vector3 to, float duration, float height)
    //--------------------------------------------//
    {
        if (state == State.Death) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            // horizontal interpolation
            Vector3 pos = Vector3.Lerp(from, to, u);

            // parabola offset: 4u(1-u) gives peak at u=0.5 with value 1
            float arc = 4f * u * (1f - u);
            float zOffset = height * arc;

            // apply zOffset as visual elevation (keep underlying X/Y on map)
            target.position = new Vector3(pos.x, pos.y, 0f);

            if (state == State.Death) yield break;
            yield return null;
        }
        target.position = new Vector3(to.x, to.y, 0f);
    }
    // Shadow movement towards player
    //--------------------------------------------//
    private IEnumerator ShadowRoutine(GameObject shadow)
    //--------------------------------------------//
    {
        if (state == State.Death) yield break;
        while (shadow != null)
        {
            Vector2 playerPos = PlayerMovement.Instance.transform.position;
            Vector2 current = shadow.transform.position;

            // Move at a fixed speed toward the player
            Vector2 next = Vector2.MoveTowards(current, playerPos, shadowFollowSpeed * Time.deltaTime);
            shadow.transform.position = next;

            if (state == State.Death) yield break;
            yield return null;
        }
    }
    //Jump to starting position
    //--------------------------------------------//
    private IEnumerator LandOnIsland()
    //--------------------------------------------//
    {
        if (state == State.Death) yield break;
        Vector3 landing = islandRoot.position;
        Vector3 start = visualRoot.position;
        float travel = 0.25f;
        float t = 0f;
        while (t < travel)
        {
            t += Time.deltaTime;
            visualRoot.position = Vector3.Lerp(start, landing, t / travel);
            if (state == State.Death) yield break;
            yield return null;
        }
        visualRoot.position = landing;

        yield return null;
    }

    public void Death()
    {
        frogFlipbook.ResetAnimation();
        frogFlipbook.SetSprites(deathSprites, 0.2f, FlipbookLoopMethod.Once);
        frogFlipbook.Play();

        state = State.Death;
        StopAllCoroutines();

        GameManager.Instance.OnWin();
        //-------------------//



        //ADD DEATH LOGIC HERE




        //-------------------//
    }

    public void ForceEnterDefense() => state = State.DefensePhase;
    public void ForceEnterAttack() => state = State.AttackPhase;
    #endregion
}
