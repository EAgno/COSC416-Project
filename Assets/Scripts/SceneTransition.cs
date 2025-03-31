using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string nextScene = "Level2"; // Set the next scene
    public float transitionX = 102.4f; // X-coordinate to trigger transition

    void Update()
    {
        // Only trigger scene change in Level1
        if (SceneManager.GetActiveScene().name == "Level1" && transform.position.x >= transitionX)
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
