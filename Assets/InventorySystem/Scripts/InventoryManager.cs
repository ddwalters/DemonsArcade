using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemData> list, InventoryType type)> ItemDataInformation;

    private void Awake()
    {
        if (ItemDataInformation == null)
        {
            var statsData = FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats();
            (List<ItemData>, InventoryType) newPlayerGrid = (new List<ItemData>(), statsData.inventoryType);

            ItemDataInformation = new Dictionary<int, (List<ItemData> list, InventoryType type)>
            {
                { 0, newPlayerGrid }
            };
        }
    }

    /// <summary>
    /// Adds a new inventory item list to the management system
    /// </summary>
    /// <param name="inv">Inventory size using enum type</param>
    /// <returns>Grid id</returns>
    public int AddNewItemList(InventoryType inv)
    {
        ItemDataInformation.Add(ItemDataInformation.Count, (new List<ItemData>(), inv));

        return ItemDataInformation.Count - 1;
    }

    public void UpdateItemData(int itemsId, ItemData data)
    {
        var list = ItemDataInformation[itemsId].list;
        var item = list.FindIndex(x => x.Id == data.Id);

        list[item] = data;
    }

    public void RemoveItems(int itemsId)
    {
        ItemDataInformation.Remove(itemsId);
    }

    public (List<ItemData> list, InventoryType type) GetItems(int itemsId)
    {
        return ItemDataInformation[itemsId];
    }

    public void AddItem(int itemsId, ItemData data)
    {
        ItemDataInformation[itemsId].list.Add(data);
    }
}