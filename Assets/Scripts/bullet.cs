using System.Collections;
using UnityEngine;

public class bullet : MonoBehaviour
{
    [Tooltip("Delay before the projectile starts moving toward the player")]
    [SerializeField] private float delayBeforeCharge = 0.5f;
    [Tooltip("Speed of the projectile when charging")]
    [SerializeField] private float chargeSpeed = 6f;
    [Tooltip("Damage applied to the player on hit")]
    [SerializeField] private float damage = 1f;
    [Tooltip("Maximum lifetime in seconds before auto-destroy")]
    [SerializeField] private float maxLifetime = 6f;

    private Rigidbody2D rb;
    private Transform targetPlayer;

    void Start()
    {
        // If this object was parented (boss instantiates as child), detach so it can move freely
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("bullet: Rigidbody2D not found on prefab. Add a Rigidbody2D to the prefab for reliable physics collisions.");
            Destroy(gameObject);
            return;
        }

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Try to find player by tag first, otherwise by PlayerHealth component in scene
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            targetPlayer = playerObj.transform;
        else
        {
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
                targetPlayer = ph.transform;
        }

        // start behavior
        StartCoroutine(ChargeRoutine());

        // ensure destruction after max lifetime
        Destroy(gameObject, maxLifetime);
    }

    private IEnumerator ChargeRoutine()
    {
        // initial idle/wait
        if (delayBeforeCharge > 0f)
            yield return new WaitForSeconds(delayBeforeCharge);

        if (targetPlayer == null)
        {
            // No target found; just destroy after a short time
            Destroy(gameObject);
            yield break;
        }

        // compute direction at moment of charge
        Vector2 dir = ((Vector2)targetPlayer.position - (Vector2)transform.position).normalized;

        // orient sprite to face movement (assumes sprite faces right by default)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // set velocity (Rigidbody2D is required)
        rb.linearVelocity = dir * chargeSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Only react to objects tagged as Player
        if (!other.CompareTag("Player"))
            return;

        // Apply damage if PlayerHealth present on the hit collider or its parents
        var ph = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
