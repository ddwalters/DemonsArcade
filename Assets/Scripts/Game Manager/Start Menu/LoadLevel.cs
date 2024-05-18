using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadLevel : MonoBehaviour
{
    public Animator Transition;

    public GameObject loadingScreen;
    public Slider slider;
    public TMP_Text textProgress;

    public float transitionTime;

    public void loadLevel(int sceneIndex)
    {
        StartCoroutine(loadAsynchronously(sceneIndex));
    }

    IEnumerator loadAsynchronously(int sceneIndex)
    {
        Transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        loadingScreen.SetActive(true);

        while (!operation.isDone)
        {
            float progress = operation.progress;
            //float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            textProgress.text = progress * 100f + "%";

            yield return null;
        }

        if (sceneIndex == 2)
        {
            yield return new WaitForSeconds(0.5f);
            slider.value = 1f;
        }
        loadingScreen.SetActive(false);
        Transition.SetTrigger("End");
    }
}
