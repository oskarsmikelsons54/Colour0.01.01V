using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
    public AudioClip coinSound;

    private bool collected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!collected && other.CompareTag("Player"))
        {
            collected = true;

            if (CoinCounter.instance != null)
            {
                CoinCounter.instance.IncreaseCoins(value);
            }

            // Play coin pickup sound
            if (coinSound != null)
            {
                AudioSource.PlayClipAtPoint(coinSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}