using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        GlobalReferences.Initialize();
    }

    public void loadScene(int sceneNum)
    {
        SceneManager.LoadScene(sceneNum);
    }
}
