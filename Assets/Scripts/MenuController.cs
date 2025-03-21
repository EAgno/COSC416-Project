using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuCanvas; // Assign the Pause Menu Canvas in the Inspector
    private bool isPaused = false;

    void Update()
    {
        pauseMenuCanvas.SetActive(isPaused); // Show the Pause Menu Canvas when game is paused
        if (Input.GetKeyDown(KeyCode.Escape)) // Toggle pause when ESC is pressed
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f; // Freeze the game
            pauseMenuCanvas.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f; // Resume the game
            pauseMenuCanvas.SetActive(false);
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f; // Ensure time resumes when switching scenes
        SceneManager.LoadScene("SampleScene"); // Loads level one scene
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Unity Editor
#endif
        Application.Quit(); // Exits the game
    }
}
