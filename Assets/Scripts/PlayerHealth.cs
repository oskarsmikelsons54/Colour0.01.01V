using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int health;

    // Optional prefab to spawn when the player is destroyed (e.g., death effect or ragdoll)
    public GameObject deathPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            // Spawn the death prefab at the same position/rotation if provided
            if (deathPrefab != null)
            {
                Instantiate(deathPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }
    }
    
}
