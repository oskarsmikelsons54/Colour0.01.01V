using System.Collections;
using UnityEngine;

public class AtkAnim : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Animator animator;

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!animator) animator = GetComponent<Animator>();

        sr.enabled = false; // start hidden
    }

    // Call this from your player attack code:
    public void Play()
    {
        if (!animator || !sr)
        {
            Debug.LogWarning("AtkAnim.Play called but Animator or SpriteRenderer is missing.");
            if (sr) sr.enabled = true;
            return;
        }

        sr.enabled = true;

        // Ensure animator is able to play normally
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.speed = 1f;

        // Preferred: use a trigger-based transition (make sure your controller has a transition on "Play" to the Attack state)
        animator.ResetTrigger("Play");
        animator.SetTrigger("Play");

        // As a fallback (if you prefer direct state play), uncomment the next line
        // animator.Play("Attack", 0, 0f);

        // Start a short logger to verify the animator progresses
        StartCoroutine(LogAnimatorState());
    }

    private IEnumerator LogAnimatorState()
    {
        // Inspect the animator for a few frames to confirm normalizedTime is increasing
        for (int i = 0; i < 8; i++)
        {
            if (animator == null) yield break;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[AtkAnim] StateIsAttack={info.IsName("Attack")}, normalizedTime={info.normalizedTime:F2}, speed={animator.speed}");
            yield return null;
        }
    }

    // Animation Event at end of clip:
    public void Hide()
    {
        Debug.Log("[AtkAnim] Hide() called via AnimationEvent");
        sr.enabled = false;
        if (animator != null)
        {
            animator.ResetTrigger("Play");
        }
    }
}