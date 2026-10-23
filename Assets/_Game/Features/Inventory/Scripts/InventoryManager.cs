using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    public const int FirstLootGridId = (int)EquipmentSlot.RingRight + 1;

    private Dictionary<int, (List<ItemInstance> list, InventoryType type)> ItemDataInformation;
    private int nextGridId = FirstLootGridId;
    private EquipmentTotals currentEquipment;
    private bool playerInventoryTypeBound;

    public event Action<EquipmentTotals> EquipmentChanged;
    public EquipmentTotals CurrentEquipment
    {
        get
        {
            EnsureInitialized();
            return currentEquipment;
        }
    }

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

        PlayerStatsManager playerStats = FindAnyObjectByType<PlayerStatsManager>();
        if (playerStats != null)
        {
            Initialize(playerStats.GetPlayerStats().inventoryType);
            playerInventoryTypeBound = true;
        }
        else
        {
            // The inventory can be created before the player during scene startup.
            Initialize(InventoryType.FourByFourFull);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() => RebuildEquipment();

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RebuildEquipment();

    public void Initialize(InventoryType playerType)
    {
        if (ItemDataInformation != null)
            return;

        ItemDataInformation = new Dictionary<int, (List<ItemInstance>, InventoryType)>();
        ItemDataInformation.Add(0, (new List<ItemInstance>(), playerType));
        for (int id = (int)EquipmentSlot.Helmet; id <= (int)EquipmentSlot.RingRight; id++)
            ItemDataInformation.Add(id, (new List<ItemInstance>(), InventoryType.OneByOneMinimal));

        currentEquipment = EquipmentCalculator.Calculate(Array.Empty<ItemData>());
    }

    private void EnsureInitialized()
    {
        if (ItemDataInformation == null)
            Initialize(InventoryType.FourByFourFull);
    }

    private void BindPlayerInventoryTypeIfAvailable()
    {
        if (playerInventoryTypeBound)
            return;

        PlayerStatsManager playerStats = FindAnyObjectByType<PlayerStatsManager>();
        if (playerStats == null)
            return;

        SetPlayerInventoryType(playerStats.GetPlayerStats().inventoryType);
        playerInventoryTypeBound = true;
    }

    public static bool IsEquipmentGrid(int id) =>
        id >= (int)EquipmentSlot.Helmet && id <= (int)EquipmentSlot.RingRight;

    public static bool CanEquip(ItemData definition, EquipmentSlot slot)
    {
        if (definition == null || definition.itemStats == null)
            return false;

        WeaponType weapon = definition.itemStats.GetWeaponType();
        if (slot == EquipmentSlot.RightHand)
            return weapon == WeaponType.ShortSword || weapon == WeaponType.Axe || weapon == WeaponType.Staff;

        if (slot == EquipmentSlot.LeftHand)
            return weapon == WeaponType.Shield;

        if (slot == EquipmentSlot.RingLeft || slot == EquipmentSlot.RingRight)
            return weapon == WeaponType.NonWeapon && definition.itemStats.GetArmorType() == ArmorType.Ring;

        return weapon == WeaponType.NonWeapon && definition.itemStats.GetArmorType() == (ArmorType)((int)slot - 1);
    }

    public Sprite GetSlotIcon(int id, ItemData definition)
    {
        if (id == (int)EquipmentSlot.RingLeft || id == (int)EquipmentSlot.RingRight)
            return definition != null ? definition.icon : null;

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
        EnsureInitialized();
        int id = nextGridId++;
        ItemDataInformation.Add(id, (new List<ItemInstance>(), type));
        return id;
    }

    public (List<ItemInstance> list, InventoryType type) GetItems(int id)
    {
        EnsureInitialized();
        return ItemDataInformation[id];
    }

    public void SetPlayerInventoryType(InventoryType type)
    {
        EnsureInitialized();
        ItemDataInformation[0] = (GetItems(0).list, type);
        playerInventoryTypeBound = true;
    }

    public void ReplaceItems(int id, List<ItemInstance> items)
    {
        EnsureInitialized();
        var list = GetItems(id).list;
        list.Clear();
        list.AddRange(items);

        if (IsEquipmentGrid(id))
            RefreshEquipment(id, id);
    }

    public void RebuildEquipment()
    {
        EnsureInitialized();
        BindPlayerInventoryTypeIfAvailable();
        RecomputeEquipment();
        RefreshRightHandWeapon();
    }

    public void RemoveGrid(int id)
    {
        EnsureInitialized();
        if (!IsEquipmentGrid(id) && id != 0)
            ItemDataInformation.Remove(id);
    }

    public IEnumerable<ItemData> EnumerateEquippedItems()
    {
        EnsureInitialized();
        for (int id = (int)EquipmentSlot.Helmet; id <= (int)EquipmentSlot.RingRight; id++)
        {
            foreach (ItemInstance item in ItemDataInformation[id].list)
            {
                if (item != null && item.Definition != null)
                    yield return item.Definition;
            }
        }
    }

    public bool CanPlace(int gridId, ItemData definition, Vector2Int position, int rotation,
        Vector2Int gridSize, ItemInstance ignore = null, ItemInstance otherIgnore = null)
    {
        EnsureInitialized();
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
        EnsureInitialized();
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
        EnsureInitialized();
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
        EnsureInitialized();
        if (item == null)
            return false;

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
        EnsureInitialized();
        if (GetItems(gridId).list.Remove(item))
            RefreshEquipment(gridId, gridId);
    }

    private void RefreshEquipment(int source, int destination)
    {
        if (!IsEquipmentGrid(source) && !IsEquipmentGrid(destination))
            return;

        RecomputeEquipment();

        if (source == (int)EquipmentSlot.RightHand || destination == (int)EquipmentSlot.RightHand)
            RefreshRightHandWeapon();
    }

    private void RecomputeEquipment()
    {
        ItemData[] equippedItems = EnumerateEquippedItems().ToArray();
        currentEquipment = EquipmentCalculator.Calculate(equippedItems);

        PlayerEquipment playerEquipment = FindPlayerEquipment();
        if (playerEquipment != null)
            playerEquipment.SetEquipment(currentEquipment, equippedItems);

        EquipmentChanged?.Invoke(currentEquipment);
    }

    private PlayerEquipment FindPlayerEquipment()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
            return null;

        PlayerEquipment playerEquipment = player.GetComponent<PlayerEquipment>();
        if (playerEquipment == null)
            playerEquipment = player.gameObject.AddComponent<PlayerEquipment>();

        if (player.GetComponent<EquipmentVisualController>() == null)
            player.gameObject.AddComponent<EquipmentVisualController>();

        return playerEquipment;
    }

    private void RefreshRightHandWeapon()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        WeaponsHandler weaponHandler = player != null ? player.GetComponent<WeaponsHandler>() : null;
        if (weaponHandler == null)
            return;

        weaponHandler.RemoveItemFromPlayerRightHand();

        var list = GetItems((int)EquipmentSlot.RightHand).list;
        if (list.Count > 0 && list[0].Definition != null && list[0].Definition.itemStats != null)
            weaponHandler.AddItemToPlayerRightHand(list[0].Definition.itemStats);
    }
}
