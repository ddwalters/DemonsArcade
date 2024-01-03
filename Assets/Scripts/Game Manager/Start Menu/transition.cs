using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class transition : MonoBehaviour
{
    public Animator Transition;

    public float transitionTime;

    // Update is called once per frame
    void Update()
    {

    }

    public void ChangeScene()
    {
        StartCoroutine(Loadlevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator Loadlevel(int levelIndex)
    {
        Transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(levelIndex);

        yield return new WaitForSeconds(transitionTime);

        Transition.SetTrigger("End");

    }
}
