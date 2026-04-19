using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int health;

    public Slider healthSlider;   // UI slider reference
    public GameObject deathPrefab;

    public GameOverScreen gameOverUI; // 👈 ADD THIS

    void Start()
    {
        health = maxHealth;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = health;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        healthSlider.value = health;

        if (health <= 0)
        {
            Die(); // 👈 cleaner
        }
    }

    void Die()
    {
        // Spawn death effect
        if (deathPrefab != null)
        {
            Instantiate(deathPrefab, transform.position, transform.rotation);
        }

        // Show Game Over UI
        if (gameOverUI != null)
        {
            gameOverUI.Show();
        }

        // Destroy player
        Destroy(gameObject);
    }

    public void Heal(int amount)
    {
        health += amount;

        if (health > maxHealth)
            health = maxHealth;

        healthSlider.value = health;
    }
}