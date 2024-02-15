using UnityEngine;

public class LootingInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();

        Debug.Log("Open Crate Loot View");
        // populate chest with loot

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.ToggleLootView();
    }
}