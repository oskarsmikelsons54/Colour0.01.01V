using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

        // 🔥 spawn pie checkpointa
        if (GameManager.instance != null && GameManager.instance.hasCheckpoint)
        {
            transform.position = GameManager.instance.checkpointPosition;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        if (healthSlider != null)
            healthSlider.value = health;

        if (health <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("PLAYER DIED");

        if (deathPrefab != null)
            Instantiate(deathPrefab, transform.position, transform.rotation);

        // 🔥 restartē visu scenu (boss reset utt)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Heal(float amount)
    {
        health += amount;

        if (health > maxHealth)
            health = maxHealth;

        if (healthSlider != null)
            healthSlider.value = health;
    }
}