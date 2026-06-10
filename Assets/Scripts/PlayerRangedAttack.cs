using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Ranged laser weapon with wind-up and cooldown. Single-shot: click once to start windup, then fire once.
// Attach to the player. Configure `fireOrigin`, `enemyLayers`, `damage`, and timings in Inspector.
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

    [Header("Beam Visual (LineRenderer)")]
    [Tooltip("Material used by the LineRenderer. If not set, a default sprite material will be created.")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineStartWidth = 0.12f;
    [SerializeField] private float lineEndWidth = 0.12f;
    [SerializeField] private float textureScrollSpeed = 2f;
    [Tooltip("Primary color used by the LineRenderer when drawing the beam (used for tip fade gradient)")]
    [SerializeField] private Color lineColor = Color.white;
    [Tooltip("Fraction of beam length used for fading the tips (0..0.45)")]
    [Range(0f, 0.45f)]
    [SerializeField] private float tipFadeFraction = 0.12f;
    [Tooltip("World units per texture repeat used for the LineRenderer material tiling")]
    [SerializeField] private float lineTextureUnitLength = 1f;
    [Tooltip("Duration of the fade-out after the beam display time")]
    [SerializeField] private float beamFadeDuration = 0.25f;

    [Header("Beam Lighting (URP 2D)")]
    [Tooltip("Prefab with a Light2D component (Point) used to light the beam. The prefab will be pooled.")]
    [SerializeField] private GameObject beamLightPrefab;
    [Tooltip("Spacing between spawned lights along the beam in world units")]
    [SerializeField] private float lightSpacing = 1f;
    [Tooltip("Intensity applied to spawned lights")]
    [SerializeField] private float lightIntensity = 1f;
    [Tooltip("Outer radius for spawned point lights")]
    [SerializeField] private float lightOuterRadius = 0.8f;

    [Header("Beam distance")]
    [SerializeField] private float beamMaxDistance = 20f;
    [SerializeField] private float beamDisplayTime = 0.15f; // how long the beam is shown after firing

    [Header("Timings")]
    [Tooltip("Time to wind up before the laser fires")]
    [SerializeField] private float windUpTime = 0.5f;
    [Tooltip("Cooldown after firing the laser")]
    [SerializeField] private float cooldownTime = 1.5f;

    [Header("Cooldown UI")]
    [Tooltip("Optional CooldownText component used to display this weapon's cooldown. Assign a distinct CooldownText per weapon to avoid conflicts.")]
    [SerializeField] private CooldownText cooldownDisplay;

    [Header("Audio")]
    [Tooltip("Optional AudioSource to play the firing sound. If null, PlayClipAtPoint will be used.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sound played when the laser fires")]
    [SerializeField] private AudioClip fireClip;
    [Range(0f, 1f)]
    [SerializeField] private float fireVolume = 1f;

    private Camera mainCamera;

    private Coroutine windupCoroutine;
    private Coroutine beamDisplayCoroutine;
    private Coroutine cooldownCoroutine;

    private bool isCoolingDown = false;

    // line-renderer based beam
    private GameObject lineInstance;
    private LineRenderer lineRenderer;
    private Material lineMaterialInstance;

    // store original visual state for fading
    private Gradient originalLineGradient;
    private Color originalLineMaterialColor = Color.white;

    // pooled 2D lights for beam illumination
    // store pooled light GameObjects so prefab components and properties are preserved
    private readonly List<GameObject> beamLights = new List<GameObject>();
    // store initial intensities for each pooled light so we can fade back to zero from the original value
    private readonly List<float> beamLightInitialIntensity = new List<float>();
    // default intensity read from the prefab so reused lights can be reset correctly
    private float defaultBeamLightIntensity = 1f;

    void Start()
    {
        mainCamera = Camera.main ?? Camera.current;

        // If fireOrigin not assigned, use this transform
        if (fireOrigin == null)
            fireOrigin = transform;

        // try to auto-find a CooldownText in children if not assigned in Inspector
        if (cooldownDisplay == null)
            cooldownDisplay = GetComponentInChildren<CooldownText>(true);

        lineInstance = null;
        lineRenderer = null;
        lineMaterialInstance = null;

        // cache default intensity from prefab if available
        if (beamLightPrefab != null)
        {
            var prefabL2 = beamLightPrefab.GetComponent<Light2D>();
            if (prefabL2 != null)
                defaultBeamLightIntensity = prefabL2.intensity;
        }

        // try to auto-find AudioSource if one wasn't assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
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

        // play firing sound
        if (fireClip != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(fireClip, fireVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(fireClip, origin, fireVolume);
            }
        }

        // show beam visual (always LineRenderer)
        EnsureLineInstance();
        if (lineRenderer != null)
        {
            // Set initial visual immediately
            UpdateLineVisual(origin, dir, beamDistance);
            if (beamDisplayCoroutine != null) StopCoroutine(beamDisplayCoroutine);
            // pass direction and distance so the beam can keep that world direction while following origin position
            beamDisplayCoroutine = StartCoroutine(LineDisplayCoroutine(beamDisplayTime, dir, beamDistance));
        }

        // spawn beam lights along the beam if prefab assigned
        if (beamLightPrefab != null)
        {
            Vector3 endPoint = origin + (Vector3)dir * beamDistance;
            SpawnBeamLights(origin, endPoint); // no immediate disable here; fade coroutine will handle cleanup
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

    private void SpawnBeamLights(Vector3 origin, Vector3 endPoint)
    {
        if (beamLightPrefab == null) return;

        float len = Vector3.Distance(origin, endPoint);
        int count = Mathf.Max(1, Mathf.CeilToInt(len / Mathf.Max(0.0001f, lightSpacing)));

        // ensure pool size
        while (beamLights.Count < count)
        {
            var go = Instantiate(beamLightPrefab);
            // keep unparented (world-space) so positioning works consistently
            go.transform.SetParent(null);
            go.SetActive(false);
            beamLights.Add(go);
            // store default intensity (from prefab) for this new pooled light
            beamLightInitialIntensity.Add(defaultBeamLightIntensity);
        }

        for (int i = 0; i < beamLights.Count; i++)
        {
            var go = beamLights[i];
            if (i < count)
            {
                float t = (i + 0.5f) / count;
                go.transform.position = Vector3.Lerp(origin, endPoint, t);
                // record initial intensity from prefab's Light2D so we can fade from it
                var l2 = go.GetComponent<Light2D>();
                if (l2 != null)
                {
                    // ensure the light uses the stored initial intensity
                    l2.intensity = beamLightInitialIntensity[i];
                }

                // Do not override other prefab light properties (color/radius) — leave them to the prefab
                go.SetActive(true);
            }
            else
            {
                go.SetActive(false);
            }
        }
    }

    private IEnumerator DisableBeamLightsAfter(float time)
    {
        yield return new WaitForSeconds(time);
        foreach (var go in beamLights)
            if (go != null) go.SetActive(false);
    }

    private IEnumerator LineDisplayCoroutine(float time, Vector2 direction, float beamDistance)
    {
        if (lineRenderer == null) yield break;
        lineRenderer.enabled = true;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;

            // update beam positions each frame so origin follows translation but direction remains fixed
            Vector3 origin = fireOrigin != null ? (Vector3)fireOrigin.position : transform.position;
            Vector3 endPoint = origin + (Vector3)direction * beamDistance;
            UpdateLineVisual(origin, direction, beamDistance);

            // update beam lights positions as well so they stay aligned even if player rotates
            if (beamLights.Count > 0)
            {
                float len = Vector3.Distance(origin, endPoint);
                int count = Mathf.Max(1, Mathf.CeilToInt(len / Mathf.Max(0.0001f, lightSpacing)));
                for (int i = 0; i < count && i < beamLights.Count; i++)
                {
                    var go = beamLights[i];
                    if (go != null)
                    {
                        float t = (i + 0.5f) / count;
                        go.transform.position = Vector3.Lerp(origin, endPoint, t);
                    }
                }
            }

            // animate texture offset if material supports it
            if (lineMaterialInstance != null && textureScrollSpeed != 0f)
            {
                Vector2 off = lineMaterialInstance.mainTextureOffset;
                off.x += textureScrollSpeed * Time.deltaTime;
                lineMaterialInstance.mainTextureOffset = off;
            }

            yield return null;
        }

        // Fade out phase
        float fadeElapsed = 0f;
        float fadeDur = Mathf.Max(0.0001f, beamFadeDuration);
        while (fadeElapsed < fadeDur)
        {
            fadeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(1f - (fadeElapsed / fadeDur)); // t goes 1->0

            // ensure beam still follows the player's translation during fade
            Vector3 origin = fireOrigin != null ? (Vector3)fireOrigin.position : transform.position;
            Vector3 endPoint = origin + (Vector3)direction * beamDistance;
            UpdateLineVisual(origin, direction, beamDistance);

            // update beam lights positions during fade as well
            if (beamLights.Count > 0)
            {
                float lenUpd = Vector3.Distance(origin, endPoint);
                int countUpd = Mathf.Max(1, Mathf.CeilToInt(lenUpd / Mathf.Max(0.0001f, lightSpacing)));
                for (int j = 0; j < countUpd && j < beamLights.Count; j++)
                {
                    var goUpd = beamLights[j];
                    if (goUpd != null)
                    {
                        float tt = (j + 0.5f) / countUpd;
                        goUpd.transform.position = Vector3.Lerp(origin, endPoint, tt);
                    }
                }
            }

            // fade material color alpha if available
            if (lineMaterialInstance != null)
            {
                Color c = originalLineMaterialColor;
                c.a *= t;
                // try common properties
                if (lineMaterialInstance.HasProperty("_Color"))
                    lineMaterialInstance.SetColor("_Color", c);
                if (lineMaterialInstance.HasProperty("_BaseColor"))
                    lineMaterialInstance.SetColor("_BaseColor", c);
                // also set material.color as a fallback
                try { lineMaterialInstance.color = c; } catch { }
            }

            // fade gradient alpha keys
            if (originalLineGradient != null)
            {
                GradientAlphaKey[] origA = originalLineGradient.alphaKeys;
                GradientAlphaKey[] newA = new GradientAlphaKey[origA.Length];
                for (int i = 0; i < origA.Length; i++)
                {
                    newA[i].time = origA[i].time;
                    newA[i].alpha = origA[i].alpha * t;
                }

                Gradient g = new Gradient();
                g.SetKeys(originalLineGradient.colorKeys, newA);
                lineRenderer.colorGradient = g;
            }

            // fade lights intensity
            if (beamLights.Count > 0)
            {
                float len2 = Vector3.Distance(origin, endPoint);
                int count2 = Mathf.Max(1, Mathf.CeilToInt(len2 / Mathf.Max(0.0001f, lightSpacing)));
                for (int i2 = 0; i2 < count2 && i2 < beamLights.Count; i2++)
                {
                    var go2 = beamLights[i2];
                    if (go2 != null)
                    {
                        var l2 = go2.GetComponent<Light2D>();
                        if (l2 != null)
                        {
                            float startI = beamLightInitialIntensity[i2];
                            l2.intensity = startI * t;
                        }
                    }
                }
            }

            // continue animating texture offset during fade for visual consistency
            if (lineMaterialInstance != null && textureScrollSpeed != 0f)
            {
                Vector2 off2 = lineMaterialInstance.mainTextureOffset;
                off2.x += textureScrollSpeed * Time.deltaTime;
                lineMaterialInstance.mainTextureOffset = off2;
            }

            yield return null;
        }

        // fully disable visuals
        lineRenderer.enabled = false;

        foreach (var go in beamLights)
        {
            if (go == null) continue;
            var l2 = go.GetComponent<Light2D>();
            if (l2 != null)
            {
                l2.intensity = 0f;
            }
            go.SetActive(false);
        }

        // restore original gradient so next beam starts fully opaque
        if (originalLineGradient != null)
            lineRenderer.colorGradient = originalLineGradient;
        if (lineMaterialInstance != null)
            lineMaterialInstance.color = originalLineMaterialColor;

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

    // Beam visual helpers - LineRenderer based
    private void EnsureLineInstance()
    {
        if (lineInstance != null) return;

        lineInstance = new GameObject("LaserLine_Instance");
        // do not parent to fireOrigin; we'll update the line's world positions each frame so it
        // follows the origin's position but not its rotation.
        lineInstance.transform.SetParent(null);
        lineRenderer = lineInstance.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        // use world space so we can control exact world positions regardless of parent's rotation
        lineRenderer.useWorldSpace = true;
        // remove rounded caps to avoid thickened tips
        lineRenderer.numCapVertices = 0;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Tile;

        // assign material instance
        if (lineMaterial != null)
            lineMaterialInstance = new Material(lineMaterial);
        else
            lineMaterialInstance = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.material = lineMaterialInstance;
        lineRenderer.enabled = false;

        // capture original material color for fade
        try { originalLineMaterialColor = lineMaterialInstance.color; } catch { originalLineMaterialColor = lineColor; }

        // Use a width curve to make the beam fade at the tips.
        float maxW = Mathf.Max(lineStartWidth, lineEndWidth);
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(tipFadeFraction, maxW);
        curve.AddKey(1f - tipFadeFraction, maxW);
        curve.AddKey(1f, 0f);
        lineRenderer.widthCurve = curve;
        lineRenderer.widthMultiplier = 1f;

        // Color gradient to fade alpha at tips
        Gradient grad = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0].color = lineColor;
        colorKeys[0].time = 0f;
        colorKeys[1].color = lineColor;
        colorKeys[1].time = 0.5f;
        colorKeys[2].color = lineColor;
        colorKeys[2].time = 1f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0].alpha = 0f; alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 1f; alphaKeys[1].time = tipFadeFraction;
        alphaKeys[2].alpha = 1f; alphaKeys[2].time = 1f - tipFadeFraction;
        alphaKeys[3].alpha = 0f; alphaKeys[3].time = 1f;

        grad.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = grad;

        // store original gradient for later fade/restore
        originalLineGradient = grad;

        // set base widths (kept for compatibility)
        lineRenderer.startWidth = maxW;
        lineRenderer.endWidth = maxW;
    }

    private void UpdateLineVisual(Vector3 origin, Vector2 dir, float beamDistance)
    {
        if (lineRenderer == null) return;

        Vector3 endPoint = origin + (Vector3)dir * beamDistance;

        // use world-space positions directly
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        lineRenderer.startWidth = lineStartWidth;
        lineRenderer.endWidth = lineEndWidth;

        // ensure material tiling looks reasonable - the texture should be set to repeat
        if (lineMaterialInstance != null)
        {
            // set basic texture scale based on beam length
            float tiling = beamDistance / Mathf.Max(0.0001f, lineTextureUnitLength);
            lineMaterialInstance.mainTextureScale = new Vector2(tiling, 1f);
        }
    }
}
