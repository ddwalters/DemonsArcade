using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public InteractionInputData interactionInputData;

    void Start()
    {
        interactionInputData.ResetInput();
    }

    void Update()
    {
        GetInteractionInputData();
    }

    void GetInteractionInputData()
    {
        interactionInputData.InteractClicked = Input.GetKeyDown(KeyCode.Tab);
        interactionInputData.InteractRelease = Input.GetKeyUp(KeyCode.Tab);
    }
} 
