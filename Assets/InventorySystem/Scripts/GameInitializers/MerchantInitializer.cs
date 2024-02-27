using Inventory;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MerchantInitializer : InteractableBase
{
    [SerializeField] InventoryItem ItemPrefab;
    [SerializeField] List<InventoryItemToPlace> MerchantItems;
    public override void OnInteract()
    {
        List<ItemGrid> grids = FindObjectsByType<ItemGrid>(FindObjectsSortMode.None).ToList();
        var merchantGrid = grids.FirstOrDefault(x => x.CompareTag("MerchantInventory"));

        merchantGrid.RemoveInventoryItems();
        merchantGrid.SetInventory(MerchantItems, ItemPrefab, true);

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.ToggleMerchantView();
    }
}
