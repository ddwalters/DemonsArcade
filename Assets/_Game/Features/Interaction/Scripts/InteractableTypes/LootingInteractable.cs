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
    public int GridId => gridId;

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
