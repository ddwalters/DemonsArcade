using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    private int selectedScene;
    private LoadLevel GameManager;

    public void SetScene(int scene)
    {
        selectedScene = scene;
    }

    public int GetScene()
    {
        return selectedScene;
    }

    private void Awake()
    {
        GameManager = FindAnyObjectByType<LoadLevel>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "HitCone") return;
        GameManager.loadLevel(selectedScene);
    }
}