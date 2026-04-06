using UnityEngine;
using TMPro;

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter instance;

    public int coins = 0;
    public TMP_Text coinText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize UI
        if (coinText != null)
            coinText.text = "Coins: " + coins;
    }

    public void IncreaseCoins(int amount)
    {
        coins += amount;

        // Update UI
        if (coinText != null)
            coinText.text = "Coins: " + coins;

        Debug.Log("Coins: " + coins);
    }
}