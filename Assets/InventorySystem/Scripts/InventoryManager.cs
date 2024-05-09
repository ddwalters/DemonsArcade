using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemSaveData> list, InventoryType type)> ItemDataInformation;

    [SerializeField] ItemData HelmetItemPrefab;
    [SerializeField] ItemData ChestpieceItemPrefab;
    [SerializeField] ItemData LeggingsItemPrefab;
    [SerializeField] ItemData BootsItemPrefab;
    [SerializeField] ItemData NecklaceItemPrefab;
    [SerializeField] ItemData WeaponItemPrefab;

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
    public int AddNewGridList(InventoryType inv)
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

    public void RemoveGrid(int gridId)
    {
        ItemDataInformation.Remove(gridId);
    }

    public (List<ItemSaveData> list, InventoryType type) GetItems(int itemsId)
    {
        return ItemDataInformation[itemsId];
    }

    public void AddItem(int gridId, ItemSaveData data)
    {
        var list = ItemDataInformation[gridId].list;

        var weaponType = data.data.itemStats.GetWeaponType();

        GameObject saveObject = new GameObject();
        var itemSaveData = saveObject.AddComponent<ItemSaveData>();
        itemSaveData = data;

        switch (gridId)
        {
            case 1:
                if (weaponType == WeaponType.Helmet)
                {
                    // add itemstats onto HelmetItemPrefab so tooltip shows correct Info
                    // save previousItemData as data on the prefab
                    // the itemdata on the prefab should be correct dont change it.
                    // set isSlot type to true on prefab

                }
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            case 7:
                break;
            default: break;
        }
        Destroy(saveObject);

        data.Id = list.Count;
        list.Add(data);
    }

    public void RemoveItem(int gridId, int itemId)
    {
        ItemDataInformation[gridId].list.RemoveAt(itemId);

        for (int i = itemId; i < ItemDataInformation[gridId].list.Count; i++)
            ItemDataInformation[gridId].list[i].Id = i;
    }
}