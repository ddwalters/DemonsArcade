using System.Collections.Generic;

namespace Inventory
{
    public interface IGetInventoryItems
    {
        public List<InventoryItemToPlace> GetInventoryItems();
        public InventoryItem GetItemPrefab();
    }

    public interface IGetItemData
    {
        public IInventoryItemData ItemData { get; }
    }
}