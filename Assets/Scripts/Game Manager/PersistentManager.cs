using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PersistentManager : MonoBehaviour
{
    public static PersistentManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to the sceneLoaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Unsubscribe from the sceneLoaded event
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Call a method to find the required GameObjects in the new scene
        FindRequiredGameObjects();
    }

    private void FindRequiredGameObjects()
    {
        var InteractionController = FindAnyObjectByType<InteractionController>();
        InteractionController.SetGameObjectsOnSceneLoad();

        var playerStatsManager = FindAnyObjectByType<PlayerStatsManager>();
        playerStatsManager.SetGameObjectsOnSceneLoad();

    }
}