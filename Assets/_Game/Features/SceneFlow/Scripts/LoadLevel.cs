using System.Collections;
using UnityEngine;

// Transition state survives activation; the scene-authored UI binds to that state.
public class LoadLevel : MonoBehaviour
{
    private static LoadLevel current;
    public bool dungeonComplete;
    public float transitionTime;
    private bool isLoading;
    private bool showProgress;
    private float progress;
    private SceneTransitionView view;
    public bool IsLoading => isLoading;

    private void Awake()
    {
        if (current != null && current != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        current = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        if (view == null)
            view = FindAnyObjectByType<SceneTransitionView>();
        if (view != null)
            view.Apply(isLoading, showProgress, progress);
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    public void loadLevel(int sceneIndex)
    {
        if (isLoading || SceneFlow.IsLoading)
            return;
        isLoading = true;
        StartCoroutine(LoadAsynchronously(sceneIndex));
    }

    private IEnumerator LoadAsynchronously(int sceneIndex)
    {
        AsyncOperation operation = SceneFlow.Load(sceneIndex);
        operation.allowSceneActivation = false;
        if (transitionTime > 0f)
            yield return new WaitForSecondsRealtime(transitionTime);
        showProgress = true;
        progress = 0f;
        yield return null;
        while (operation.progress < 0.9f)
        {
            progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
        progress = 1f;
        operation.allowSceneActivation = true;
        yield return operation;
        showProgress = false;
        isLoading = false;
    }

    public void DungeonComplete() => dungeonComplete = true;
}
