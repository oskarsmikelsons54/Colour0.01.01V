using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelExit : MonoBehaviour
{
    public string nextSceneName;
    public int requiredCoins = 5;
    public float drainSpeed = 0.3f;

    private bool activated = false;

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

        SceneManager.LoadScene(nextSceneName);
    }
}