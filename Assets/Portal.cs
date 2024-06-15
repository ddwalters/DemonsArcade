using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField]
    private int selectedScene;

    private LoadLevel GameManager;

    private void Awake()
    {
        GameManager = FindAnyObjectByType<LoadLevel>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") return;
        GameManager.loadLevel(selectedScene);
    }

    public void SetSceneIndex(int scene) => selectedScene = scene;
    public int GetSceneIndex() => selectedScene;
}