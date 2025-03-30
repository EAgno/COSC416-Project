using UnityEngine;
using TMPro;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;

public class HudController : MonoBehaviour
{
    [SerializeField] private int lives = 3; // Initial lives
    [SerializeField] private TextMeshProUGUI livesText; // Drag your UI text object here

    void Start()
    {
        UpdateLivesUI();
    }

    public void LoseLife()
    {
        if (lives > 0)
        {
            lives--;
            UpdateLivesUI();
        }

        if (lives <= 0)
        {
            // Load the you lose scene
            SceneManager.LoadScene("LoseScreen");
        }
    }

    private void UpdateLivesUI()
    {
        livesText.text = lives.ToString();
    }
}

