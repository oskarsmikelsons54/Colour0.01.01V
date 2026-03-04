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

    void Update()
    {
        if (player == null) return;

        CheckGround();

        float distance = Vector2.Distance(transform.position, player.position);

        // Only follow if player is close
        if (distance <= followRange)
        {
            FollowPlayer();
            CheckForJump();
        }
    }

    // Move toward player
    private void FollowPlayer()
    {
        float direction = player.position.x - transform.position.x;
        direction = Mathf.Sign(direction);

        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        Flip(direction);
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

    // Jump if wall ahead
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
            Jump();
        }
    }

    // Jump
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // Flip sprite
    private void Flip(float direction)
    {
        if (direction > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
}