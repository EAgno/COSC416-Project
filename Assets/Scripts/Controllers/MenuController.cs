using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuCanvas; // Optional: Assign in Inspector
    private bool isPaused = false; // Start unpaused

    void Start()
    {
        if (pauseMenuCanvas == null)
        {
            Debug.LogWarning("Pause menu canvas is not assigned in the Inspector.");
        }
        else
        {
            pauseMenuCanvas.SetActive(false); // Ensure it starts hidden
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Toggle pause when ESC is pressed
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f; // Pause or resume game

        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(isPaused);
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f; // Ensure time resumes when switching scenes
        SceneManager.LoadScene("Level1"); // Loads level one scene
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Unity Editor
#endif
        Application.Quit(); // Exits the game
    }
}
