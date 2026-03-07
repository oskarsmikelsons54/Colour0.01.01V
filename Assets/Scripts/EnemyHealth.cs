using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    // Optional prefab to spawn on death (e.g., death effect, loot, ragdoll)
    public GameObject deathPrefab;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Enemy Hurt");
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // Spawn death prefab at this position/rotation if assigned
        if (deathPrefab != null)
        {
            Instantiate(deathPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject); // Or play death animation
        Debug.Log("Enemy Dead");
    }
}