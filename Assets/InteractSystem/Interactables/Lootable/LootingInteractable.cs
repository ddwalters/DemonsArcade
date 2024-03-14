using System.Collections.Generic;
using UnityEngine;

public class LootingInteractable : InteractableBase
{
    //[SerializeField] InventoryItem ItemPrefab;
    //[SerializeField] List<InventoryItemToPlace> LootItems;

    public override void OnInteract()
    {
        base.OnInteract();

        //var lootGrid = GameObject.Find("LootInventoryGrid").GetComponent<ItemGrid>();
        //lootGrid.SetInventory(LootItems, ItemPrefab, false);

        InteractionController controller = FindFirstObjectByType<InteractionController>();
        controller.LootInventoryView();
    }
}