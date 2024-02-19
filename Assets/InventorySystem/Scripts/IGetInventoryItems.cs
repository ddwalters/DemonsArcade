using System.Collections.Generic;

namespace Inventory
{
    public interface IGetInventoryItems
    {
        public List<InventoryItemToPlace> GetInventoryItems();
        public void SetInventoryItems(List<InventoryItemToPlace> newItems);
        public InventoryItem GetItemPrefab();
    }

    public interface IGetItemData
    {
        public IInventoryItemData ItemData { get; }
    }
}