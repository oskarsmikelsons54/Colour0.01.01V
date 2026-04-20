using System.Collections;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;      // How far the attack reaches
    [SerializeField] private int attackDamage = 25;         // Damage dealt per hit
    [SerializeField] private float attackRate = 1f;         // Attacks per second (used as interval)

    [Header("Attack Point Movement")]
    [SerializeField] private float attackPointRadius = 1f;  // How far from the center the attackPoint can move
    [SerializeField] private Transform radiusCenter;        // Center of the radius (if null, player transform is used)
    [Tooltip("Degrees added to the computed rotation. Use to match your sprite's default orientation.")]
    [SerializeField] private float attackPointRotationOffset = 0f;

    [Header("References")]
    [SerializeField] private Transform attackPoint;         // Empty GameObject where attack originates
    [SerializeField] private LayerMask enemyLayers;         // Which layers count as enemies
    [SerializeField] private AtkAnim attackEffect; // Optional visual/sfx effect for attacks

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;        // Default attack sound
    [SerializeField] private float attackSoundVolume = 1f;
    [SerializeField] private AudioClip hitSound;           // Sound played when an enemy is hit
    [SerializeField] private float hitSoundVolume = 1f;

    private Camera mainCamera;

    // Cached renderers on the attackPoint to toggle visibility
    private Renderer[] attackPointRenderers;

    // Audio source for playing clips
    private AudioSource audioSource;

    // Attack coroutine reference
    private Coroutine attackCoroutine;

    // Desired world scale for the attackPoint so it doesn't mirror when parent flips
    private Vector3 desiredAttackPointWorldScale = Vector3.one;

    private void Awake()
    {
        // Cache main camera
        mainCamera = Camera.main ?? Camera.current;

        // If no explicit center assigned, default to this GameObject
        if (radiusCenter == null)
            radiusCenter = transform;

        // Cache renderers on attackPoint (including children) so we can hide/show them
        if (attackPoint != null)
        {
            attackPointRenderers = attackPoint.GetComponentsInChildren<Renderer>(true);
            // Start hidden
            SetAttackPointVisible(false);

            // Cache desired world scale so we can restore it if parent flips (prevents visual mirroring)
            desiredAttackPointWorldScale = attackPoint.lossyScale;

            // Detach attackPoint from player so it doesn't inherit flips/negative scale
            // Keep its current world transform when unparenting
            attackPoint.SetParent(null, true);
        }

        // Ensure there's an AudioSource to play sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    private void OnDisable()
    {
        StopAttacking();
    }

    private void OnDestroy()
    {
        StopAttacking();
    }

    private void Update()
    {
        // Update attackPoint position to follow mouse within radius
        UpdateAttackPointPosition();

        // Start attack when button pressed, stop when released
        if (Input.GetButtonDown("Fire1"))
        {
            StartAttacking();
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            StopAttacking();
        }
    }

    private void StartAttacking()
    {
        if (attackCoroutine != null) return;
        attackCoroutine = StartCoroutine(AttackLoop());

        // Show visuals immediately when starting
        SetAttackPointVisible(true);

        // Trigger effect at start (looping animation should be handled by the effect's animator)
        attackEffect?.Play();
    }

    private void StopAttacking()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        // Hide visuals when stopping
        SetAttackPointVisible(false);

        // Hide/stop effect if available
        attackEffect?.Hide();
    }

    private IEnumerator AttackLoop()
    {
        // interval between attacks
        float interval = (attackRate > 0f) ? (1f / attackRate) : 0.1f;

        while (true)
        {
            // Play attack sound if assigned
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound, Mathf.Clamp01(attackSoundVolume));
            }

            // Detect enemies in range
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

            bool anyHit = false;
            // Damage all enemies in range
            foreach (Collider2D enemy in hitEnemies)
            {
                // Assumes enemies have a script with TakeDamage(int amount)
                var health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(attackDamage);
                    anyHit = true;
                }
            }

            // Play hit sound if at least one enemy was hit
            if (anyHit && hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound, Mathf.Clamp01(hitSoundVolume));
            }

            // Wait for next tick
            if (interval <= 0f)
                yield return null;
            else
                yield return new WaitForSeconds(interval);
        }
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null || radiusCenter == null) return;

        // Ensure camera is available
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? Camera.current;
            if (mainCamera == null) return;
        }

        // Convert mouse to world point at the same Z as the radius center
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = radiusCenter.position.z;

        Vector3 origin = radiusCenter.position;
        origin.z = mouseWorld.z;

        Vector3 dir = mouseWorld - origin;
        float distance = Mathf.Min(dir.magnitude, attackPointRadius);

        Vector3 newPos;
        if (dir.sqrMagnitude <= Mathf.Epsilon)
        {
            // If mouse is exactly on center, place attackPoint to the right by distance
            newPos = origin + Vector3.right * distance;
        }
        else
        {
            newPos = origin + dir.normalized * distance;
        }

        attackPoint.position = newPos;

        // Rotate attackPoint so its local +X faces away from the center
        Vector3 rotDir = newPos - origin;
        if (rotDir.sqrMagnitude > Mathf.Epsilon)
        {
            float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg;
            attackPoint.rotation = Quaternion.Euler(0f, 0f, angle + attackPointRotationOffset);
        }

        // Restore attackPoint world scale to prevent flipping when parent scale changes (e.g. player flip)
        if (attackPoint != null)
        {
            Transform parent = attackPoint.parent;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;

            // Avoid division by zero
            float px = (Mathf.Abs(parentScale.x) < 1e-6f) ? 1f : parentScale.x;
            float py = (Mathf.Abs(parentScale.y) < 1e-6f) ? 1f : parentScale.y;
            float pz = (Mathf.Abs(parentScale.z) < 1e-6f) ? 1f : parentScale.z;

            Vector3 newLocalScale = new Vector3(
                desiredAttackPointWorldScale.x / px,
                desiredAttackPointWorldScale.y / py,
                desiredAttackPointWorldScale.z / pz
            );

            attackPoint.localScale = newLocalScale;
        }
    }

    private void SetAttackPointVisible(bool visible)
    {
        if (attackPointRenderers == null) return;
        for (int i = 0; i < attackPointRenderers.Length; i++)
        {
            var r = attackPointRenderers[i];
            if (r != null)
                r.enabled = visible;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // Draw attack range in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // Draw allowed radius around the selected center
        if (radiusCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(radiusCenter.position, attackPointRadius);
        }
    }
}