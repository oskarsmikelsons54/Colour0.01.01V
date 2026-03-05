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

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParam = "isWalking";
    private bool isWalking;

    // Coyote time: allow jump briefly after leaving ground
    [Header("Coyote / Grace Settings")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float lastGroundedTime = -1f;

    // Input flags to avoid modifying physics in Update
    private bool jumpPressed = false;
    private bool jumpReleased = false;

    // Whether the current requested jump was triggered by coyote time
    private bool coyoteJumpRequested = false;

    // Timing to avoid processing a release that happened *before* the jump was applied
    private float lastJumpAppliedTime = -1f;
    private float lastJumpReleasedTime = -1f;

    void Start()
    {
        // Ensure interpolation is enabled so the Rigidbody2D's movement is smoothly interpolated between physics steps
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        // Get horizontal input (sampled each frame)
        horizontal = Input.GetAxisRaw("Horizontal");

        // Check grounded once and reuse
        bool grounded = IsGrounded();

        // Reset jumps when grounded
        if (grounded)
        {
            jumpsLeft = maxJumps;
            lastGroundedTime = Time.time;
        }

        // Update walking state for animation (walking only when moving horizontally and grounded)
        isWalking = horizontal != 0f && grounded;
        if (animator != null)
        {
            animator.SetBool(walkingParam, isWalking);
        }

        // Sample jump input here, but do not modify the Rigidbody directly in Update
        if (Input.GetButtonDown("Jump"))
        {
            bool canJump = false;
            bool requestCoyote = false;

            // Allow jump if grounded
            if (grounded)
            {
                canJump = true;
                requestCoyote = false;
            }
            // Allow jump if within coyote time after leaving ground; mark it as a coyote jump
            else if (Time.time - lastGroundedTime <= coyoteTime)
            {
                canJump = true;
                requestCoyote = true;
            }
            // Allow jump if we have remaining air jumps
            else if (jumpsLeft > 0)
            {
                canJump = true;
                requestCoyote = false;
            }

            if (canJump)
            {
                jumpPressed = true;
                coyoteJumpRequested = requestCoyote;
            }
        }

        // Short jump if releasing early - sample the release in Update and timestamp it
        if (Input.GetButtonUp("Jump"))
        {
            jumpReleased = true;
            lastJumpReleasedTime = Time.time;
        }

        // NOTE: Flip is visual. We'll perform it in LateUpdate so it doesn't mix with physics updates.
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Handle jump application inside FixedUpdate (physics step)
        if (jumpPressed)
        {
            if (coyoteJumpRequested)
            {
                // Apply jump for coyote without consuming an air jump
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                // Do not decrement jumpsLeft so mid-air jumps remain available
                jumpPressed = false;
                coyoteJumpRequested = false;
                lastJumpAppliedTime = Time.time;
            }
            else if (jumpsLeft > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                jumpsLeft--;
                jumpPressed = false;
                lastJumpAppliedTime = Time.time;
            }
            else
            {
                // No jump performed; clear the request
                jumpPressed = false;
                coyoteJumpRequested = false;
            }
        }

        // Short jump application: only cut the jump if the release happened after the jump was applied
        if (jumpReleased)
        {
            if (lastJumpAppliedTime > 0f && lastJumpReleasedTime > lastJumpAppliedTime && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }

            // Clear the release flag so it doesn't affect subsequent jumps
            jumpReleased = false;
            lastJumpReleasedTime = -1f;
        }

        // Move player horizontally in FixedUpdate (physics-driven motion)
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }

    private void LateUpdate()
    {
        // Flip player sprite based on movement direction in LateUpdate (visual change)
        Flip();
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