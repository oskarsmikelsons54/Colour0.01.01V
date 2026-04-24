using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Vector3 checkpointPosition;
    public bool hasCheckpoint = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // saglabājas pēc scene reload
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Vector3 pos)
    {
        checkpointPosition = pos;
        hasCheckpoint = true;

        Debug.Log("Checkpoint saved!");
    }
}