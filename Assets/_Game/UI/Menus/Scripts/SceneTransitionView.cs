using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SceneTransitionView : MonoBehaviour
{
    [SerializeField] private Animator transition;
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider progress;
    [SerializeField] private TMP_Text progressText;

    private void Awake() => Apply(false, false, 0f);

    public void Apply(bool closing, bool showProgress, float amount)
    {
        transition.SetBool("Awake", !closing);
        loadingScreen.SetActive(showProgress);
        progress.value = amount;
        progressText.text = Mathf.RoundToInt(amount * 100f) + "%";
    }
}
