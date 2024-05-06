using System.Collections.Generic;
using UnityEngine;

public class MerchantInteractable : InteractableBase
{
    [SerializeField] List<ItemData> items;

    private Inventory inventory;

    private InventoryManager inventoryManager;

    private InteractionController controller;

    private int gridId;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        controller = FindAnyObjectByType<InteractionController>();
    }

    private void Start()
    {
        gridId = inventoryManager.AddNewGridList(InventoryType.FourByFourFull);

        foreach (var itemData in items)
        {
            inventory.AddItem(gridId, itemData);
        }
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