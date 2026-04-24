using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool playerInside = false;

    void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            GameManager.instance.SetCheckpoint(transform.position);
            Debug.Log("Checkpoint ACTIVATED (E)");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log("Press E to activate checkpoint");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
}