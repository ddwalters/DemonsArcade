using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractionInputData", menuName = "InteractionSystem/InputData")]
public class InteractionInputData : ScriptableObject
{
    private bool _interactClicked;
    public bool InteractClicked
    {
        get => _interactClicked;
        set => _interactClicked = value;
    }

    private bool _interactRelease;
    public bool InteractRelease
    {
        get => _interactRelease;
        set => _interactRelease = value;
    }

    public void ResetInput()
    {
        _interactClicked = false;
        _interactRelease = false;
    }
}