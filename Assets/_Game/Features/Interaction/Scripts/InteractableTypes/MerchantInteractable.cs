using System.Collections.Generic;
using UnityEngine;

public class MerchantInteractable : InteractableBase
{
    [SerializeField] List<ItemData> items;

    private Inventory inventory;

    private InventoryManager inventoryManager;

    private InteractionController controller;

    private int gridId;
    public int GridId => gridId;

    public override InteractionWorldSpaceNameType WorldSpaceNameType => InteractionWorldSpaceNameType.Npc;

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

    private void OnDestroy()
    {
        if (inventoryManager != null)
            inventoryManager.RemoveGrid(gridId);
    }

    public override void OnInteract()
    {
        base.OnInteract();

        controller.OpenLoot(gridId);
    }
}
