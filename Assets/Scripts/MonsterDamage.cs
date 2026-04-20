using UnityEngine;

public class MonsterDamage : MonoBehaviour
{
    public int damage;
    private PlayerHealth playerHealth;

    void Start()
    {
        // Try to auto-assign the Player's health component by tag
        if (playerHealth == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null)
                playerHealth = found.GetComponent<PlayerHealth>();
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)   
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // If not yet assigned, try to get PlayerHealth from the collided object
            if (playerHealth == null)
            {
                playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }   
    }
}
