using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 10f;
    public float health;

    public Slider healthSlider;
    public GameObject deathPrefab;
    public GameOverScreen gameOverUI;

    void Start()
    {
        health = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
        else
        {
            Debug.LogWarning("HealthSlider NAV PIEVIENOTS!");
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("TAKING DAMAGE: " + damage);

        health -= damage;

        if (healthSlider != null)
        {
            healthSlider.value = health;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("PLAYER DIED");

        if (deathPrefab != null)
        {
            Instantiate(deathPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("DeathPrefab NAV PIEVIENOTS!");
        }

        if (gameOverUI != null)
        {
            gameOverUI.Show();
        }
        else
        {
            Debug.LogWarning("GameOverUI NAV PIEVIENOTS!");
        }

        Destroy(gameObject);
    }

    public void Heal(float amount)
    {
        health += amount;

        if (health > maxHealth)
            health = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.value = health;
        }
    }
}