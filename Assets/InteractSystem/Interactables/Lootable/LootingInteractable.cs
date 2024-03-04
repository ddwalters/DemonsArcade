using Inventory;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootingInteractable : InteractableBase
{
    [SerializeField] InventoryItem ItemPrefab;
    [SerializeField] List<InventoryItemToPlace> LootItems;

    public override void OnInteract()
    {
        base.OnInteract();

        List<ItemGrid> grids = FindObjectsByType<ItemGrid>(FindObjectsSortMode.None).ToList();
        var lootGrid = grids.FirstOrDefault(x => x.CompareTag("CrateInventory"));

        lootGrid.SetInventory(LootItems, ItemPrefab, false);

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.LootInventoryView();
    }
}