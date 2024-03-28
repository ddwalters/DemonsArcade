using UnityEngine;

public class LootingInteractable : InteractableBase
{
    private Inventory inventory;

    // use later when opening grids that have Items 
    //private InventoryManager inventoryManager;

    private InteractionController controller;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        //inventoryManager = FindAnyObjectByType<InventoryManager>();
        controller = FindAnyObjectByType<InteractionController>();
    }

    public override void OnInteract()
    {
        base.OnInteract();

        controller.SetLootingInteracted();

        Cursor.lockState = CursorLockMode.Confined;
        inventory.OpenInventoryGrid(InventoryType.InvType.ThreeByThree, null, true);
        inventory.OpenInventoryGrid(InventoryType.InvType.SixByThree, null, false);
    }
}