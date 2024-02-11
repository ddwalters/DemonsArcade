using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractionInputData", menuName = "InteractionSystem/InputData")]
public class InteractionInputData : ScriptableObject
{
    private bool _interactClick;
    public bool InteractClick
    {
        get => _interactClick;
        set => _interactClick = value;
    }

    private bool _interactRelease;
    public bool InteractRelease
    {
        get => _interactRelease;
        set => _interactRelease = value;
    }

    public void ResetInput()
    {
        _interactClick = false;
        _interactRelease = false;
    }
}