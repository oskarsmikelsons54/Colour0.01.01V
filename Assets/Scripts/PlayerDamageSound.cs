using UnityEngine;

public class PlayerDamageSound : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public AudioSource audioSource;
    public AudioClip damageSound;

    private float lastHealth;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        lastHealth = playerHealth.health;
    }

    private void Update()
    {
        if (playerHealth == null) return;

        if (playerHealth.health < lastHealth)
        {
            if (audioSource != null && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
        }

        lastHealth = playerHealth.health;
    }
}