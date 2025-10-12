using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossController : MonoBehaviour
{
    #region Variables
    public enum State { Idle, AttackPhase, DefensePhase }

    [Header("References")]
    [SerializeField] private Transform visualRoot;            // child used for visual movement (jumping)
    [SerializeField] private Transform islandRoot;            // logical position on island (stay here)
    [SerializeField] private Transform offIslandPivot;        // where the frog moves while off-island
    [SerializeField] private GameObject shadowPrefab;         // visual telegraph at player position
    [SerializeField] private GameObject landingAoePrefab;     // AOE prefab (CircleCollider2D trigger + script)
    [SerializeField] private GameObject tonguePrefab;         // prefab used in Attack2 (long tongue)
    [SerializeField] private GameObject stompAoePrefab;       // Optional visual effect
    [SerializeField] private Animator animator;
    [SerializeField] private BossEntity bossEntity;
    [SerializeField] private Collider2D frogHitbox;         // assign the collider that blocks/contacts the player

    [Header("Timings")]
    [SerializeField] private float idleDelay = 1.0f;

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

    [Header("Phase 2 (Tongue) ATTACK")]
    [SerializeField] private float preAttack2Delay = 1.5f;    // longer delay before attack2 starts
    [SerializeField] private float offIslandMoveTime = 3.0f;
    [SerializeField] private float tongueHoldTime = 1.0f;
    [SerializeField] private int maxAttackPhase2Repeats = 6;

    [Header("Phase 3 (Stomp) DEFENSE")]
    [SerializeField] private int stompsPerDefense = 10;        // # of stomps
    [SerializeField] private float stompInterval = 5f;        // Time between stomps
    [SerializeField] private float stompTelegraphTime = 1f;   // Wind-up before stomp
    [SerializeField] private float stompRadius = 3f;          // damage radius
    [SerializeField] private float stompRecoveryTime = 2f;
    [SerializeField] private int stompDamage = 10;            // Damage dealt on stomp

    [Header("Movement / Visual")]
    [SerializeField] private float jumpElevation = 0.5f;      // local visual elevation during "in-air"
    [SerializeField] private float offIslandAmplitude = 2f;   // vertical oscillation amount while off-island
    [SerializeField] private float offIslandSpeed = 2f;       // pacing speed off-island
    [SerializeField] private bool hideVisualOffscreen = true; // hide visualRoot when offscreen

    private State state = State.Idle;
    private Coroutine stateLoopCoroutine;
    private int attackPhaseCount = 0;
    #endregion

    #region Initialization
    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (visualRoot == null) visualRoot = transform;
        stateLoopCoroutine = StartCoroutine(StateLoop());
    }

    // Sets up attack and defense loop with states
    //--------------------------------------------//
    private IEnumerator StateLoop()
    //--------------------------------------------//
    {
        yield return new WaitForSeconds(idleDelay);
        state = State.AttackPhase;

        while (true)
        {
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
        animator.SetTrigger("LeapOff");

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

        DisableFrogHitboxForAttack();

        // Launch upward
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

                yield return null;
            }
            visualRoot.position = fallTarget;

            if (landingAoePrefab != null)
                Instantiate(landingAoePrefab, fallTarget, Quaternion.identity, islandRoot);

            ReenableHitboxAfterDelay(reenableDelay);
            Destroy(shadow);
        }

        yield return new WaitForSeconds(landingRecovery);
    }

    // Tongue side swipe attack
    //--------------------------------------------//
    // Attack Phase 2 (with tongue logic inline)
    private IEnumerator PerformAttack2()
    {
        animator.SetTrigger("JumpOffIsland");

        Vector3 start = visualRoot.position;
        Vector3 offPivotPos = offIslandPivot != null ? offIslandPivot.position : visualRoot.position + Vector3.up * 3f;

        DisableFrogHitboxForAttack();

        // Jump out
        float arcOutTime = 0.4f;
        yield return StartCoroutine(ParabolicMove(visualRoot, start, offPivotPos, arcOutTime, jumpElevation * 1.8f));

        SetVisualVisible(true);
        while (attackPhaseCount < maxAttackPhase2Repeats)
        {
            // Pace vertically
            float elapsed = 0f;
            while (elapsed < offIslandMoveTime)
            {
                elapsed += Time.deltaTime;
                Vector3 playerPos = PlayerMovement.Instance.transform.position;
                float offset = Mathf.Sin(elapsed * offIslandSpeed) * offIslandAmplitude;
                Vector3 target = new Vector3(offPivotPos.x, playerPos.y + offset, 0f);
                visualRoot.position = Vector3.Lerp(visualRoot.position, target, Time.deltaTime * 6f);
                yield return null;
            }

            ReenableHitboxAfterDelay(reenableDelay);
            yield return new WaitForSeconds(0.15f);

            // Tongue attack
            animator.SetTrigger("PrepareTongue");

            if (tonguePrefab != null)
            {
                // Spawn tongue sprite as child
                GameObject tongue = Instantiate(tonguePrefab, visualRoot.position, Quaternion.identity, visualRoot);

                // Stretch, hold, retract
                yield return StartCoroutine(StretchTongue(tongue.transform, true, tongueHoldTime));

                Destroy(tongue);
            }

            attackPhaseCount++;
        }
        attackPhaseCount = 0;
        yield return new WaitForSeconds(0.3f);

        // Return to island
        Vector3 landing = islandRoot.position;
        float arcReturnTime = 0.45f;
        yield return StartCoroutine(ParabolicMove(visualRoot, visualRoot.position, landing, arcReturnTime, jumpElevation * 1.2f));
    }

    private IEnumerator StretchTongue(Transform tongue, bool horizontal, float holdTime)
    {
        float maxLength = 6f;
        float extendTime = 0.25f;
        float retractTime = 0.25f;

        // Extend
        float t = 0f;
        while (t < extendTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / extendTime);
            UpdateTongue(tongue, progress * maxLength, horizontal);
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
            UpdateTongue(tongue, progress * maxLength, horizontal);
            yield return null;
        }
    }

    private void UpdateTongue(Transform tongue, float length, bool horizontal)
    {
        var col = tongue.GetComponent<BoxCollider2D>();

        if (horizontal)
        {
            tongue.localScale = new Vector3(length, 1f, 1f);
            tongue.localPosition = new Vector3(-length / 2f, 0f, 0f); // anchored on right edge

            if (col != null)
            {
                col.size = new Vector2(length, col.size.y);
                col.offset = new Vector2(-length / 2f, 0f);
            }
        }
        else
        {
            tongue.localScale = new Vector3(1f, length, 1f);
            tongue.localPosition = new Vector3(0f, length / 2f, 0f);

            if (col != null)
            {
                col.size = new Vector2(col.size.x, length);
                col.offset = new Vector2(0f, length / 2f);
            }
        }
    }

    #endregion

    #region DefensePhase
    // TO BE IMPLEMENTED (DEFENSE PHASE)
    //--------------------------------------------//
    private IEnumerator DefenseRoutine()
    //--------------------------------------------//
    {
        animator.SetTrigger("DefenseIdle");

        for (int i = 0; i < stompsPerDefense; i++)
        {
            yield return new WaitForSeconds(stompInterval);

            animator.SetTrigger("StompCharge"); // Animation trigger
            yield return new WaitForSeconds(stompTelegraphTime);

            animator.SetTrigger("Stomp");
            StompAoe();

            yield return new WaitForSeconds(stompRecoveryTime);

        }

        yield return new WaitForSeconds(0.2f);
    }
    #endregion

    #region HelperFunctions
    // TO BE IMPLEMENTED
    //--------------------------------------------//
    private void StompAoe()
    //--------------------------------------------//
    {
        if (stompAoePrefab != null)
        {
            Instantiate(stompAoePrefab, transform.position, Quaternion.identity, islandRoot);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stompRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("Player hit on stomp");
                // hit.GetComponent<PlayerHealth>()?.TakeDamage(stompDamage);
            }
        }
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot == null) return;
        var srs = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) sr.enabled = visible;
        var anim = visualRoot.GetComponent<Animator>();
        if (anim != null) anim.enabled = visible;
    }

    // Moves the transform along a 2D parabolic arc from 'from' to 'to' over duration seconds.
    // height is peak additional elevation (local Z offset used for visual elevation).
    //--------------------------------------------//
    private IEnumerator ParabolicMove(Transform target, Vector3 from, Vector3 to, float duration, float height)
    //--------------------------------------------//
    {
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

            yield return null;
        }
        target.position = new Vector3(to.x, to.y, 0f);
    }
    // Shadow movement towards player
    //--------------------------------------------//
    private IEnumerator ShadowRoutine(GameObject shadow)
    //--------------------------------------------//
    {
        while (shadow != null)
        {
            Vector2 playerPos = PlayerMovement.Instance.transform.position;
            Vector2 current = shadow.transform.position;

            // Move at a fixed speed toward the player
            Vector2 next = Vector2.MoveTowards(current, playerPos, shadowFollowSpeed * Time.deltaTime);
            shadow.transform.position = next;


            yield return null;
        }
    }
    //Jump to starting position
    //--------------------------------------------//
    private IEnumerator LandOnIsland()
    //--------------------------------------------//
    {
        Vector3 landing = islandRoot.position;
        Vector3 start = visualRoot.position;
        float travel = 0.25f;
        float t = 0f;
        while (t < travel)
        {
            t += Time.deltaTime;
            visualRoot.position = Vector3.Lerp(start, landing, t / travel);
            yield return null;
        }
        visualRoot.position = landing;

        if (hideVisualOffscreen)
        {
            SetVisualVisible(true);
            animator.SetTrigger("Reappear");
        }

        yield return null;
    }

    // TO BE IMPLEMENTED
    //--------------------------------------------//
    private void DisableFrogHitboxForAttack()
    //--------------------------------------------//
    {
        if (frogHitbox != null) frogHitbox.enabled = false;
    }

    // TO BE IMPLEMENTED
    //--------------------------------------------//
    private void ReenableFrogHitboxOnLand()
    //--------------------------------------------//
    {
        if (frogHitbox == null) return;
        if (reenableDelay <= 0f)
        {
            frogHitbox.enabled = true;
            return;
        }
        StartCoroutine(ReenableHitboxAfterDelay(reenableDelay));
    }

    // TO BE IMPLEMENTED
    //--------------------------------------------//
    private IEnumerator ReenableHitboxAfterDelay(float delay)
    //--------------------------------------------//
    {
        yield return new WaitForSeconds(delay);
        if (frogHitbox != null) frogHitbox.enabled = true;
    }

    public void ForceEnterDefense() => state = State.DefensePhase;
    public void ForceEnterAttack() => state = State.AttackPhase;
    #endregion
}
