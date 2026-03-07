using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float followRange = 8f;

    [Header("Obstacle Check")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.5f;

    private bool isGrounded;

    // Cached intent to avoid modifying physics in Update
    private bool shouldFollow = false;
    private float followDirection = 0f; // -1, 0 or 1
    private bool jumpRequested = false;

    void Start()
    {
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        if (player == null) return;

        CheckGround();

        float distance = Vector2.Distance(transform.position, player.position);

        // Only follow if player is close; cache intent
        if (distance <= followRange)
        {
            shouldFollow = true;
            followDirection = Mathf.Sign(player.position.x - transform.position.x);

            // Check for jump conditions and only request jump here
            CheckForJump();
        }
        else
        {
            shouldFollow = false;
            followDirection = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Apply horizontal movement in FixedUpdate (physics)
        if (shouldFollow)
        {
            rb.linearVelocity = new Vector2(followDirection * speed, rb.linearVelocity.y);
        }
        else
        {
            // Optionally let other systems control x-velocity; set to 0 to stop when not following
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // Apply jump if requested
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
    }

    // Check ground
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            0.2f,
            groundLayer
        );
    }

    // Jump if wall ahead - only request jump here
    private void CheckForJump()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            transform.right,
            wallCheckDistance,
            groundLayer
        );

        if (hit.collider != null && isGrounded)
        {
            jumpRequested = true;
        }
    }

    private void LateUpdate()
    {
        // Visual flip in LateUpdate so it runs after physics
        if (shouldFollow && followDirection != 0f)
        {
            Flip(followDirection);
        }
    }

    // Flip sprite on the parent transform if present. This avoids flipping only the child that has this script.
    private void Flip(float direction)
    {
        Transform target = transform.parent != null ? transform.parent : transform;
        Vector3 scale = target.localScale;
        // Invert sign mapping so the visual faces the player when default art orientation differs.
        float sign = direction > 0 ? -1f : 1f;
        scale.x = Mathf.Abs(scale.x) * sign;
        target.localScale = scale;
    }
}