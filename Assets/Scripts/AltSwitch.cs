using UnityEngine;

// Simple component to switch the color of a SpriteRenderer using keyboard shortcuts.
public class AltSwitch : MonoBehaviour
{
    // The SpriteRenderer whose color will be changed. If null, this component will try to get one from the same GameObject.
    public SpriteRenderer targetRenderer;

    // Two alternate colors to switch between in play mode.
    public Color color1 = Color.white;
    public Color color2 = Color.red;

    void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer != null)
        {
            SetColor1();
        }
        else
        {
            Debug.LogWarning("AltSwitch: No SpriteRenderer assigned or found on the GameObject.");
        }
    }

    void Update()
    {
        // Press C to toggle between the two inspector-configured colors.
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleColor();
        }
    }

    public void SetColor1()
    {
        if (targetRenderer != null) targetRenderer.color = color1;
    }

    public void SetColor2()
    {
        if (targetRenderer != null) targetRenderer.color = color2;
    }

    public void ToggleColor()
    {
        if (targetRenderer == null) return;
        targetRenderer.color = targetRenderer.color == color1 ? color2 : color1;
    }
}
