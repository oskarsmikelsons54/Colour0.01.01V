using UnityEngine;

public class HealthPickupSound : MonoBehaviour
{
    public AudioClip healthPickupSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (healthPickupSound != null)
            {
                AudioSource.PlayClipAtPoint(healthPickupSound, transform.position);
            }
        }
    }
}