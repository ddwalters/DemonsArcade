using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemSaveData> list, InventoryType type)> ItemDataInformation;

    private void Awake()
    {
        if (ItemDataInformation == null)
        {
            var statsData = FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats();
            (List<ItemSaveData>, InventoryType) newPlayerGrid = (new List<ItemSaveData>(), statsData.inventoryType);
            (List<ItemSaveData>, InventoryType) HeadSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) ChestSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) LegSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) BootSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) NecklaceSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) RightHandSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);
            (List<ItemSaveData>, InventoryType) LeftHandSlot = (new List<ItemSaveData>(), InventoryType.OneByOneMinimal);

            ItemDataInformation = new Dictionary<int, (List<ItemSaveData> list, InventoryType type)>
            {
                { 0, newPlayerGrid },
                { 1, HeadSlot },
                { 2, ChestSlot },
                { 3, LegSlot },
                { 4, BootSlot },
                { 5, NecklaceSlot },
                { 6, RightHandSlot },
                { 7, LeftHandSlot }
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
        ItemDataInformation.Add(ItemDataInformation.Count, (new List<ItemSaveData>(), inv));

        return ItemDataInformation.Count - 1;
    }

    public void UpdateItemData(int gridId, ItemSaveData data)
    {
        var list = ItemDataInformation[gridId].list;
        var item = list.FindIndex(x => x.Id == data.Id);

        list[item] = data;
    }

    public void RemoveItems(int itemsId)
    {
        ItemDataInformation.Remove(itemsId);
    }

    public (List<ItemSaveData> list, InventoryType type) GetItems(int itemsId)
    {
        return ItemDataInformation[itemsId];
    }

    public void AddItem(int gridId, ItemSaveData data)
    {
        var list = ItemDataInformation[gridId].list;

        data.Id = list.Count - 1;
        list.Add(data);
    }
}