using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
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

            Destroy(gameObject);
        }
    }
}