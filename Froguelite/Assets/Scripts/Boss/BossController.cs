using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossController : MonoBehaviour
{
    public enum State { Idle, AttackPhase, DefensePhase }

    [Header("References")]
    [SerializeField] private Transform visualRoot;            // child used for visual movement (jumping)
    [SerializeField] private Transform islandRoot;            // logical position on island (stay here)
    [SerializeField] private Transform offIslandPivot;        // where the frog moves while off-island
    [SerializeField] private GameObject shadowPrefab;         // visual telegraph at player position
    [SerializeField] private GameObject landingAoePrefab;     // AOE prefab (CircleCollider2D trigger + script)
    [SerializeField] private GameObject tonguePrefab;         // prefab used in Attack2 (long tongue)
    [SerializeField] private Animator animator;
    [SerializeField] private BossEntity bossEntity;
    [SerializeField] private Collider2D frogHitbox;         // assign the collider that blocks/contacts the player

    [Header("Timings")]
    [SerializeField] private float idleDelay = 1.0f;
    [SerializeField] private float shadowTelegraph = 1.0f;    // fallback telegraph if overrideDropWait <= 0
    [SerializeField] private float overrideDropWait = 5.0f;   // explicit wait before falling
    [SerializeField] private float dropTravelTime = 0.35f;
    [SerializeField] private float offIslandMoveTime = 3.0f;
    [SerializeField] private float preAttack2Delay = 1.5f;    // longer delay before attack2 starts
    [SerializeField] private float tongueHoldTime = 1.0f;
    [SerializeField] private float landingRecovery = 0.25f;
    [SerializeField] private int stompsPerDefense = 3;
    [SerializeField] private float stompInterval = 1.2f;
    [SerializeField] private float hideDelay = 0.25f;
    [SerializeField] private float launchTime = 0.18f;
    [SerializeField] private float shadowInitialStay = 0.6f;      // How long shadow stays at frog's starting position before moving
    [SerializeField] private float shadowMoveDelay = 0.0f;        // Optional extra dealy before starting move towards player
    [SerializeField] private float shadowMoveSpeed = 3.5f;        // Speed used by SmoothDamp while moving towards player
    [SerializeField] private float shadowFollowLerp = 0.12f;      // lerp factor when shadow is following the player
    [SerializeField] private float shadowSnapDistance = 0.12f; // when closer than this we begin the final snap
    [SerializeField] private float shadowFinalLerpTime = 0.12f; // time over which to smoothly finish into the snap
    [SerializeField] private float reenableDelay = 0f;      // optional delay after landing before re-enabling
    [SerializeField] private int maxAttackPhase1Repeats = 5;


    [Header("Movement / Visual")]
    [SerializeField] private float jumpElevation = 0.5f;      // local visual elevation during "in-air"
    [SerializeField] private float offIslandAmplitude = 2f;   // vertical oscillation amount while off-island
    [SerializeField] private float offIslandSpeed = 2f;       // pacing speed off-island
    [SerializeField] private bool hideVisualOffscreen = true; // hide visualRoot when offscreen

    private State state = State.Idle;
    private Coroutine stateLoopCoroutine;
    private int attackPhaseCount = 0;

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

    private IEnumerator StateLoop()
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
                yield return new WaitForSeconds(preAttack2Delay);
                yield return StartCoroutine(PerformAttack2()); // off-island pacing + tongue
                yield return StartCoroutine(LandOnIsland());
                attackPhaseCount = 0;
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

    private IEnumerator PerformAttack1()
    {
        animator.SetTrigger("LeapOff");

        Vector3 visualStart = visualRoot.position;

        GameObject shadow = null;
        if (shadowPrefab != null)
        {
            Vector3 spawnPos = new Vector3(visualStart.x, visualStart.y, 0f);
            shadow = Instantiate(shadowPrefab, spawnPos, Quaternion.identity, islandRoot);
            StartCoroutine(ShadowRoutine(shadow));
        }

        DisableFrogHitboxForAttack(); //disabled hitbox

        // FAST upward launch: move visualRoot quickly upward so it looks like it jumps off-screen
        Vector3 launchApex = visualStart + new Vector3(0f, jumpElevation * 3f, 0f); // larger visual elevation for quick launch

        float t = 0f;
        while (t < launchTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / launchTime);
            float eased = Mathf.SmoothStep(0f, 1f, u); // smooth, slower feel by increasing launchTime
            visualRoot.position = Vector3.Lerp(visualStart, launchApex, eased);
            yield return null;
        }

        yield return new WaitForSeconds(hideDelay); //Sets dissapear delay

        // Hide the visual so it appears to leave screen (shadow remains)
        if (hideVisualOffscreen)
            SetVisualVisible(false);

        // Wait the explicit drop wait while updating shadow to follow the player
        float wait = overrideDropWait > 0f ? overrideDropWait : shadowTelegraph;
        float waited = 0f;
        while (waited < wait)
        {
            waited += Time.deltaTime;
            if (shadow != null)
            {
                Vector3 p = PlayerMovement.Instance.transform.position;
                shadow.transform.position = new Vector3(p.x, p.y, 0f);
            }
            yield return null;
        }

        // Fall to shadow position (fast fall)
        if (shadow != null)
        {
            Vector3 fallStart = visualRoot.position;
            Vector3 fallTarget = new Vector3(shadow.transform.position.x, shadow.transform.position.y, 0f);
            t = 0f;
            float fallTime = dropTravelTime; // keep your tuned fall duration
            while (t < fallTime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fallTime);
                float eased = Mathf.SmoothStep(0f, 1f, u);
                visualRoot.position = Vector3.Lerp(fallStart, fallTarget, u);
                yield return null;
            }
            visualRoot.position = fallTarget;

            // Spawn AOE on landing
            if (landingAoePrefab != null)
            {
                Instantiate(landingAoePrefab, new Vector3(fallTarget.x, fallTarget.y, 0f), Quaternion.identity, islandRoot);
            }

            ReenableHitboxAfterDelay(reenableDelay); //reenables hitbox

            Destroy(shadow);
        }

        // Ensure visual is shown on landing with small reappear animation
        if (hideVisualOffscreen)
        {
            SetVisualVisible(true);
            animator.SetTrigger("Reappear");
        }

        yield return new WaitForSeconds(landingRecovery);
    }

    private IEnumerator PerformAttack2()
    {
        animator.SetTrigger("JumpOffIsland");

        Vector3 start = visualRoot.position;
        Vector3 offPivotPos = offIslandPivot != null ? offIslandPivot.position : visualRoot.position + Vector3.up * 3f;

        DisableFrogHitboxForAttack();

        // Jump out following a parabolic arc from island to offIslandPivot
        float arcOutTime = 0.4f; // travel time along arc outward
        yield return StartCoroutine(ParabolicMove(visualRoot, start, offPivotPos, arcOutTime, jumpElevation * 1.8f));

        SetVisualVisible(true);

        // 2a) Pace vertical relative to player while off-island: keep X fixed at offPivot.x, Y follows player.y + oscillation
        float elapsed = 0f;
        while (elapsed < offIslandMoveTime)
        {
            elapsed += Time.deltaTime;
            Vector3 playerPos = PlayerMovement.Instance.transform.position;
            float offset = Mathf.Sin(elapsed * offIslandSpeed) * offIslandAmplitude;
            Vector3 target = new Vector3(offPivotPos.x, playerPos.y + offset, 0f);
            visualRoot.position = Vector3.Lerp(visualRoot.position, target, Time.deltaTime * 6f); // smooth follow vertically only
            yield return null;
        }

        ReenableHitboxAfterDelay(reenableDelay);
        // Short pause before tongue
        yield return new WaitForSeconds(0.15f);

        // 3) Stop, aim, spawn tongue from off-island area (not centered on player)
        animator.SetTrigger("PrepareTongue");
        GameObject tongue = null;
        if (tonguePrefab != null)
        {
            Vector3 tongueSpawnPos = visualRoot.position;
            tongue = Instantiate(tonguePrefab, tongueSpawnPos, Quaternion.identity, transform);
            var tb = tongue.GetComponent<TongueBehaviour>();
            if (tb != null)
            {
                bool horizontal = true; // stretch across island horizontally by default
                yield return StartCoroutine(tb.ExtendAndHold(horizontal, tongueHoldTime));
            }
            else
            {
                yield return new WaitForSeconds(tongueHoldTime);
            }
            Destroy(tongue);
        }

        yield return new WaitForSeconds(0.3f);

        // Return to island following a parabolic arc back to islandRoot
        Vector3 landing = islandRoot.position;
        float arcReturnTime = 0.45f;
        yield return StartCoroutine(ParabolicMove(visualRoot, visualRoot.position, landing, arcReturnTime, jumpElevation * 1.2f));
    }


    private IEnumerator LandOnIsland()
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

    private IEnumerator DefenseRoutine()
    {
        animator.SetTrigger("DefenseIdle");
        for (int i = 0; i < stompsPerDefense; i++)
        {
            yield return new WaitForSeconds(stompInterval);
            animator.SetTrigger("Stomp");
            StompAoe();
        }
        yield return new WaitForSeconds(0.2f);
    }

    private void StompAoe()
    {
        if (landingAoePrefab != null)
        {
            Instantiate(landingAoePrefab, new Vector3(islandRoot.position.x, islandRoot.position.y, 0f), Quaternion.identity, islandRoot);
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
    private IEnumerator ParabolicMove(Transform target, Vector3 from, Vector3 to, float duration, float height)
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

    private IEnumerator ShadowRoutine(GameObject shadow)
    {
        // initial wait
        float waited = 0f;
        while (waited < shadowInitialStay)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        if (shadowMoveDelay > 0f)
            yield return new WaitForSeconds(shadowMoveDelay);

        // main loop: MoveTowards until close, then smoothly lerp into final snap
        while (shadow != null)
        {
            Vector2 playerPos = PlayerMovement.Instance.transform.position;
            Vector2 current = shadow.transform.position;
            float dist = Vector2.Distance(current, playerPos);

            if (dist > shadowSnapDistance)
            {
                // capped per-frame move so shadow visibly drags
                float maxStep = shadowMoveSpeed * Time.deltaTime;
                Vector2 next = Vector2.MoveTowards(current, playerPos, maxStep);
                shadow.transform.position = next;
                yield return null;
                continue;
            }

            // within snap distance: do a short smooth finish to avoid an abrupt jump
            float elapsed = 0f;
            Vector2 start = current;
            while (elapsed < shadowFinalLerpTime && shadow != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shadowFinalLerpTime);
                // smooth step for pleasing curve
                float eased = Mathf.SmoothStep(0f, 1f, t);
                shadow.transform.position = Vector2.Lerp(start, playerPos, eased);
                yield return null;
            }

            // final snap (keeps behavior deterministic) and keep following with slight lerp so it won't jitter
            if (shadow != null)
            {
                shadow.transform.position = playerPos;
                // small follow loop to maintain the shadow following after snap; keep it soft with Lerp
                while (shadow != null)
                {
                    Vector2 cur = shadow.transform.position;
                    Vector2 desired = PlayerMovement.Instance.transform.position;
                    shadow.transform.position = Vector2.Lerp(cur, desired, shadowFollowLerp);
                    yield return null;
                }
            }
        }
    }

    private void DisableFrogHitboxForAttack()
    {
        if (frogHitbox != null) frogHitbox.enabled = false;
    }

    private void ReenableFrogHitboxOnLand()
    {
        if (frogHitbox == null) return;
        if (reenableDelay <= 0f)
        {
            frogHitbox.enabled = true;
            return;
        }
        StartCoroutine(ReenableHitboxAfterDelay(reenableDelay));
    }

    private IEnumerator ReenableHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (frogHitbox != null) frogHitbox.enabled = true;
    }

    public void ForceEnterDefense() => state = State.DefensePhase;
    public void ForceEnterAttack() => state = State.AttackPhase;
}
