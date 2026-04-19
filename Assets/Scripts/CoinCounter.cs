using UnityEngine;
using TMPro;
using System.Collections;

public class CoinCounter : MonoBehaviour
{
    public static CoinCounter instance;

    public int coins = 0;
    public TMP_Text coinText;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        UpdateUI();
    }

    public void IncreaseCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    public bool HasEnoughCoins(int amount)
    {
        return coins >= amount;
    }

    public IEnumerator DrainCoins(int amount, float delay)
    {
        for (int i = 0; i < amount; i++)
        {
            coins--;
            UpdateUI();
            yield return new WaitForSeconds(delay);
        }
    }

    public void ResetCoins()
    {
        coins = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinText != null)
            coinText.text = "Coins: " + coins;
    }
}