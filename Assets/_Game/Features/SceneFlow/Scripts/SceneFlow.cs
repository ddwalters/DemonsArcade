using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlow
{
    private static AsyncOperation pendingLoad;
    public static bool IsLoading => pendingLoad != null && !pendingLoad.isDone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => pendingLoad = null;

    public static AsyncOperation Load(int buildIndex, bool captureSession = true)
    {
        if (IsLoading)
            return pendingLoad;

        if (captureSession)
            SaveManager.Instance?.CaptureWorld();
        Object.FindAnyObjectByType<PlayerModeController>()?.SetMode(PlayerMode.Gameplay);
        pendingLoad = SceneManager.LoadSceneAsync(buildIndex);
        return pendingLoad;
    }
}
