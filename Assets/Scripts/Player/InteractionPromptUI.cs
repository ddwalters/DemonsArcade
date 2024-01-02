using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _promtText;

    void Start()
    {
        _promtText.text = "";
    }

    public void SetUp(string promptText)
    {
        _promtText.text = promptText;
    }

    public void Close()
    {
        _promtText.text = "";
    }
}
