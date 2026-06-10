using System.Collections;
using UnityEngine;

public class boss : MonoBehaviour
{
    [Header("Patrol settings")]
    [SerializeField] private float speed = 2f;

    [Header("Timing settings")]
    [Tooltip("Minimum time (seconds) the boss will walk continuously")] [SerializeField] private float minWalkTime = 1f;
    [Tooltip("Maximum time (seconds) the boss will walk continuously")] [SerializeField] private float maxWalkTime = 3f;
    [Tooltip("Chance (0..1) after finishing a walking period to go idle instead of turning")] [SerializeField] [Range(0f, 1f)] private float idleChance = 0.3f;
    [Tooltip("Minimum idle time (seconds) when the boss chooses to stand still")] [SerializeField] private float minIdleTime = 0.5f;
    [Tooltip("Maximum idle time (seconds) when the boss chooses to stand still")] [SerializeField] private float maxIdleTime = 1.5f;

    [Header("Obstacle detection")]
    [Tooltip("Transform positioned at the front of the boss to test for walls/obstacles")]
    [SerializeField] private Transform frontCheck;
    [Tooltip("Radius used for overlap check at the frontCheck position")]
    [SerializeField] private float checkRadius = 0.1f;
    [Tooltip("Layers considered obstacles (leave default to check all)")]
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Header("Attack settings")]
    [Tooltip("Number of damage events required to trigger the attack animation")]
    [SerializeField] private int hitsToTrigger = 2;
    [Tooltip("Time to wait (seconds) before firing the Attack trigger after being hit")]
    [SerializeField] private float preAttackDelay = 0.05f;
    [Tooltip("Time to remain idle (seconds) after the Attack animation ends")]
    [SerializeField] private float postAttackDelay = 0.05f;

    // Chance to randomly perform an attack when a walking/idle period ends
    [Tooltip("Chance (0..1) to start the attack when a walk or idle period finishes")]
    [SerializeField] [Range(0f, 1f)] private float idleAttackChance = 0f;

    // Clap visual prefab and fade settings
    [Tooltip("Prefab to spawn for clap1 (should contain a SpriteRenderer and optional Light)")]
    [SerializeField] private GameObject clapPrefab;
    [Tooltip("Local position of spawned clap relative to boss")]
    [SerializeField] private Vector3 clapLocalPosition = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Time to fade in the clap visual")]
    [SerializeField] private float clapFadeInDuration = 0.15f;
    [Tooltip("Time to fade out the clap visual when attack ends")]
    [SerializeField] private float clapFadeOutDuration = 0.25f;

    [Header("Projectile settings")]
    [Tooltip("Projectile prefab spawned by clap events")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Maximum radius around boss to spawn the projectile (randomized)")]
    [SerializeField] private float projectileSpawnRadius = 0.6f;

    [Header("Teleport settings")]
    [Tooltip("Possible teleport locations for the boss; designer-placed Transforms")]
    [SerializeField] private Transform[] teleportPoints;

    private Rigidbody2D rb;
    private Animator animator;
    private bool movingRight = true;

    private int hitCount = 0;

    private enum State { Walking, Idle, Attacking }
    private State state = State.Walking;
    private float stateTimer = 0f;

    // attack timing
    private float attackTimer = 0f;
    private bool attackTriggered = false; // true after animator trigger fired, waiting for animation event

    // spawned clap instance
    private GameObject spawnedClap;
    private Light spawnedClapLight;
    private float spawnedClapLightOriginalIntensity = 1f;

    // track last teleport to avoid picking same spot twice
    private int lastTeleportIndex = -1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Start with a random direction and walking duration
        movingRight = Random.value > 0.5f;
        state = State.Walking;
        stateTimer = Random.Range(minWalkTime, maxWalkTime);

        if (rb == null)
        {
            Debug.LogError("boss requires a Rigidbody2D component. Please add one to the GameObject.");
        }

        if (frontCheck == null)
        {
            Debug.LogWarning("boss frontCheck is not assigned. Add a small empty GameObject in front of the boss and assign it to frontCheck to enable obstacle detection.");
        }

        if (hitsToTrigger < 1) hitsToTrigger = 1;
        if (preAttackDelay < 0f) preAttackDelay = 0f;
        if (postAttackDelay < 0f) postAttackDelay = 0f;

        // Subscribe to EnemyHealth damage event if present
        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
        }
        else
        {
            Debug.LogWarning("EnemyHealth component not found on boss; damage-driven attacks won't trigger.");
        }

        // Validate timing bounds
        if (minWalkTime > maxWalkTime) minWalkTime = maxWalkTime = Mathf.Max(minWalkTime, 0.1f);
        if (minIdleTime > maxIdleTime) minIdleTime = maxIdleTime = Mathf.Max(minIdleTime, 0.1f);
    }

    void OnDestroy()
    {
        var health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
        }
    }

    void FixedUpdate()
    {
        // Update timer
        stateTimer -= Time.fixedDeltaTime;

        if (state == State.Attacking)
        {
            // Stay still until preAttackDelay passes and trigger fires, then wait for animation event
            if (!attackTriggered)
            {
                attackTimer -= Time.fixedDeltaTime;
                if (attackTimer <= 0f)
                {
                    // Fire attack trigger
                    if (animator != null)
                    {
                        animator.SetTrigger("Attack");
                    }
                    attackTriggered = true;
                }
            }

            // Force no horizontal movement while attacking
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            return; // skip other logic while attacking
        }

        if (state == State.Walking)
        {
            float dir = movingRight ? 1f : -1f;

            // Obstacle check using an overlap circle at the frontCheck transform
            if (frontCheck != null)
            {
                Collider2D hit = Physics2D.OverlapCircle(frontCheck.position, checkRadius, obstacleLayers);
                if (hit != null && hit.gameObject != gameObject)
                {
                    // Flip direction immediately when hitting a wall
                    movingRight = !movingRight;
                    // reset walk timer a bit to avoid rapid flipping
                    stateTimer = Random.Range(minWalkTime * 0.5f, maxWalkTime);
                    // update dir for this frame
                    dir = movingRight ? 1f : -1f;
                }
            }

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            }

            // Flip sprite by rotating 180 degrees on Y axis depending on direction
            float yRot = movingRight ? 0f : 180f;
            transform.eulerAngles = new Vector3(0f, yRot, 0f);

            // Update animator parameter
            if (animator != null)
            {
                bool isWalking = Mathf.Abs(dir * speed) > 0.01f;
                animator.SetBool("isWalking", isWalking);
            }

            // When walk timer ends, decide whether to turn or idle
            if (stateTimer <= 0f)
            {
                // random chance to start an attack instead of turning/idling
                if (Random.value < idleAttackChance)
                {
                    // start attack
                    state = State.Attacking;
                    attackTriggered = false;
                    attackTimer = preAttackDelay;

                    if (rb != null)
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    if (animator != null)
                        animator.SetBool("isWalking", false);

                    return;
                }

                if (Random.value < idleChance)
                {
                    // Go idle
                    state = State.Idle;
                    stateTimer = Random.Range(minIdleTime, maxIdleTime);

                    // Stop horizontal movement
                    if (rb != null)
                    {
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    }

                    if (animator != null) animator.SetBool("isWalking", false);
                }
                else
                {
                    // Turn and keep walking
                    movingRight = !movingRight;
                    state = State.Walking;
                    stateTimer = Random.Range(minWalkTime, maxWalkTime);
                }
            }
        }
        else // Idle
        {
            // Ensure animator shows idle
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }

            // Keep velocity zero while idle
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            if (stateTimer <= 0f)
            {
                // random chance to start an attack instead of immediately resuming walking
                if (Random.value < idleAttackChance)
                {
                    state = State.Attacking;
                    attackTriggered = false;
                    attackTimer = preAttackDelay;

                    if (rb != null)
                        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    if (animator != null)
                        animator.SetBool("isWalking", false);

                    return;
                }

                // Resume walking after idle
                state = State.Walking;
                stateTimer = Random.Range(minWalkTime, maxWalkTime);
                // keep current facing/movingRight
            }
        }
    }

    // Handler for EnemyHealth.OnDamaged
    private void OnDamaged(int damage)
    {
        hitCount++;
        if (hitCount >= hitsToTrigger)
        {
            hitCount = 0;

            // Pause movement and schedule attack trigger after preAttackDelay
            state = State.Attacking;
            attackTriggered = false;
            attackTimer = preAttackDelay;

            // Stop horizontal movement immediately
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
        }
    }

    // This should be called by an Animation Event at the end of the Attack animation
    public void OnAttackAnimationEnd()
    {
        // After the attack animation ends, stay idle for postAttackDelay then resume walking
        if (state == State.Attacking)
        {
            state = State.Idle;
            stateTimer = postAttackDelay;

            // If a clap visual exists, fade it out and destroy it
            if (spawnedClap != null)
            {
                StartCoroutine(FadeOutAndDestroyClap(spawnedClap, clapFadeOutDuration));
                spawnedClap = null; // allow future spawns
                spawnedClapLight = null;
            }
        }
    }

    // Minimal animation event callbacks: clap1 and clap2
    // Clap1 spawns a prefab and fades it in. Clap2 will now also spawn a projectile.
    public void Clap1()
    {
        if (clapPrefab == null) return;
        if (spawnedClap != null) return; // already spawned

        // Parent the spawned clap to this boss and preserve local transform
        spawnedClap = Instantiate(clapPrefab, transform, false);
        spawnedClap.transform.localPosition = clapLocalPosition;

        // initialize sprite alpha to 0
        var srs = spawnedClap.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            var c = sr.color;
            c.a = 0f;
            sr.color = c;
        }

        // initialize light if present
        spawnedClapLight = spawnedClap.GetComponentInChildren<Light>();
        if (spawnedClapLight != null)
        {
            spawnedClapLightOriginalIntensity = spawnedClapLight.intensity;
            spawnedClapLight.intensity = 0f;
        }

        StartCoroutine(FadeInClap(spawnedClap, clapFadeInDuration));

        // spawn a projectile somewhere near the boss
        SpawnProjectileRandom();
    }

    public void Clap2()
    {
        // spawn a projectile somewhere near the boss
        SpawnProjectileRandom();

        TeleportToRandomPoint();
    }

    private void SpawnProjectileRandom()
    {
        if (projectilePrefab == null) return;

        Vector2 offset = Random.insideUnitCircle * projectileSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);
        Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    }

    private IEnumerator FadeInClap(GameObject go, float duration)
    {
        if (go == null) yield break;
        float t = 0f;
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            foreach (var sr in srs)
            {
                var c = sr.color;
                c.a = a;
                sr.color = c;
            }

            if (spawnedClapLight != null)
            {
                spawnedClapLight.intensity = spawnedClapLightOriginalIntensity * a;
            }

            yield return null;
        }

        // ensure fully visible
        foreach (var sr in srs)
        {
            var c = sr.color; c.a = 1f; sr.color = c;
        }
        if (spawnedClapLight != null)
        {
            spawnedClapLight.intensity = spawnedClapLightOriginalIntensity;
        }
    }

    private IEnumerator FadeOutAndDestroyClap(GameObject go, float duration)
    {
        if (go == null) yield break;
        float t = 0f;
        var srs = go.GetComponentsInChildren<SpriteRenderer>();
        float startLightIntensity = spawnedClapLight != null ? spawnedClapLight.intensity : 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / duration);
            foreach (var sr in srs)
            {
                var c = sr.color;
                c.a = a;
                sr.color = c;
            }

            if (spawnedClapLight != null)
            {
                spawnedClapLight.intensity = startLightIntensity * a;
            }

            yield return null;
        }

        Destroy(go);
    }

    private void TeleportToRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0) return;

        int index = -1;
        int attempts = 0;

        // On the very first teleport (lastTeleportIndex == -1) we must not pick index 0 if possible
        bool excludeZeroOnFirst = (lastTeleportIndex == -1 && teleportPoints.Length > 1);

        if (teleportPoints.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                // pick from 0..length-1
                index = Random.Range(0, teleportPoints.Length);
                attempts++;

                // if it's the first teleport and we picked 0, retry
                if (excludeZeroOnFirst && index == 0) continue;

                // stop if different from lastTeleportIndex
                if (index != lastTeleportIndex) break;

            } while (attempts < 20);

            // if after retries we still picked lastTeleportIndex (rare), choose the next available index
            if (index == lastTeleportIndex)
            {
                for (int i = 0; i < teleportPoints.Length; i++)
                {
                    if (i == lastTeleportIndex) continue;
                    if (excludeZeroOnFirst && i == 0) continue;
                    index = i;
                    break;
                }
            }
        }

        lastTeleportIndex = index;

        Transform teleportPoint = teleportPoints[index];

        if (teleportPoint != null)
        {
            transform.position = teleportPoint.position;
            transform.eulerAngles = new Vector3(0f, movingRight ? 0f : 180f, 0f);
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (frontCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(frontCheck.position, checkRadius);
        }
    }
}
