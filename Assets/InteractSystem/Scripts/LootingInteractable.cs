using UnityEngine;

public class LootingInteractable : InteractableBase
{
    public override void OnInteract()
    {
        base.OnInteract();
        Debug.Log("Loot");
    }
}