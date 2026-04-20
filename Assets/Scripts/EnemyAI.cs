using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Target")]
    // Automatically find the player by tag at runtime instead of assigning in inspector
    private Transform player;

    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float followRange = 8f;
    
    [Header("Speed Boost (Charge)")]
    [SerializeField] private float chargeRange = 4f;
    [SerializeField] private float chargeMultiplier = 1.5f;

    // --- JAUNUMS: Lēciena uzbrukuma iestatījumi ---
    [Header("Jump Attack")] 
    [SerializeField] private float minJumpAttackTime = 2f; // Minimālais laiks starp lēcieniem
    [SerializeField] private float maxJumpAttackTime = 5f; // Maksimālais laiks starp lēcieniem
    private float jumpAttackTimer;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Obstacle & Edge Check")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private Transform edgeCheck;
    [SerializeField] private float edgeCheckDistance = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParam = "isWalking";
    private bool isWalking;

    private bool isGrounded;

    // State Variables
    private bool shouldFollow = false;
    private float moveDirection = 0f;
    private bool jumpRequested = false;
    private float currentSpeed;
    private float patrolDirection = 1f;

    void Start()
    {
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }

        // Try to auto-assign the player Transform by tag if not set in inspector
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }
        
        currentSpeed = speed;
        
        // Sākotnēji iestatām taimeri
        ResetJumpAttackTimer(); 
    }

    void Update()
    {
        // Ensure we have a reference to the player; try to find by tag at runtime
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
            else return;
        }

        CheckGround();

        float distance = Vector2.Distance(transform.position, player.position);

        // 1. Loģika: Ja spēlētājs ir uztveršanas rādiusā
        if (distance <= followRange)
        {
            shouldFollow = true;
            moveDirection = Mathf.Sign(player.position.x - transform.position.x);

            // Uzrāviens (Charge)
            if (distance <= chargeRange)
            {
                currentSpeed = speed * chargeMultiplier;
            }
            else
            {
                currentSpeed = speed;
            }

            CheckForJump();

            // --- JAUNUMS: Jump Attack loģika ---
            // Taimeris iet uz leju tikai tad, kad dzenas pakaļ
            jumpAttackTimer -= Time.deltaTime;
            
            if (jumpAttackTimer <= 0f && isGrounded)
            {
                jumpRequested = true; // Pasūtam lēcienu
                ResetJumpAttackTimer(); // Sākam skaitīt laiku līdz nākamajam lēcienam
            }
        }
        // 2. Loģika: Ja spēlētājs ir pārāk tālu (Patrulēšana)
        else
        {
            shouldFollow = false;
            currentSpeed = patrolSpeed;
            PatrolLogic();
            
            // Atiestatām taimeri, lai ienaidnieks nepalēktos uzreiz tajā pašā sekundē, kad ieraudzīs spēlētāju
            ResetJumpAttackTimer(); 
        }

        // Animācijas atjaunināšana
        bool newIsWalking = isGrounded && moveDirection != 0f;
        if (newIsWalking != isWalking)
        {
            isWalking = newIsWalking;
            if (animator != null && animator.GetBool(walkingParam) != isWalking)
            {
                animator.SetBool(walkingParam, isWalking);
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector2(moveDirection * currentSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void CheckForJump()
    {
        bool wallAhead = Physics2D.Raycast(wallCheck.position, Vector2.right * moveDirection, wallCheckDistance, groundLayer).collider != null;

        bool edgeAhead = false;
        if (edgeCheck != null)
        {
            edgeAhead = Physics2D.Raycast(edgeCheck.position, Vector2.down, edgeCheckDistance, groundLayer).collider == null;
        }

        if ((wallAhead || edgeAhead) && isGrounded)
        {
            jumpRequested = true;
        }
    }

    // --- JAUNUMS: Funkcija taimera atiestatīšanai ---
    private void ResetJumpAttackTimer()
    {
        // Izvēlas nejaušu laiku starp minimālo un maksimālo vērtību
        jumpAttackTimer = Random.Range(minJumpAttackTime, maxJumpAttackTime);
    }

    private void PatrolLogic()
    {
        moveDirection = patrolDirection;

        bool wallAhead = Physics2D.Raycast(wallCheck.position, Vector2.right * moveDirection, wallCheckDistance, groundLayer).collider != null;
        
        bool edgeAhead = false;
        if (edgeCheck != null)
        {
            edgeAhead = Physics2D.Raycast(edgeCheck.position, Vector2.down, edgeCheckDistance, groundLayer).collider == null;
        }

        if (wallAhead || edgeAhead)
        {
            patrolDirection *= -1f;
            moveDirection = patrolDirection;
        }
    }

    private void LateUpdate()
    {
        if (moveDirection != 0f)
        {
            Flip(moveDirection);
        }
    }

    private void Flip(float direction)
    {
        Transform target = transform.parent != null ? transform.parent : transform;
        Vector3 scale = target.localScale;
        
        float sign = direction > 0 ? -1f : 1f; 
        scale.x = Mathf.Abs(scale.x) * sign;
        target.localScale = scale;
    }
}