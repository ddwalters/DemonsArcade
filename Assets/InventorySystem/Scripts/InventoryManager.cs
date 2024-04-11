using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemData>, InventoryType)> ItemDataInformation;

    private void Awake()
    {
        if (ItemDataInformation == null)
        {
            var statsData = FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats();
            (List<ItemData>, InventoryType) newPlayerGrid = (new List<ItemData>(), statsData.inventoryType);

            ItemDataInformation = new Dictionary<int, (List<ItemData>, InventoryType)>
            {
                { 0, newPlayerGrid }
            };
        }
    }

    public int AddItems(InventoryType inv)
    {
        ItemDataInformation.Add(ItemDataInformation.Count, (new List<ItemData>(), inv));

        return ItemDataInformation.Count - 1;
    }

    public void RemoveItems(int itemsId)
    {
        ItemDataInformation.Remove(itemsId);
    }

    public (List<ItemData>, InventoryType) GetItems(int itemsId)
    {
        return ItemDataInformation[itemsId];
    }

    public void SetItems(int itemsId, List<ItemData> data, InventoryType type)
    {
        ItemDataInformation[itemsId] = (data, type);
    }
}