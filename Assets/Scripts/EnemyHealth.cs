using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

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
        Destroy(gameObject); // Or play death animation
        Debug.Log("Enemy Dead");
    }
}