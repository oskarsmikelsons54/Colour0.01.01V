using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // Optional prefab to spawn on death (e.g., death effect, loot, ragdoll)
    public GameObject deathPrefab;

    // Event invoked when this enemy takes damage. Parameter is damage amount.
    public event Action<int> OnDamaged;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Enemy Hurt");

        // Notify listeners that damage occurred
        OnDamaged?.Invoke(damage);

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