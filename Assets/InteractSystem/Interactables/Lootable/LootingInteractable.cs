using Inventory;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootingInteractable : InteractableBase
{
    [SerializeField] InventoryItem ItemPrefab;
    [SerializeField] List<InventoryItemToPlace> MerchantItems;

    public override void OnInteract()
    {
        base.OnInteract();

        List<ItemGrid> grids = FindObjectsByType<ItemGrid>(FindObjectsSortMode.None).ToList();
        var merchantGrid = grids.FirstOrDefault(x => x.CompareTag("CrateInventory"));

        merchantGrid.SetInventory(MerchantItems, ItemPrefab, false);

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.ToggleLootView();
    }
}