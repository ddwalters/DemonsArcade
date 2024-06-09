using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadLevel : MonoBehaviour
{
    public Animator Transition;

    public GameObject player;
    private PlayerController playerController;
    private PlayerStatsManager playerStats;
    private InteractionController playerInteraction;

    public GameObject loadingScreen;
    public Slider slider;
    public TMP_Text textProgress;

    public bool dungeonComplete = false;

    public float transitionTime;

    private void Awake()
    {
        Transition.SetBool("Awake", true);
        DontDestroyOnLoad(this);
    }

    public void loadLevel(int sceneIndex)
    {
        Transition.SetBool("Awake", false);
        StartCoroutine(loadAsynchronously(sceneIndex));
    }

    IEnumerator loadAsynchronously(int sceneIndex)
    {
        yield return new WaitForSeconds(transitionTime);
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
        {
            float progress;

            progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            textProgress.text = progress * 100f + "%";

            yield return null;
        }

        yield return new WaitForSeconds(0.6f);

        loadingScreen.SetActive(false);
        Transition.SetBool("Awake", true);

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>().gameObject;
            playerController = player.GetComponent<PlayerController>();
            playerStats = player.GetComponent<PlayerStatsManager>();
            playerInteraction = player.GetComponent<InteractionController>();
        }

        playerController.GetNewComponents();
        playerStats.GetNewComponents();
        playerInteraction.GetNewComponents();
    }

    public void DungeonComplete()
    {
        dungeonComplete = true;
    }
}
