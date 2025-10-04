using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    // Check for any input to start the game
    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene(1);  // Load level 1
        }
    }
}
