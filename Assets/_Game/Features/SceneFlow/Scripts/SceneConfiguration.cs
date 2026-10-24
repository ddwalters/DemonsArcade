using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneConfiguration : MonoBehaviour
{
    public bool gameplayScene = true;
    public bool saveProgress = true;
    public bool createInitialSave = true;

    public static SceneConfiguration ForScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var configuration = root.GetComponentInChildren<SceneConfiguration>(true);
            if (configuration != null)
                return configuration;
        }
        return null;
    }
}
