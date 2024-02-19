using Inventory;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MerchantInitializer : InteractableBase
{
    [SerializeField] List<InventoryItemToPlace> MerchantItems;
    [SerializeField] Vector2Int MerchantGridSize;
    public override void OnInteract()
    {
        List<ItemGrid> grids = FindObjectsByType<ItemGrid>(FindObjectsSortMode.None).ToList();
        var merchantGrid = grids.FirstOrDefault(x => x.CompareTag("MerchantInventory"));

        merchantGrid.SetInventory(MerchantItems, MerchantGridSize);
    }
}
