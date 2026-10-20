using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void loadScene(int sceneNum)
    {
        SaveManager.Instance?.CaptureWorld();
        SceneManager.LoadScene(sceneNum);
    }
}
