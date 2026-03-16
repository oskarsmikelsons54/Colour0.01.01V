using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int health;

    public Slider healthSlider;   // UI slider reference

    public GameObject deathPrefab;

    void Start()
    {
        health = maxHealth;

        // Set slider values
        healthSlider.maxValue = maxHealth;
        healthSlider.value = health;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        // Update UI
        healthSlider.value = health;

        if (health <= 0)
        {
            if (deathPrefab != null)
            {
                Instantiate(deathPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }
    }
}