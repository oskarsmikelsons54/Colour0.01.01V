using UnityEngine;

public class SpikeKill : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Spike trigger entered by: " + other.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player touched spikes!");

            PlayerHealth player = other.GetComponent<PlayerHealth>();

            if (player != null)
            {
                player.TakeDamage(player.health); // instant kill
            }
        }
    }
}