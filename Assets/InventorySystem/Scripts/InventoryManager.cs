using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemSaveData> list, InventoryType type)> ItemDataInformation;
    WeaponsHandler weaponHandler;

    [SerializeField] ItemData fakeHelmetData;
    [SerializeField] ItemData fakeChestpieceData;
    [SerializeField] ItemData fakeLeggingsData;
    [SerializeField] ItemData fakeBootsData;
    [SerializeField] ItemData fakeNecklaceData;
    [SerializeField] ItemData fakeWeaponData;

    private void Awake()
    {
        DontDestroyOnLoad(this);

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

    private void OnLevelWasLoaded(int level)
    {
        weaponHandler = FindAnyObjectByType<WeaponsHandler>();
    }

    private void Start()
    {
        weaponHandler = FindAnyObjectByType<WeaponsHandler>();
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

    public bool AddItem(int gridId, ItemSaveData data, bool isSlot)
    {
        var list = ItemDataInformation[gridId].list;

        if (isSlot)
        {
            ItemSaveData itemSaveData;
            switch (gridId)
            {
                case 1:
                    itemSaveData = CreateSlotSaveData(WeaponType.Helmet, fakeHelmetData, data);
                    break;
                case 2:
                    itemSaveData = CreateSlotSaveData(WeaponType.Chestpiece, fakeChestpieceData, data);
                    break;
                case 3:
                    itemSaveData = CreateSlotSaveData(WeaponType.Leggings, fakeLeggingsData, data);
                    break;
                case 4:
                    itemSaveData = CreateSlotSaveData(WeaponType.Boots, fakeBootsData, data);
                    break;
                case 5:
                    itemSaveData = CreateSlotSaveData(WeaponType.Necklace, fakeNecklaceData, data);
                    break;
                case 6:
                    itemSaveData = CreateSlotSaveData(fakeWeaponData, data);
                    if (itemSaveData != null)
                        weaponHandler.AddItemToPlayerLeftHand(data.data.itemStats);
                    break;
                case 7:
                    itemSaveData = CreateSlotSaveData(fakeWeaponData, data);
                    if (itemSaveData != null)
                        weaponHandler.AddItemToPlayerRightHand(data.data.itemStats);
                    break;
                default:
                    return false;
            }

            if (itemSaveData == null)
                return false;

            itemSaveData.Id = list.Count;
            list.Add(itemSaveData);
            return true;
        }

        data.Id = list.Count;
        list.Add(data);
        return true;
    }

    #region Slot Save Data Creation
    private ItemSaveData CreateSlotSaveData(WeaponType typeToCheck, ItemData slotData, ItemSaveData data)
    {
        if (data.isSlotType == true)
        {
            Debug.Log("Item already in a slot");
            return null;
        }

        GameObject saveObject = new GameObject();
        var itemSaveData = saveObject.AddComponent<ItemSaveData>();

        WeaponType weaponType;
        if (data.PreviousItemData != null)
            weaponType = data.PreviousItemData.itemStats.GetWeaponType();
        else
            weaponType = data.data.itemStats.GetWeaponType();

        if (weaponType == typeToCheck)
        {
            itemSaveData.data = slotData;
            itemSaveData.isSlotType = true;
            itemSaveData.PreviousItemData = data.data;
        }
        else
        {
            itemSaveData = null;
        }

        return itemSaveData;
    }

    private ItemSaveData CreateSlotSaveData(ItemData slotData, ItemSaveData data)
    {
        GameObject saveObject = new GameObject();
        var itemSaveData = saveObject.AddComponent<ItemSaveData>();

        WeaponType weaponType;
        if (data.PreviousItemData != null)
            weaponType = data.PreviousItemData.itemStats.GetWeaponType();
        else
            weaponType = data.data.itemStats.GetWeaponType();

        // needs to check if a two handed weapon is taking the other hand slot.
        if (weaponType == WeaponType.Axe || weaponType == WeaponType.Shield || weaponType == WeaponType.ShortSword || weaponType == WeaponType.Staff)
        {
            itemSaveData.data = slotData;
            itemSaveData.isSlotType = true;
            itemSaveData.PreviousItemData = data.data;
        }
        else
        {
            itemSaveData = null;
        }

        return itemSaveData;
    }
    #endregion

    public void RemoveItem(int gridId, int itemId)
    {
        ItemDataInformation[gridId].list.RemoveAt(itemId);

        for (int i = itemId; i < ItemDataInformation[gridId].list.Count; i++)
            ItemDataInformation[gridId].list[i].Id = i;
    }
}