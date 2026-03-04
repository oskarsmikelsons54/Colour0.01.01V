using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private float horizontal;
    private bool isFacingRight = true;

    private int jumpsLeft;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpingPower = 16f;

    [Header("Jump Settings")]
    [SerializeField] private int maxJumps = 2; // 2 = double jump

    void Update()
    {
        // Get horizontal input
        horizontal = Input.GetAxisRaw("Horizontal");

        // Reset jumps when grounded
        if (IsGrounded())
        {
            jumpsLeft = maxJumps;
        }

        // Jump (allow if we still have jumps)
        if (Input.GetButtonDown("Jump") && jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
            jumpsLeft--;
        }

        // Short jump if releasing early
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        Flip();
    }

    private void FixedUpdate()
    {
        // Move player horizontally
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    // Check if player is touching the ground
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    // Flip player sprite based on movement direction
    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;

            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
}