using UnityEngine;

public class CollectInteractable : InteractableBase
{
    [SerializeField] ItemData itemData;
    [SerializeField] Inventory playerInventory;
    public override void OnInteract()
    {
        base.OnInteract();

        playerInventory.AddItem(itemData);
        Destroy(gameObject);
    }
}
