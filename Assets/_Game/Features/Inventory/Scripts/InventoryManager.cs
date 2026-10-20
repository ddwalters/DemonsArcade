using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    private Dictionary<int, (List<ItemInstance> list, InventoryType type)> ItemDataInformation;
    private WeaponsHandler weaponHandler;
    private int nextGridId = 8;

    [SerializeField] private Sprite[] armorSlotIcons;
    [SerializeField] private Sprite[] weaponIcons;

    private void Awake()
    {
        var existing = FindObjectsByType<InventoryManager>();
        foreach (var manager in existing)
        {
            if (manager != this && manager.ItemDataInformation != null)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
        Initialize(FindAnyObjectByType<PlayerStatsManager>().GetPlayerStats().inventoryType);
    }

    private void Start() => weaponHandler = FindAnyObjectByType<WeaponsHandler>();

    public void Initialize(InventoryType playerType)
    {
        if (ItemDataInformation != null)
            return;

        ItemDataInformation = new Dictionary<int, (List<ItemInstance>, InventoryType)>();
        ItemDataInformation.Add(0, (new List<ItemInstance>(), playerType));
        for (int id = 1; id <= 7; id++)
            ItemDataInformation.Add(id, (new List<ItemInstance>(), InventoryType.OneByOneMinimal));
    }

    public static bool IsEquipmentGrid(int id) => id >= 1 && id <= 7;

    public static bool CanEquip(ItemData definition, EquipmentSlot slot)
    {
        if (definition == null || definition.itemStats == null)
            return false;

        WeaponType weapon = definition.itemStats.GetWeaponType();
        if (slot == EquipmentSlot.RightHand)
            return weapon == WeaponType.ShortSword || weapon == WeaponType.Axe || weapon == WeaponType.Staff;

        if (slot == EquipmentSlot.LeftHand)
            return weapon == WeaponType.Shield;

        return weapon == WeaponType.NonWeapon && definition.itemStats.GetArmorType() == (ArmorType)((int)slot - 1);
    }

    public Sprite GetSlotIcon(int id, ItemData definition)
    {
        if (id < (int)EquipmentSlot.RightHand)
            return armorSlotIcons != null && id > 0 && id <= armorSlotIcons.Length ? armorSlotIcons[id - 1] : definition.icon;

        int index = id == (int)EquipmentSlot.LeftHand ? 4 : definition.itemStats.GetWeaponType() switch
        {
            WeaponType.ShortSword => 1,
            WeaponType.Axe => 2,
            WeaponType.Staff => 3,
            _ => 0
        };

        return weaponIcons != null && index < weaponIcons.Length ? weaponIcons[index] : definition.icon;
    }

    public int AddNewGridList(InventoryType type)
    {
        int id = nextGridId++;
        ItemDataInformation.Add(id, (new List<ItemInstance>(), type));
        return id;
    }

    public (List<ItemInstance> list, InventoryType type) GetItems(int id) => ItemDataInformation[id];

    public void SetPlayerInventoryType(InventoryType type)
    {
        ItemDataInformation[0] = (GetItems(0).list, type);
    }

    public void ReplaceItems(int id, List<ItemInstance> items)
    {
        var list = GetItems(id).list;
        list.Clear();
        list.AddRange(items);
    }

    public void RebuildEquipment()
    {
        weaponHandler = FindAnyObjectByType<WeaponsHandler>();
        RefreshEquipment((int)EquipmentSlot.RightHand, (int)EquipmentSlot.LeftHand);
    }

    public void RemoveGrid(int id)
    {
        if (!IsEquipmentGrid(id) && id != 0)
            ItemDataInformation.Remove(id);
    }

    public bool CanPlace(int gridId, ItemData definition, Vector2Int position, int rotation,
        Vector2Int gridSize, ItemInstance ignore = null, ItemInstance otherIgnore = null)
    {
        if (definition == null || !ItemDataInformation.TryGetValue(gridId, out var grid))
            return false;

        if (IsEquipmentGrid(gridId))
            return CanEquip(definition, (EquipmentSlot)gridId) && grid.list.TrueForAll(item => ReferenceEquals(item, ignore) || ReferenceEquals(item, otherIgnore));

        Vector2Int size = InventoryPlacement.Size(definition, rotation);
        if (!InventoryPlacement.Fits(position, size, gridSize))
            return false;

        foreach (var item in grid.list)
        {
            if (ReferenceEquals(item, ignore) || ReferenceEquals(item, otherIgnore))
                continue;

            if (InventoryPlacement.Overlaps(position, size, item.Position, InventoryPlacement.Size(item.Definition, item.Rotation)))
                return false;
        }

        return true;
    }

    private static void Configure(ItemInstance item, int gridId, Vector2Int position, int rotation)
    {
        item.Position = IsEquipmentGrid(gridId) ? Vector2Int.zero : position;
        item.Rotation = IsEquipmentGrid(gridId) ? 0 : rotation % 4;
    }

    private void Transfer(int sourceId, ItemInstance item, int destinationId, Vector2Int position, int rotation)
    {
        GetItems(sourceId).list.Remove(item);
        GetItems(destinationId).list.Add(item);
        Configure(item, destinationId, position, rotation);
    }

    // Validation happens before either container or the item record changes.
    public bool TryMoveItem(int sourceId, ItemInstance item, int destinationId,
        Vector2Int position, int rotation, Vector2Int destinationSize)
    {
        var source = GetItems(sourceId).list;
        if (!source.Contains(item) || !CanPlace(destinationId, item.Definition, position, rotation, destinationSize, item))
            return false;

        Transfer(sourceId, item, destinationId, position, rotation);
        RefreshEquipment(sourceId, destinationId);
        return true;
    }

    public bool TrySwapItems(int sourceId, ItemInstance item, int destinationId, ItemInstance other,
        Vector2Int position, int rotation, Vector2Int sourceSize, Vector2Int destinationSize)
    {
        var source = GetItems(sourceId).list;
        var destination = GetItems(destinationId).list;
        if (ReferenceEquals(item, other) || !source.Contains(item) || !destination.Contains(other))
            return false;

        Vector2Int oldPosition = item.Position;
        int otherRotation = other.Rotation;
        if (!CanPlace(destinationId, item.Definition, position, rotation, destinationSize, item, other) ||
            !CanPlace(sourceId, other.Definition, oldPosition, otherRotation, sourceSize, item, other))
            return false;

        if (sourceId == destinationId && !IsEquipmentGrid(sourceId) &&
            InventoryPlacement.Overlaps(position, InventoryPlacement.Size(item.Definition, rotation),
                oldPosition, InventoryPlacement.Size(other.Definition, otherRotation)))
            return false;

        Transfer(sourceId, item, destinationId, position, rotation);
        Transfer(destinationId, other, sourceId, oldPosition, otherRotation);
        RefreshEquipment(sourceId, destinationId);
        return true;
    }

    public bool TryAddItem(int gridId, ItemInstance item, Vector2Int gridSize)
    {
        if (!CanPlace(gridId, item.Definition, item.Position, item.Rotation, gridSize))
            return false;

        Configure(item, gridId, item.Position, item.Rotation);
        var list = GetItems(gridId).list;
        list.Add(item);
        RefreshEquipment(gridId, gridId);
        return true;
    }

    public void RemoveItem(int gridId, ItemInstance item)
    {
        if (GetItems(gridId).list.Remove(item))
            RefreshEquipment(gridId, gridId);
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
                weaponHandler.AddItemToPlayerRightHand(list[0].Definition.itemStats);
        }
        if (source == (int)EquipmentSlot.LeftHand || destination == (int)EquipmentSlot.LeftHand)
        {
            weaponHandler.RemoveItemFromPlayerLeftHand();

            var list = GetItems((int)EquipmentSlot.LeftHand).list;
            if (list.Count > 0)
                weaponHandler.AddItemToPlayerLeftHand(list[0].Definition.itemStats);
        }
    }
}
