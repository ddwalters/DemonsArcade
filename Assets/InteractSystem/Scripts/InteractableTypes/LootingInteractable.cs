using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;

public class LootingInteractable : InteractableBase
{
    [SerializeField] InventoryType invType;

    [SerializeField] List<ItemData> items;

    [SerializeField] bool isRewardChest;
    [SerializeField] int minChestReward;
    [SerializeField] int maxChestReward;

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
        gridId = inventoryManager.AddNewGridList(invType);

        if (isRewardChest)
        {
            int itemRange = Random.Range(minChestReward, maxChestReward);

            for (int i = itemRange; i >= 0; i--)
            {
                int randIndex = Random.Range(0, items.Count-1);
                ItemData randomItem = items[randIndex];
                inventory.AddItem(gridId, randomItem);
            }
            return;
        }

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