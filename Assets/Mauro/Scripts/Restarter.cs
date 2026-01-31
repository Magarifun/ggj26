using UnityEngine;

public class Restarter : MonoBehaviour
{
    public KeyCode key = KeyCode.R;

    void Start()
    {
        
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

    }
}
