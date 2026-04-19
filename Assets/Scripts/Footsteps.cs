using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip footstepClip;
    public float stepDistance = 2f;

    [Header("Ground Detection")]
    [Tooltip("Optional transform used to check ground. If null, Collider2D.IsTouchingLayers will be used when available.")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Step Tuning")]
    [Tooltip("Minimum horizontal movement (units/frame) considered as moving.")]
    public float moveThreshold = 0.01f;
    [Tooltip("How long to suppress footstep playback after movement stops (seconds).")]
    public float stopMuteDuration = 0.12f;

    AudioSource audioSource;
    Rigidbody2D rb;
    Collider2D col;
    Vector3 lastPosition;
    float accumulatedDistance;
    bool wasGrounded;

    // movement state to avoid a single step when stopping
    bool wasMoving;
    float muteUntil = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        lastPosition = transform.position;
        wasGrounded = IsGrounded();
        wasMoving = false;
    }

    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 horizDelta = currentPos - lastPosition;
        // ignore vertical movement for footstep distance
        horizDelta.y = 0f;
        float moved = horizDelta.magnitude;
        lastPosition = currentPos;

        bool grounded = IsGrounded();

        // Play landing sound when transitioning from air -> ground (first contact)
        if (!wasGrounded && grounded)
        {
            if (footstepClip != null)
                audioSource.PlayOneShot(footstepClip);

            accumulatedDistance = 0f; // avoid immediate step after landing
            wasGrounded = grounded;
            wasMoving = false;
            return;
        }

        // If not grounded, don't accumulate steps
        if (!grounded)
        {
            wasGrounded = grounded;
            wasMoving = false;
            return;
        }

        wasGrounded = grounded;

        bool isMoving = moved >= moveThreshold;

        // If we just stopped moving, reset accumulation and mute briefly to avoid stray step
        if (wasMoving && !isMoving)
        {
            accumulatedDistance = 0f;
            muteUntil = Time.time + stopMuteDuration;
        }

        if (isMoving)
        {
            accumulatedDistance += moved;

            // Only play if not muted
            if (accumulatedDistance >= stepDistance && Time.time >= muteUntil)
            {
                if (footstepClip != null)
                    audioSource.PlayOneShot(footstepClip);
                accumulatedDistance = 0f;
            }
        }

        wasMoving = isMoving;
    }

    private bool IsGrounded()
    {
        // Prefer explicit groundCheck if provided
        if (groundCheck != null)
        {
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // If we have a collider, use built-in layer check
        if (col != null)
        {
            return col.IsTouchingLayers(groundLayer);
        }

        // Fallback: use vertical velocity (considered grounded if near zero and not falling)
        if (rb != null)
        {
            return Mathf.Abs(rb.linearVelocity.y) < 0.01f;
        }

        // If nothing available, assume grounded to avoid silent footstep issues
        return true;
    }

    // Optional: visualize ground check radius in editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}