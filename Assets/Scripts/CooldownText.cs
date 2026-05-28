using System.Collections;
using UnityEngine;
using TMPro;

// Reusable textual cooldown display. Attach to a GameObject with a TextMeshPro component.
// It will render something like: [||||||||||-------]
public class CooldownText : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private int segments = 10;
    [SerializeField] private char fillChar = '|';
    [SerializeField] private char emptyChar = '-';
    [SerializeField] private bool hideWhenReady = true;

    private Coroutine cooldownRoutine;

    void Reset()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogWarning("CooldownText requires a TMP_Text component on the same GameObject.");
            enabled = false;
            return;
        }

        // IMPORTANT: don't deactivate the GameObject. If the GameObject is inactive the
        // MonoBehaviour cannot start coroutines (Unity throws the error you saw).
        // Use the TMP_Text component's enabled flag to hide the visual while keeping
        // this component active so coroutines can run.
        if (hideWhenReady)
            textComponent.enabled = false;
    }

    public void StartCooldown(float duration)
    {
        if (cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);

        cooldownRoutine = StartCoroutine(CooldownCoroutine(duration));
    }

    public bool IsOnCooldown => cooldownRoutine != null;

    private IEnumerator CooldownCoroutine(float duration)
    {
        if (textComponent == null)
            yield break;

        if (hideWhenReady)
            textComponent.enabled = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // use unscaled so it still progresses if timeScale changes
            float t = Mathf.Clamp01(elapsed / duration);
            UpdateText(t);
            yield return null;
        }

        UpdateText(1f);

        // finished
        cooldownRoutine = null;

        if (hideWhenReady)
            textComponent.enabled = false;
    }

    private void UpdateText(float progress)
    {
        int filled = Mathf.RoundToInt(progress * segments);
        filled = Mathf.Clamp(filled, 0, segments);
        int empty = segments - filled;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (int i = 0; i < filled; i++) sb.Append(fillChar);
        for (int i = 0; i < empty; i++) sb.Append(emptyChar);
        sb.Append(']');

        textComponent.text = sb.ToString();
    }
}
