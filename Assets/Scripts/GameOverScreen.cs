using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    public GameObject panel;

    private void Start()
    {
        // Hide panel at start
        panel.SetActive(false);
    }

    public void Show()
    {
        // DEBUG MESSAGE
        Debug.Log("Game Over Screen Showing");

        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Debug.Log("Restart Button Pressed");

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Debug.Log("Main Menu Button Pressed");

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}