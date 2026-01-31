using UnityEngine;

public class Restarter : MonoBehaviour
{
    public KeyCode key = KeyCode.R;

    void Start()
    {
        
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
