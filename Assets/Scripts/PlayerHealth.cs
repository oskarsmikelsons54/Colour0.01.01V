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

    private bool isDead = false;

    void Start()
    {
        health = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        if (healthSlider != null)
            healthSlider.value = health;

        if (health <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("PLAYER DIED");

        if (deathPrefab != null)
            Instantiate(deathPrefab, transform.position, transform.rotation);

        // Show the game-over UI if assigned; otherwise fallback to immediate reload
        if (gameOverUI != null)
        {
            gameOverUI.Show();

            // Optionally disable player components so they can't move/act while the panel is shown:
            // GetComponent<PlayerMovement>()?.enabled = false;
            // GetComponent<PlayerMeleeAttack>()?.enabled = false;
        }
        else
        {
            // immediate scene reload (restores previous behavior)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        health += amount;

        if (health > maxHealth)
            health = maxHealth;

        if (healthSlider != null)
            healthSlider.value = health;
    }
}