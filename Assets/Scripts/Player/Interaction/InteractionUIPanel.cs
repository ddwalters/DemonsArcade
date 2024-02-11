using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUIPanel : MonoBehaviour
{
    [SerializeField] private Image progressBar;

    [SerializeField] private TextMeshProUGUI tooltipText;

    public void SetToolTip(string tooltip)
    {
        tooltipText.SetText(tooltip);
    }

    public void UpdateProgressBar(float fillAmount)
    {
        progressBar.fillAmount = fillAmount;
    }

    public void ResetUI()
    {
        progressBar.fillAmount = 0;

        //allocates new empty string every frame, there should be a good way to only do this once.
        tooltipText.SetText("");
    }
}
