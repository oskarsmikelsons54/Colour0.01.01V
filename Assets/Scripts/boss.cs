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
