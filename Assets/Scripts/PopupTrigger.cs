using UnityEngine;

public class PopupTrigger : MonoBehaviour
{
    [SerializeField] private GameObject objectToShow;

    private void Start()
    {
        if (objectToShow != null)
        {
            objectToShow.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && objectToShow != null)
        {
            objectToShow.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && objectToShow != null)
        {
            objectToShow.SetActive(false);
        }
    }
}