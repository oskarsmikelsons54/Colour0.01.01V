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

    // Input buffer so a jump press slightly before FixedUpdate or landing is still applied
    [Header("Input Buffering")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    private float lastJumpPressedTime = -10f;

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

        // Initialize jumps so spawning in air still has expected jumps
        jumpsLeft = maxJumps;

        // If starting grounded, set lastGroundedTime so coyote behaves correctly
        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
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
            // Record the time of the press for buffering
            lastJumpPressedTime = Time.time;
            // Also set jumpPressed flag for immediate application path
            jumpPressed = true;
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

        // Determine if there's a pending jump input (immediate flag or buffered recent press)
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        bool shouldAttemptJump = jumpPressed || bufferedJump;

        if (shouldAttemptJump)
        {
            // Re-evaluate grounded and coyote at physics step
            bool grounded = IsGrounded();
            bool coyoteNow = !grounded && (Time.time - lastGroundedTime) <= coyoteTime;

            if (grounded || coyoteNow || jumpsLeft > 0)
            {
                if (coyoteNow && !grounded)
                {
                    // Apply coyote jump without consuming an air jump
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                }
                else if (grounded)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                }
                else
                {
                    // Mid-air jump consumes one jump
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                    jumpsLeft--;
                }

                // Record application and clear input markers
                lastJumpAppliedTime = Time.time;
                jumpPressed = false;
                lastJumpPressedTime = -10f;
                coyoteJumpRequested = false;
            }
            else
            {
                // Could not jump now; keep buffered input until it expires
                jumpPressed = false; // clear immediate flag so we rely on buffer
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