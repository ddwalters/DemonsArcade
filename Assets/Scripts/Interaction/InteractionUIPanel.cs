using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIPanel : MonoBehaviour
{
    private bool isReset;

    [SerializeField] private Image progressBar;

    [SerializeField] private TextMeshProUGUI tooltipText;

    public void SetToolTip(string tooltip)
    {
        tooltipText.SetText(tooltip);
        isReset = false;
    }

    public void UpdateProgressBar(float fillAmount)
    {
        progressBar.fillAmount = fillAmount;
        isReset = false;
    }

    public void ResetUI()
    {
        if (!isReset)
        {
            progressBar.fillAmount = 0;
            tooltipText.SetText("");
        }
    }
}
