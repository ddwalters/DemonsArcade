using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public InteractionInputData interactionInputData;
    private InteractionController controller;
    private FirstPersonController playerController;

    void Start()
    {
        controller = FindFirstObjectByType<InteractionController>();
        playerController = FindFirstObjectByType<FirstPersonController>();

        interactionInputData.ResetInput();
    }   

    void Update()
    {
        GetInteractionInputData();
    }

    void GetInteractionInputData()
    {
        // Closes any open menu or interacts with interactable //
        if (!playerController.playerCanMove)
        {
            if (Input.GetKeyDown(KeyCode.E))
                controller.ActivatePlayerHud();
        }
        else
        {
            interactionInputData.InteractClicked = Input.GetKeyDown(KeyCode.E);
        }
        interactionInputData.InteractRelease = Input.GetKeyUp(KeyCode.E);

        // Inventory //
        if (Input.GetKeyDown(KeyCode.I))
            controller.TogglePlayerInventory();
    }
} 
