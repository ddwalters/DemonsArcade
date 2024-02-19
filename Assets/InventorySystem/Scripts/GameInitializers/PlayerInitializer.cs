using Inventory;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInitializer : MonoBehaviour, IGetInventoryItems
{
    [SerializeField] private InventoryItem _itemPrefab;
    [SerializeField] private List<InventoryItemToPlace> _itemsToPlace;

    public List<InventoryItemToPlace> GetInventoryItems() => _itemsToPlace;

    public void SetInventoryItems(List<InventoryItemToPlace> newItemsToPlace) => _itemsToPlace = newItemsToPlace;

    public InventoryItem GetItemPrefab() => _itemPrefab;
}
