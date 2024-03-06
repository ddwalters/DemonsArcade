using Inventory;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MerchantInitializer : InteractableBase
{
    [SerializeField] InventoryItem ItemPrefab;
    [SerializeField] List<InventoryItemToPlace> MerchantItems;
    public override void OnInteract()
    {
        var merchantGrid = GameObject.Find("MerchantInventoryGrid").GetComponent<ItemGrid>();
        merchantGrid.SetInventory(MerchantItems, ItemPrefab, true);

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.MerchantStoreView();
    }
}
