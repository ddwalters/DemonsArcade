using UnityEngine;

public class LootingInteractable : InteractableBase
{
    [SerializeField] InventoryType invType;

    private Inventory inventory;

    private InventoryManager inventoryManager;

    private InteractionController controller;

    public int gridId;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        controller = FindAnyObjectByType<InteractionController>();
    }

    private void Start()
    {
        gridId = inventoryManager.AddNewGridList(invType);
    }

    public override void OnInteract()
    {
        base.OnInteract();

        controller.SetLootingInteracted();

        Cursor.lockState = CursorLockMode.Confined;
        inventory.CreateGrid(0, true);
        inventory.CreateGrid(gridId, false);
    }
}