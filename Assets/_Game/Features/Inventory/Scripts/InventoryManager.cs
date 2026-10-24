using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PartyManager))]
public class InventoryManager : MonoBehaviour
{
    public const int FirstLootGridId = (int)EquipmentSlot.RingRight + 1;

    private Dictionary<int, (List<ItemInstance> list, InventoryType type)> ItemDataInformation;
    public PartyManager Party { get; private set; }
    private int nextGridId = FirstLootGridId;
    private EquipmentTotals currentEquipment;

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
        Party = GetComponent<PartyManager>();

        Initialize(Party.Get(PartyManager.PlayerId).Stats.inventoryType);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() => RebuildEquipment();

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            ItemDataInformation.Clear();
            nextGridId = FirstLootGridId;
        }
        RebuildEquipment();
    }

    public void Initialize(InventoryType playerType)
    {
        if (ItemDataInformation != null)
            return;

        ItemDataInformation = new Dictionary<int, (List<ItemInstance>, InventoryType)>();
        Party.Get(PartyManager.PlayerId).Stats.inventoryType = playerType;
        currentEquipment = EquipmentCalculator.Calculate(Array.Empty<ItemData>());
    }

    private void EnsureInitialized()
    {
        if (ItemDataInformation == null)
            Initialize(InventoryType.CharacterBagFull);
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

    public (List<ItemInstance> list, InventoryType type) GetItems(int id, string ownerId = null)
    {
        EnsureInitialized();
        if (id >= 0 && id < FirstLootGridId)
        {
            var member = Party.Get(ownerId ?? PartyManager.PlayerId) ?? throw new ArgumentException("Unknown inventory owner.");
            return (member.GetItems(id), id == 0 ? member.Stats.inventoryType : InventoryType.OneByOneMinimal);
        }
        return ItemDataInformation[id];
    }

    public void SetPlayerInventoryType(InventoryType type)
    {
        EnsureInitialized();
        Party.Get(PartyManager.PlayerId).Stats.inventoryType = type;
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
        RecomputeEquipment();
        RefreshRightHandWeapon();
    }

    public void RemoveGrid(int id)
    {
        EnsureInitialized();
        if (!IsEquipmentGrid(id) && id != 0)
            ItemDataInformation.Remove(id);
    }

    public IEnumerable<ItemData> EnumerateEquippedItems(string ownerId = null)
    {
        EnsureInitialized();
        for (int id = (int)EquipmentSlot.Helmet; id <= (int)EquipmentSlot.RingRight; id++)
        {
            foreach (ItemInstance item in GetItems(id, ownerId).list)
            {
                if (item != null && item.Definition != null)
                    yield return item.Definition;
            }
        }
    }

    public bool CanPlace(int gridId, ItemData definition, Vector2Int position, int rotation,
        Vector2Int gridSize, ItemInstance ignore = null, ItemInstance otherIgnore = null, string ownerId = null)
    {
        EnsureInitialized();
        if (definition == null || (gridId >= FirstLootGridId && !ItemDataInformation.ContainsKey(gridId)))
            return false;

        if (IsEquipmentGrid(gridId))
            return CanEquip(definition, (EquipmentSlot)gridId) && GetItems(gridId, ownerId).list.TrueForAll(item => ReferenceEquals(item, ignore) || ReferenceEquals(item, otherIgnore));

        var grid = GetItems(gridId, ownerId);
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

    private void Transfer(int sourceId, ItemInstance item, int destinationId, Vector2Int position, int rotation, string ownerId)
    {
        GetItems(sourceId, ownerId).list.Remove(item);
        GetItems(destinationId, ownerId).list.Add(item);
        Configure(item, destinationId, position, rotation);
    }

    // Validation happens before either container or the item record changes.
    public bool TryMoveItem(int sourceId, ItemInstance item, int destinationId,
        Vector2Int position, int rotation, Vector2Int destinationSize, string ownerId = null)
    {
        EnsureInitialized();
        var source = GetItems(sourceId, ownerId).list;
        if (!source.Contains(item) || !CanPlace(destinationId, item.Definition, position, rotation, destinationSize, item, null, ownerId))
            return false;

        Transfer(sourceId, item, destinationId, position, rotation, ownerId);
        RefreshEquipment(sourceId, destinationId, ownerId);
        return true;
    }

    public bool TrySwapItems(int sourceId, ItemInstance item, int destinationId, ItemInstance other,
        Vector2Int position, int rotation, Vector2Int sourceSize, Vector2Int destinationSize, string ownerId = null)
    {
        EnsureInitialized();
        var source = GetItems(sourceId, ownerId).list;
        var destination = GetItems(destinationId, ownerId).list;
        if (ReferenceEquals(item, other) || !source.Contains(item) || !destination.Contains(other))
            return false;

        Vector2Int oldPosition = item.Position;
        int otherRotation = other.Rotation;
        if (!CanPlace(destinationId, item.Definition, position, rotation, destinationSize, item, other, ownerId) ||
            !CanPlace(sourceId, other.Definition, oldPosition, otherRotation, sourceSize, item, other, ownerId))
            return false;

        if (sourceId == destinationId && !IsEquipmentGrid(sourceId) &&
            InventoryPlacement.Overlaps(position, InventoryPlacement.Size(item.Definition, rotation),
                oldPosition, InventoryPlacement.Size(other.Definition, otherRotation)))
            return false;

        Transfer(sourceId, item, destinationId, position, rotation, ownerId);
        Transfer(destinationId, other, sourceId, oldPosition, otherRotation, ownerId);
        RefreshEquipment(sourceId, destinationId, ownerId);
        return true;
    }

    public bool TryAddItem(int gridId, ItemInstance item, Vector2Int gridSize, string ownerId = null)
    {
        EnsureInitialized();
        if (item == null)
            return false;

        if (!CanPlace(gridId, item.Definition, item.Position, item.Rotation, gridSize, null, null, ownerId))
            return false;

        Configure(item, gridId, item.Position, item.Rotation);
        var list = GetItems(gridId, ownerId).list;
        list.Add(item);
        RefreshEquipment(gridId, gridId, ownerId);
        return true;
    }

    public void RemoveItem(int gridId, ItemInstance item, string ownerId = null)
    {
        EnsureInitialized();
        if (GetItems(gridId, ownerId).list.Remove(item))
            RefreshEquipment(gridId, gridId, ownerId);
    }

    private void RefreshEquipment(int source, int destination, string ownerId = null)
    {
        if (!IsEquipmentGrid(source) && !IsEquipmentGrid(destination))
            return;

        RebuildMemberEquipment(ownerId);

        if (ownerId != null && ownerId != PartyManager.PlayerId) return;
        if (source == (int)EquipmentSlot.RightHand || destination == (int)EquipmentSlot.RightHand)
            RefreshRightHandWeapon();
    }

    public void RebuildMemberEquipment(string ownerId)
    {
        if (ownerId == null || ownerId == PartyManager.PlayerId) { RecomputeEquipment(); return; }
        var member = Party.Get(ownerId);
        if (member == null) return;
        var items = EnumerateEquippedItems(ownerId).ToArray();
        member.Equipment = EquipmentCalculator.Calculate(items);
        if (member.Actor != null)
        {
            var equipment = member.Actor.GetComponent<PlayerEquipment>();
            equipment.SetEquipment(member.Equipment, items);
            var weapons = member.Actor.GetComponent<WeaponsHandler>();
            if (weapons != null)
            {
                weapons.RemoveItemFromPlayerRightHand();
                var hand = member.GetItems((int)EquipmentSlot.RightHand);
                if (hand.Count > 0) weapons.AddItemToPlayerRightHand(hand[0].Definition.itemStats);
            }
        }
    }

    private void RecomputeEquipment()
    {
        ItemData[] equippedItems = EnumerateEquippedItems().ToArray();
        currentEquipment = EquipmentCalculator.Calculate(equippedItems);

        PlayerEquipment playerEquipment = FindPlayerEquipment();
        if (playerEquipment != null)
            playerEquipment.SetEquipment(currentEquipment, equippedItems);

        if (Party != null) Party.Get(PartyManager.PlayerId).Equipment = currentEquipment;
        EquipmentChanged?.Invoke(currentEquipment);
    }

    private PlayerEquipment FindPlayerEquipment()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null)
            return null;

        PlayerEquipment playerEquipment = player.GetComponent<PlayerEquipment>();
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
