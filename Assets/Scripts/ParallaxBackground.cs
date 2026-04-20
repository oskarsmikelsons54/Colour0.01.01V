using UnityEngine;

// Slight parallax for large background sprites so they don't perfectly follow the camera.
// Attach this to the background Sprite GameObject. If the background is currently a child
// of the camera the script will unparent it at Awake() while keeping the same world transform.
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("Camera to follow. If null, main camera will be used.")]
    public Transform cameraTransform;

    [Tooltip("How much the background moves relative to camera movement. (0 = static, 1 = match camera).")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.2f;

    [Tooltip("Smooth factor for following the target position. 0 = instant, 1 = no movement.")]
    [Range(0f, 1f)]
    public float smoothFactor = 0.1f;

    [Header("Optional mouse-based subtle offset (for depth feel)")]
    public bool useMouseOffset = false;
    [Tooltip("Strength of mouse-based offset in world units.")]
    public Vector2 mouseOffsetStrength = new Vector2(0.5f, 0.3f);

    private Vector3 initialCameraPos;
    private Vector3 initialBackgroundPos;
    private Camera cam;

    private void Awake()
    {
        if (cameraTransform == null)
        {
            cam = Camera.main ?? Camera.current;
            if (cam != null)
                cameraTransform = cam.transform;
        }
        else
        {
            cam = cameraTransform.GetComponent<Camera>();
        }

        if (cameraTransform == null)
        {
            Debug.LogWarning("ParallaxBackground: No camera found. Disabling.", this);
            enabled = false;
            return;
        }

        initialCameraPos = cameraTransform.position;
        initialBackgroundPos = transform.position;

        // If object is child of camera, detach it to allow independent movement
        if (transform.parent == cameraTransform)
        {
            transform.SetParent(null, true);
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 camDelta = cameraTransform.position - initialCameraPos;
        Vector3 targetPos = initialBackgroundPos + camDelta * parallaxFactor;

        if (useMouseOffset && cam != null)
        {
            Vector3 mouseScreen = Input.mousePosition;
            Vector3 camScreenCenter = cam.WorldToScreenPoint(cameraTransform.position);

            // normalized offset from center in range approximately [-0.5, 0.5]
            float nx = (mouseScreen.x - camScreenCenter.x) / (float)cam.pixelWidth;
            float ny = (mouseScreen.y - camScreenCenter.y) / (float)cam.pixelHeight;

            Vector3 mouseOffset = new Vector3(nx * mouseOffsetStrength.x * 2f, ny * mouseOffsetStrength.y * 2f, 0f);
            targetPos += mouseOffset;
        }

        // Smoothly move towards target position
        transform.position = Vector3.Lerp(transform.position, targetPos, Mathf.Clamp01(1f - smoothFactor));
    }
}
