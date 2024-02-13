using UnityEngine;

public class LootingInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();

        Debug.Log("Open Crate Loot View");
        // populate chest with look

        InteractionController controller = GetComponent<InteractionController>();
        controller.ActivateLootView();
    }
}