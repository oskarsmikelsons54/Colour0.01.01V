using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1; // Coin value
    private bool collected = false; // Prevent double collection

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only allow collection by player
        if (!collected && other.CompareTag("Player"))
        {
            collected = true; // Mark as collected

            // Increase coin count
            CoinCounter.instance.IncreaseCoins(value);

            // Optional: Play sound or animation here

            // Destroy coin
            Destroy(gameObject);
        }
    }
}