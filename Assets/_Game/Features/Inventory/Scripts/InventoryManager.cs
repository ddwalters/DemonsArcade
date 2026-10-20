using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemSaveData> list, InventoryType type)> ItemDataInformation;
    private WeaponsHandler weaponHandler;
    private int nextGridId = 8;

    [SerializeField] private ItemData fakeHelmetData;
    [SerializeField] private ItemData fakeChestpieceData;
    [SerializeField] private ItemData fakeLeggingsData;
    [SerializeField] private ItemData fakeBootsData;
    [SerializeField] private ItemData fakeNecklaceData;
    [SerializeField] private ItemData fakeWeaponData;
    public Sprite[] itemType;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Initialize(FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats().inventoryType);
    }

    private void Start() => weaponHandler = FindAnyObjectByType<WeaponsHandler>();

    public void Initialize(InventoryType playerType)
    {
        if (ItemDataInformation != null)
            return;

        ItemDataInformation = new Dictionary<int, (List<ItemSaveData>, InventoryType)>();
        ItemDataInformation.Add(0, (new List<ItemSaveData>(), playerType));
        for (int id = 1; id <= 7; id++)
            ItemDataInformation.Add(id, (new List<ItemSaveData>(), InventoryType.OneByOneMinimal));
    }

    public static bool IsEquipmentGrid(int id) => id >= 1 && id <= 7;
    public static ItemData Definition(ItemSaveData item) => item.PreviousItemData != null ? item.PreviousItemData : item.data;

    public static bool CanEquip(ItemData definition, EquipmentSlot slot)
    {
        if (definition == null || definition.itemStats == null)
            return false;

        WeaponType weapon = definition.itemStats.GetWeaponType();
        if (slot == EquipmentSlot.RightHand)
            return weapon == WeaponType.ShortSword || weapon == WeaponType.Axe || weapon == WeaponType.Staff;

        if (slot == EquipmentSlot.LeftHand)
            return weapon == WeaponType.Shield;

        // Existing weapon definitions have a default armor enum value; they are never armor.
        return weapon == WeaponType.NonWeapon && definition.itemStats.GetArmorType() == (ArmorType)((int)slot - 1);
    }

    private ItemData SlotTemplate(int id) => id switch
    {
        1 => fakeHelmetData,
        2 => fakeChestpieceData,
        3 => fakeLeggingsData,
        4 => fakeBootsData,
        5 => fakeNecklaceData,
        6 or 7 => fakeWeaponData,
        _ => null
    };

    public Sprite GetSlotIcon(int id, ItemData definition)
    {
        int index = id == (int)EquipmentSlot.LeftHand ? 4 : definition.itemStats.GetWeaponType() switch
        {
            WeaponType.ShortSword => 1,
            WeaponType.Axe => 2,
            WeaponType.Staff => 3,
            _ => 0
        };

        return id >= 6 && itemType != null && index < itemType.Length ? itemType[index] : SlotTemplate(id)?.icon;
    }

    public int AddNewGridList(InventoryType type)
    {
        int id = nextGridId++;
        ItemDataInformation.Add(id, (new List<ItemSaveData>(), type));
        return id;
    }

    public (List<ItemSaveData> list, InventoryType type) GetItems(int id) => ItemDataInformation[id];

    public void RemoveGrid(int id)
    {
        if (!IsEquipmentGrid(id) && id != 0)
            ItemDataInformation.Remove(id);
    }

    public bool CanPlace(int gridId, ItemData definition, Vector2Int position, int rotation,
        Vector2Int gridSize, ItemSaveData ignore = null, ItemSaveData otherIgnore = null)
    {
        if (definition == null || !ItemDataInformation.TryGetValue(gridId, out var grid))
            return false;

        if (IsEquipmentGrid(gridId))
            return SlotTemplate(gridId) != null && CanEquip(definition, (EquipmentSlot)gridId) && grid.list.TrueForAll(item => ReferenceEquals(item, ignore) || ReferenceEquals(item, otherIgnore));

        Vector2Int size = InventoryPlacement.Size(definition, rotation);
        if (!InventoryPlacement.Fits(position, size, gridSize))
            return false;

        foreach (var item in grid.list)
        {
            if (ReferenceEquals(item, ignore) || ReferenceEquals(item, otherIgnore))
                continue;

            if (InventoryPlacement.Overlaps(position, size, item.slotPosition, InventoryPlacement.Size(Definition(item), item.rotateIndex)))
                return false;
        }

        return true;
    }

    private void Configure(ItemSaveData item, int gridId, Vector2Int position, int rotation)
    {
        ItemData definition = Definition(item);
        bool equipped = IsEquipmentGrid(gridId);
        item.data = equipped ? SlotTemplate(gridId) : definition;
        item.PreviousItemData = equipped ? definition : null;
        item.isSlotType = equipped;
        item.slotPosition = equipped ? Vector2Int.zero : position;
        item.rotateIndex = equipped ? 0 : rotation % 4;
        item.isRotated = (item.rotateIndex & 1) != 0;
        item.transform.SetParent(transform);
    }

    // Validation happens before either container or the item record changes.
    public bool TryMoveItem(int sourceId, ItemSaveData item, int destinationId,
        Vector2Int position, int rotation, Vector2Int destinationSize)
    {
        var source = GetItems(sourceId).list;
        if (!source.Contains(item) || !CanPlace(destinationId, Definition(item), position, rotation, destinationSize, item))
            return false;

        var destination = GetItems(destinationId).list;
        source.Remove(item);
        destination.Add(item);
        Configure(item, destinationId, position, rotation);
        Reindex(source);
        Reindex(destination);
        RefreshEquipment(sourceId, destinationId);
        return true;
    }

    public bool TrySwapItems(int sourceId, ItemSaveData item, int destinationId, ItemSaveData other,
        Vector2Int position, int rotation, Vector2Int sourceSize, Vector2Int destinationSize)
    {
        var source = GetItems(sourceId).list;
        var destination = GetItems(destinationId).list;
        if (ReferenceEquals(item, other) || !source.Contains(item) || !destination.Contains(other))
            return false;

        Vector2Int oldPosition = item.slotPosition;
        int otherRotation = other.rotateIndex;
        if (!CanPlace(destinationId, Definition(item), position, rotation, destinationSize, item, other) ||
            !CanPlace(sourceId, Definition(other), oldPosition, otherRotation, sourceSize, item, other))
            return false;

        if (sourceId == destinationId && !IsEquipmentGrid(sourceId) &&
            InventoryPlacement.Overlaps(position, InventoryPlacement.Size(Definition(item), rotation),
                oldPosition, InventoryPlacement.Size(Definition(other), otherRotation)))
            return false;

        source.Remove(item);
        destination.Remove(other);
        destination.Add(item);
        source.Add(other);
        Configure(item, destinationId, position, rotation);
        Configure(other, sourceId, oldPosition, otherRotation);
        Reindex(source);
        Reindex(destination);
        RefreshEquipment(sourceId, destinationId);
        return true;
    }

    public bool TryAddItem(int gridId, ItemSaveData item, Vector2Int gridSize)
    {
        if (!CanPlace(gridId, Definition(item), item.slotPosition, item.rotateIndex, gridSize))
            return false;

        Configure(item, gridId, item.slotPosition, item.rotateIndex);
        var list = GetItems(gridId).list;
        list.Add(item);
        Reindex(list);
        RefreshEquipment(gridId, gridId);
        return true;
    }

    private static void Reindex(List<ItemSaveData> list)
    {
        for (int i = 0; i < list.Count; i++)
            list[i].Id = i;
    }

    public void RemoveItem(int gridId, int itemId)
    {
        var list = GetItems(gridId).list;
        var item = list[itemId];
        list.RemoveAt(itemId);
        Reindex(list);
        RefreshEquipment(gridId, gridId);
        Destroy(item.gameObject);
    }

    private void RefreshEquipment(int source, int destination)
    {
        if (weaponHandler == null)
            return;

        if (source == (int)EquipmentSlot.RightHand || destination == (int)EquipmentSlot.RightHand)
        {
            weaponHandler.RemoveItemFromPlayerRightHand();

            var list = GetItems((int)EquipmentSlot.RightHand).list;
            if (list.Count > 0)
                weaponHandler.AddItemToPlayerRightHand(Definition(list[0]).itemStats);
        }
        if (source == (int)EquipmentSlot.LeftHand || destination == (int)EquipmentSlot.LeftHand)
        {
            weaponHandler.RemoveItemFromPlayerLeftHand();

            var list = GetItems((int)EquipmentSlot.LeftHand).list;
            if (list.Count > 0)
                weaponHandler.AddItemToPlayerLeftHand(Definition(list[0]).itemStats);
        }
    }
}
