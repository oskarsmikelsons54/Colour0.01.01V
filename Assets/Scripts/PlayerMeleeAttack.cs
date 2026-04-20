using System.Collections;
using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 1f;

    [Header("Attack Point Movement")]
    [SerializeField] private float attackPointRadius = 1f;
    [SerializeField] private Transform radiusCenter;
    [SerializeField] private float attackPointRotationOffset = 0f;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask destructibleLayers; // ✅ NEW
    [SerializeField] private AtkAnim attackEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private float attackSoundVolume = 1f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private float hitSoundVolume = 1f;

    private Camera mainCamera;
    private Renderer[] attackPointRenderers;
    private AudioSource audioSource;
    private Coroutine attackCoroutine;
    private Vector3 desiredAttackPointWorldScale = Vector3.one;

    private void Awake()
    {
        mainCamera = Camera.main ?? Camera.current;

        if (radiusCenter == null)
            radiusCenter = transform;

        if (attackPoint != null)
        {
            attackPointRenderers = attackPoint.GetComponentsInChildren<Renderer>(true);
            SetAttackPointVisible(false);

            desiredAttackPointWorldScale = attackPoint.lossyScale;
            attackPoint.SetParent(null, true);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnDisable() => StopAttacking();
    private void OnDestroy() => StopAttacking();

    private void Update()
    {
        UpdateAttackPointPosition();

        if (Input.GetButtonDown("Fire1"))
            StartAttacking();
        else if (Input.GetButtonUp("Fire1"))
            StopAttacking();
    }

    private void StartAttacking()
    {
        if (attackCoroutine != null) return;

        attackCoroutine = StartCoroutine(AttackLoop());
        SetAttackPointVisible(true);
        attackEffect?.Play();
    }

    private void StopAttacking()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        SetAttackPointVisible(false);
        attackEffect?.Hide();
    }

    private IEnumerator AttackLoop()
    {
        float interval = (attackRate > 0f) ? (1f / attackRate) : 0.1f;

        while (true)
        {
            if (attackSound != null)
                audioSource.PlayOneShot(attackSound, Mathf.Clamp01(attackSoundVolume));

            // ✅ ENEMIES
            Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
                attackPoint.position, attackRange, enemyLayers);

            // ✅ DESTRUCTIBLE OBJECTS
            Collider2D[] hitObjects = Physics2D.OverlapCircleAll(
                attackPoint.position, attackRange, destructibleLayers);

            bool anyHit = false;

            foreach (Collider2D enemy in hitEnemies)
            {
                var health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(attackDamage);
                    anyHit = true;
                }
            }

            foreach (Collider2D obj in hitObjects)
            {
                var destructible = obj.GetComponent<Destructible>();
                if (destructible != null)
                {
                    destructible.TakeDamage(attackDamage);
                    anyHit = true;
                }
            }

            if (anyHit && hitSound != null)
                audioSource.PlayOneShot(hitSound, Mathf.Clamp01(hitSoundVolume));

            yield return interval > 0f ? new WaitForSeconds(interval) : null;
        }
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null || radiusCenter == null) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? Camera.current;
            if (mainCamera == null) return;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = radiusCenter.position.z;

        Vector3 origin = radiusCenter.position;
        origin.z = mouseWorld.z;

        Vector3 dir = mouseWorld - origin;
        float distance = Mathf.Min(dir.magnitude, attackPointRadius);

        Vector3 newPos = (dir.sqrMagnitude <= Mathf.Epsilon)
            ? origin + Vector3.right * distance
            : origin + dir.normalized * distance;

        attackPoint.position = newPos;

        Vector3 rotDir = newPos - origin;
        if (rotDir.sqrMagnitude > Mathf.Epsilon)
        {
            float angle = Mathf.Atan2(rotDir.y, rotDir.x) * Mathf.Rad2Deg;
            attackPoint.rotation = Quaternion.Euler(0f, 0f, angle + attackPointRotationOffset);
        }

        Vector3 parentScale = attackPoint.parent != null ? attackPoint.parent.lossyScale : Vector3.one;

        attackPoint.localScale = new Vector3(
            desiredAttackPointWorldScale.x / (Mathf.Abs(parentScale.x) < 1e-6f ? 1f : parentScale.x),
            desiredAttackPointWorldScale.y / (Mathf.Abs(parentScale.y) < 1e-6f ? 1f : parentScale.y),
            desiredAttackPointWorldScale.z / (Mathf.Abs(parentScale.z) < 1e-6f ? 1f : parentScale.z)
        );
    }

    private void SetAttackPointVisible(bool visible)
    {
        if (attackPointRenderers == null) return;

        foreach (var r in attackPointRenderers)
            if (r != null) r.enabled = visible;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        if (radiusCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(radiusCenter.position, attackPointRadius);
        }
    }
}