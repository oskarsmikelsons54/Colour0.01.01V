using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelExit : MonoBehaviour
{
    public string nextSceneName;
    public int requiredCoins = 5;
    public float drainSpeed = 0.3f;

    private bool activated = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private float delayAfterOpen = 1f; // seconds to wait after playing the animation

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            if (CoinCounter.instance.HasEnoughCoins(requiredCoins))
            {
                activated = true;
                StartCoroutine(CompleteLevel());
            }
            else
            {
                Debug.Log("Need more coins!");
            }
        }
    }

    IEnumerator CompleteLevel()
    {
        yield return CoinCounter.instance.DrainCoins(requiredCoins, drainSpeed);

        // Play exit/open animation if provided
        if (animator != null && !string.IsNullOrEmpty(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }

        // Wait a short delay to allow the animation to play (configurable)
        if (delayAfterOpen > 0f)
        {
            yield return new WaitForSeconds(delayAfterOpen);
        }

        // Finally load the next scene
        SceneManager.LoadScene(nextSceneName);
    }
}