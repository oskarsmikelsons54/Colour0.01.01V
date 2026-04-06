using UnityEngine;

public class SpikeKill : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Debug: log everything that enters the spike trigger
        Debug.Log("Trigger entered by: " + other.name);

        // Check if it’s the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player touched spikes! Destroying...");

            // Destroy the player
            Destroy(other.gameObject);
        }
    }
}