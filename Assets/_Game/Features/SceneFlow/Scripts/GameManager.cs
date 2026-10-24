using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void loadScene(int sceneNum)
    {
        var transition = FindAnyObjectByType<LoadLevel>();
        if (transition != null)
            transition.loadLevel(sceneNum);
        else
            SceneFlow.Load(sceneNum);
    }
}
