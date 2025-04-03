using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public void LoadPlayScene()
    {
        SceneManager.LoadScene("Play Scene"); 
    }
    public void RestartScene()
    {
        SceneManager.LoadScene("Start Scene");
    }
}
