using System.Collections;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;      // How far the attack reaches
    [SerializeField] private int attackDamage = 25;         // Damage dealt per hit
    [SerializeField] private float attackRate = 1f;         // Attacks per second

    [Header("Attack Point Movement")]
    [SerializeField] private float attackPointRadius = 1f;  // How far from the center the attackPoint can move
    [SerializeField] private Transform radiusCenter;        // Center of the radius (if null, player transform is used)
    [Tooltip("Degrees added to the computed rotation. Use to match your sprite's default orientation.")]
    [SerializeField] private float attackPointRotationOffset = 0f;

    [Header("References")]
    [SerializeField] private Transform attackPoint;         // Empty GameObject where attack originates
    [SerializeField] private LayerMask enemyLayers;         // Which layers count as enemies
    [SerializeField] private AtkAnim attackEffect; // Optional visual/sfx effect for attacks

    [Header("Visibility")]
    [SerializeField] private float attackVisibleDuration = 0.15f; // How long the attackPoint is visible when attacking

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;        // Default attack sound
    [SerializeField] private float attackSoundVolume = 1f;
    [SerializeField] private AudioClip hitSound;           // Sound played when an enemy is hit
    [SerializeField] private float hitSoundVolume = 1f;

    private float nextAttackTime = 0f;
    private Camera mainCamera;

    // Cached renderers on the attackPoint to toggle visibility
    private Renderer[] attackPointRenderers;
    private Coroutine hideCoroutine;

    // Audio source for playing clips
    private AudioSource audioSource;

    void Awake()
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

    void Update()
    {
        // Update attackPoint position to follow mouse within radius
        UpdateAttackPointPosition();

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1")) // Default: left mouse button or Ctrl
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
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
    }

    private void Attack()
    {
        Debug.Log("Attack");

        // Play attack sound if assigned
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound, Mathf.Clamp01(attackSoundVolume));
        }

        // Temporarily show the attackPoint visuals
        ShowAttackPointTemporarily(attackVisibleDuration);

        // Play effect if assigned
        attackEffect?.Play();

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
    }

    private void ShowAttackPointTemporarily(float duration)
    {
        SetAttackPointVisible(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAttackPointVisible(false);
        hideCoroutine = null;
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