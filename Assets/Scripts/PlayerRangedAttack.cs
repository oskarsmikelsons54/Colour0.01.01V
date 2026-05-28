using System.Collections;
using UnityEngine;

// Ranged laser weapon with wind-up and cooldown. Single-shot: click once to start windup, then fire once.
// Attach to the player. Configure `fireOrigin` (optional), `enemyLayers`, `damage`, and timings in Inspector.
public class PlayerRangedAttack : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional transform to originate the laser from. If null, uses this GameObject's position.")]
    [SerializeField] private Transform fireOrigin;

    [Header("Damage")]
    [SerializeField] private int damage = 25;
    [Tooltip("Not used for continuous fire here; single-shot applies damage once along beam.")]
    [SerializeField] private float damageInterval = 0.2f; // kept for compatibility but not used for repeated ticks
    [SerializeField] private LayerMask enemyLayers;

    [Header("Beam Visual")]
    [Tooltip("Optional prefab containing a simple square sprite (SpriteRenderer) to use as the beam. The prefab should be a 1x1 unit square facing right.")]
    [SerializeField] private GameObject beamPrefab;
    [SerializeField] private float beamThickness = 0.1f;
    [SerializeField] private float beamMaxDistance = 20f;
    [SerializeField] private float beamSpriteUnitLength = 1f; // world units per sprite length
    [SerializeField] private float beamDisplayTime = 0.15f; // how long the beam is shown after firing

    [Header("LineRenderer (optional)")]
    [Tooltip("If true, a LineRenderer will be used for the beam instead of the sprite prefab.")]
    [SerializeField] private bool useLineRenderer = true;
    [Tooltip("Material used by the LineRenderer. If not set, a default sprite material will be created.")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineStartWidth = 0.12f;
    [SerializeField] private float lineEndWidth = 0.12f;
    [SerializeField] private float textureScrollSpeed = 2f;

    [Header("Timings")]
    [Tooltip("Time to wind up before the laser fires")]
    [SerializeField] private float windUpTime = 0.5f;
    [Tooltip("Cooldown after firing the laser")]
    [SerializeField] private float cooldownTime = 1.5f;

    [Header("Cooldown UI")]
    [Tooltip("Optional CooldownText component used to display this weapon's cooldown. Assign a distinct CooldownText per weapon to avoid conflicts.")]
    [SerializeField] private CooldownText cooldownDisplay;

    private Camera mainCamera;

    private Coroutine windupCoroutine;
    private Coroutine beamDisplayCoroutine;
    private Coroutine cooldownCoroutine;

    private bool isCoolingDown = false;

    // sprite-based beam
    private GameObject beamInstance;
    private SpriteRenderer beamRenderer;

    // line-renderer based beam
    private GameObject lineInstance;
    private LineRenderer lineRenderer;
    private Material lineMaterialInstance;

    void Start()
    {
        mainCamera = Camera.main ?? Camera.current;

        // If fireOrigin not assigned, use this transform
        if (fireOrigin == null)
            fireOrigin = transform;

        // try to auto-find a CooldownText in children if not assigned in Inspector
        if (cooldownDisplay == null)
            cooldownDisplay = GetComponentInChildren<CooldownText>(true);

        beamInstance = null;
        beamRenderer = null;
        lineInstance = null;
        lineRenderer = null;
        lineMaterialInstance = null;
    }

    void Update()
    {
        if (!enabled) return;

        if (Input.GetButtonDown("Fire1"))
        {
            TryStartWindup();
        }
    }

    private void TryStartWindup()
    {
        if (isCoolingDown) return;
        if (windupCoroutine != null) return;

        windupCoroutine = StartCoroutine(WindupAndFire());
    }

    private IEnumerator WindupAndFire()
    {
        float elapsed = 0f;
        while (elapsed < windUpTime)
        {
            elapsed += Time.deltaTime;
            // Could add wind-up VFX here
            yield return null;
        }

        windupCoroutine = null;
        FireOnce();
        yield break;
    }

    private void FireOnce()
    {
        // perform a single laser attack: damage all enemies along beam up to mouse position or beamMaxDistance
        Vector3 origin = fireOrigin != null ? (Vector3)fireOrigin.position : transform.position;

        if (mainCamera == null)
            mainCamera = Camera.main ?? Camera.current;

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = origin.z;

        Vector2 dirVec = (mouseWorld - origin);
        float requestedDistance = dirVec.magnitude;
        Vector2 dir = requestedDistance > Mathf.Epsilon ? dirVec / requestedDistance : Vector2.right;

        float beamDistance = Mathf.Min(requestedDistance, beamMaxDistance);

        // Cast along beam to damage enemies up to beamDistance
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, beamDistance, enemyLayers);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            var health = hit.collider.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // show beam visual briefly (LineRenderer preferred if enabled)
        if (useLineRenderer)
        {
            EnsureLineInstance();
            if (lineRenderer != null)
            {
                UpdateLineVisual(origin, dir, beamDistance);
                if (beamDisplayCoroutine != null) StopCoroutine(beamDisplayCoroutine);
                beamDisplayCoroutine = StartCoroutine(LineDisplayCoroutine(beamDisplayTime));
            }
        }
        else
        {
            EnsureBeamInstance();
            if (beamInstance != null && beamRenderer != null)
            {
                UpdateBeamVisualOnce(origin, dir, beamDistance);
                if (beamDisplayCoroutine != null) StopCoroutine(beamDisplayCoroutine);
                beamDisplayCoroutine = StartCoroutine(BeamDisplayCoroutine(beamDisplayTime));
            }
        }

        // start cooldown UI if assigned (each weapon should have its own CooldownText instance)
        if (cooldownDisplay != null)
        {
            cooldownDisplay.StartCooldown(cooldownTime);
        }

        // start cooldown
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(CooldownCoroutine());

        Debug.Log("Laser fired (single-shot)");
    }

    private IEnumerator BeamDisplayCoroutine(float time)
    {
        if (beamInstance != null) beamInstance.SetActive(true);
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (beamInstance != null) beamInstance.SetActive(false);
        beamDisplayCoroutine = null;
    }

    private IEnumerator LineDisplayCoroutine(float time)
    {
        if (lineRenderer == null) yield break;
        lineRenderer.enabled = true;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            // animate texture offset if material supports it
            if (lineMaterialInstance != null && textureScrollSpeed != 0f)
            {
                Vector2 off = lineMaterialInstance.mainTextureOffset;
                off.x += textureScrollSpeed * Time.deltaTime;
                lineMaterialInstance.mainTextureOffset = off;
            }
            yield return null;
        }
        lineRenderer.enabled = false;
        beamDisplayCoroutine = null;
    }

    private IEnumerator CooldownCoroutine()
    {
        isCoolingDown = true;
        float elapsed = 0f;
        while (elapsed < cooldownTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        isCoolingDown = false;
        cooldownCoroutine = null;
        Debug.Log("Laser cooldown complete");
    }

    // Beam visual helpers - sprite based
    private void EnsureBeamInstance()
    {
        if (beamInstance != null) return;
        if (beamPrefab == null) return;

        beamInstance = Instantiate(beamPrefab, Vector3.zero, Quaternion.identity);
        beamInstance.name = "LaserBeam_Instance";
        beamRenderer = beamInstance.GetComponent<SpriteRenderer>();
        if (beamRenderer == null)
        {
            beamRenderer = beamInstance.GetComponentInChildren<SpriteRenderer>();
        }

        // keep inactive until firing
        beamInstance.SetActive(false);
    }

    private void UpdateBeamVisualOnce(Vector3 origin, Vector2 dir, float beamDistance)
    {
        if (beamInstance == null || beamRenderer == null) return;

        // position beam at midpoint
        Vector3 endPoint = origin + (Vector3)dir * beamDistance;
        Vector3 mid = (origin + endPoint) * 0.5f;
        beamInstance.transform.position = mid;

        // rotation
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        beamInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // scale: sprite is expected to be beamSpriteUnitLength units long (X = 1 by default)
        float scaleX = (beamDistance / Mathf.Max(0.0001f, beamSpriteUnitLength));
        beamInstance.transform.localScale = new Vector3(scaleX, beamThickness, 1f);
    }

    // Beam visual helpers - LineRenderer based
    private void EnsureLineInstance()
    {
        if (lineInstance != null) return;

        lineInstance = new GameObject("LaserLine_Instance");
        lineInstance.transform.SetParent(null);
        lineRenderer = lineInstance.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = 4;
        lineRenderer.alignment = LineAlignment.View;

        // assign material instance
        if (lineMaterial != null)
            lineMaterialInstance = new Material(lineMaterial);
        else
            lineMaterialInstance = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.material = lineMaterialInstance;
        lineRenderer.enabled = false;

        lineRenderer.startWidth = lineStartWidth;
        lineRenderer.endWidth = lineEndWidth;
    }

    private void UpdateLineVisual(Vector3 origin, Vector2 dir, float beamDistance)
    {
        if (lineRenderer == null) return;

        Vector3 endPoint = origin + (Vector3)dir * beamDistance;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        lineRenderer.startWidth = lineStartWidth;
        lineRenderer.endWidth = lineEndWidth;

        // ensure material tiling looks reasonable - the texture should be set to repeat
        if (lineMaterialInstance != null)
        {
            // set basic texture scale based on beam length
            float tiling = beamDistance / Mathf.Max(0.0001f, beamSpriteUnitLength);
            lineMaterialInstance.mainTextureScale = new Vector2(tiling, 1f);
        }
    }
}
