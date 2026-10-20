using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadLevel : MonoBehaviour
{
    public Animator Transition;

    public GameObject player;
    private PlayerStatsManager playerStats;
    private InteractionController playerInteraction;

    public GameObject loadingScreen;
    public Slider slider;
    public TMP_Text textProgress;

    public bool dungeonComplete = false;

    public float transitionTime;

    private bool isLoading;
    public bool IsLoading => isLoading;

    private void Awake()
    {
        Transition.SetBool("Awake", true);
        DontDestroyOnLoad(this);
    }

    public void loadLevel(int sceneIndex)
    {
        if (isLoading)
            return;

        SaveManager.Instance?.CaptureWorld();
        isLoading = true;
        FindAnyObjectByType<PlayerModeController>()?.SetMode(PlayerMode.Gameplay);
        Transition.SetBool("Awake", false);
        StartCoroutine(loadAsynchronously(sceneIndex));
    }

    IEnumerator loadAsynchronously(int sceneIndex)
    {
        // Load while the closing animation plays, then activate behind the loading screen.
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        if (transitionTime > 0f)
            yield return new WaitForSecondsRealtime(transitionTime);

        slider.value = 0f;
        textProgress.text = "0%";
        loadingScreen.SetActive(true);
        yield return null;

        while (operation.progress < 0.9f)
        {
            float progress;

            progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            textProgress.text = progress * 100f + "%";

            yield return null;
        }

        slider.value = 1f;
        textProgress.text = "100%";
        operation.allowSceneActivation = true;
        yield return operation;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>().gameObject;
        }

        playerStats = player.GetComponent<PlayerStatsManager>();
        playerInteraction = player.GetComponent<InteractionController>();

        playerStats.GetNewComponents();
        playerInteraction.GetNewComponents();

        loadingScreen.SetActive(false);
        Transition.SetBool("Awake", true);
        isLoading = false;
    }

    public void DungeonComplete()
    {
        dungeonComplete = true;
    }
}
