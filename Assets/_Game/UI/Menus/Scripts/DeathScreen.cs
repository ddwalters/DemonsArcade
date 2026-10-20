using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    public Button respawnButton;
    public CanvasGroup respawnGroup;
    public TMP_Text status;
    [SerializeField] private float buttonDelay = 1f;
    [SerializeField] private float fadeDuration = 0.2f;

    private void Awake() => respawnButton.onClick.AddListener(Respawn);

    private void OnEnable()
    {
        respawnGroup.alpha = 0f;
        respawnGroup.interactable = false;
        respawnGroup.blocksRaycasts = false;
        status.text = "";
        StartCoroutine(RevealButton());
    }

    private IEnumerator RevealButton()
    {
        yield return new WaitForSecondsRealtime(buttonDelay);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            respawnGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        respawnGroup.alpha = 1f;
        respawnGroup.interactable = true;
        respawnGroup.blocksRaycasts = true;
        respawnButton.Select();
    }

    private void Respawn()
    {
        if (SaveManager.Instance.Respawn())
            respawnGroup.interactable = false;
        else
            status.text = SaveManager.Instance.LastError;
    }
}
