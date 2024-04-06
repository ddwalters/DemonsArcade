using UnityEngine;

public class CollectInteractable : InteractableBase
{
    [SerializeField] ItemData itemData;

    Inventory inventory;

    InventoryManager inventoryManager;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    public override void OnInteract()
    {
        base.OnInteract();

        inventory.AddItemInventoryList(itemData, 0);
        Destroy(gameObject);
    }
}
