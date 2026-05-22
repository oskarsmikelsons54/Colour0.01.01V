using System.Collections;
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
    [SerializeField] private ParticleSystem dashParticle;
    [SerializeField] private ParticleSystem doubleJumpParticle;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpingPower = 16f;

    [Header("Jump Settings")]
    [SerializeField] private int maxJumps = 2;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 24f; 
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParam = "isWalking";
    [SerializeField] private string dashingParam = "isDashing"; 
    private bool isWalking;

    [Header("Coyote / Grace Settings")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float lastGroundedTime = -1f;

    private bool jumpPressed = false;
    private bool jumpReleased = false;

    [Header("Input Buffering")]
    [SerializeField] private float jumpBufferTime = 0.12f;
    private float lastJumpPressedTime = -10f;
    private float lastJumpAppliedTime = -1f;
    private float lastJumpReleasedTime = -1f;

    void Start()
    {
        if (rb != null)
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        jumpsLeft = maxJumps;
    }

    void Update()
    {
        if (isDashing) return;

        horizontal = Input.GetAxisRaw("Horizontal");

        bool grounded = IsGrounded();
        if (grounded)
        {
            jumpsLeft = maxJumps;
            lastGroundedTime = Time.time;
        }

        isWalking = horizontal != 0f && grounded;

        if (animator != null)
            animator.SetBool(walkingParam, isWalking);

        // DASH ar Left Shift taustiņu
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        // Lēciens
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
            jumpPressed = true;
        }

        if (Input.GetButtonUp("Jump"))
        {
            jumpReleased = true;
            lastJumpReleasedTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || isDashing) return;

        // Izmantojam .velocity (strādā visās versijās)
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

        HandleJump();
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        if (animator != null) animator.SetBool(dashingParam, true);

        // spawn dash particles
        SpawnEffect(dashParticle);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDirection = horizontal != 0 ? Mathf.Sign(horizontal) : (isFacingRight ? 1f : -1f);
        rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        if (animator != null) animator.SetBool(dashingParam, false);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void HandleJump()
    {
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        bool shouldAttemptJump = jumpPressed || bufferedJump;

        if (shouldAttemptJump)
        {
            bool grounded = IsGrounded();
            bool coyoteNow = !grounded && (Time.time - lastGroundedTime) <= coyoteTime;

            if (grounded || coyoteNow || jumpsLeft > 0)
            {
                bool isAirJump = !grounded && !coyoteNow; // treat this as a double-jump / mid-air jump

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
                if (!grounded && !coyoteNow) jumpsLeft--;

                lastJumpAppliedTime = Time.time;
                jumpPressed = false;
                lastJumpPressedTime = -10f;

                if (isAirJump)
                {
                    SpawnEffect(doubleJumpParticle);
                }
            }
        }

        if (jumpReleased)
        {
            if (lastJumpAppliedTime > 0f && lastJumpReleasedTime > lastJumpAppliedTime && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            }
            jumpReleased = false;
            lastJumpReleasedTime = -1f;
        }
    }

    private void LateUpdate()
    {
        if (!isDashing) Flip();
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

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

    private void SpawnEffect(ParticleSystem effect)
    {
        if (effect == null) return;
        // Instantiate as a child of the player, then set localPosition to y = -1 so the particle spawns below the player.
        ParticleSystem instance = Instantiate(effect, transform.position, Quaternion.identity, transform);
        instance.transform.localPosition = new Vector3(0f, -1f, 0f);
        instance.Play();
        Destroy(instance.gameObject, 3f);
    }
}