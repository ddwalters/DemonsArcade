using UnityEngine;

public class LootingInteractable : InteractableBase
{
    [SerializeField] InventoryType.InvType invType;

    private Inventory inventory;

    private InventoryManager inventoryManager;

    private InteractionController controller;

    private int gridId;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        controller = FindAnyObjectByType<InteractionController>();

        gridId = inventoryManager.AddGrid(invType);
    }

    public override void OnInteract()
    {
        base.OnInteract();

        controller.SetLootingInteracted();

        Cursor.lockState = CursorLockMode.Confined;
        inventory.OpenInventoryGrid(InventoryType.InvType.ThreeByThree, 0, true); // player
        inventory.OpenInventoryGrid(InventoryType.InvType.SixByThree, gridId, false); // chest
    }
}