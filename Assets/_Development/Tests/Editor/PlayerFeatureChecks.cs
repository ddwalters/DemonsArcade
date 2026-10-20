using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Edit-mode regression checks for the gameplay boundaries; no scene or save-file changes.
public static class PlayerFeatureChecks
{
    private static int checks;
    private static readonly List<Object> temporaryAssets = new();

    [MenuItem("Tools/Validation/Player Features")]
    public static void RunAll()
    {
        checks = 0;
        var fixture = new GameObject("Player feature checks (temporary)");
        fixture.SetActive(false);
        float oldTimeScale = Time.timeScale;
        CursorLockMode oldLock = Cursor.lockState;
        bool oldCursor = Cursor.visible;
        try
        {
            CheckInventory(fixture);
            CheckStats(fixture);
            CheckInteraction(fixture);
            CheckModes(fixture);
            Debug.Log($"[Player Features] PASS: {checks} regression checks.");
        }
        finally
        {
            Object.DestroyImmediate(fixture);
            foreach (Object asset in temporaryAssets)
                Object.DestroyImmediate(asset);
            temporaryAssets.Clear();
            Time.timeScale = oldTimeScale;
            Cursor.lockState = oldLock;
            Cursor.visible = oldCursor;
        }
    }

    private static void CheckInventory(GameObject fixture)
    {
        var manager = fixture.AddComponent<InventoryManager>();
        manager.Initialize(InventoryType.FourByFourFull);
        ItemData sword = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Features/Inventory/Data/Items/ShortSword.asset");
        ItemData shield = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Features/Inventory/Data/Items/Shield.asset");
        ItemData apple = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/_Game/Features/Inventory/Data/Items/Apple.asset");
        var slot = ScriptableObject.CreateInstance<ItemData>();
        slot.size = new SizeInt(1, 1);
        temporaryAssets.Add(slot);
        foreach (string name in new[] { "fakeHelmetData", "fakeChestpieceData", "fakeLeggingsData", "fakeBootsData", "fakeNecklaceData", "fakeWeaponData" })
            Set(manager, name, slot);
        Require(InventoryManager.CanEquip(sword, EquipmentSlot.RightHand), "Sword fits the right hand.");
        Require(!InventoryManager.CanEquip(sword, EquipmentSlot.Helmet), "Sword is not a helmet despite its old armor enum value.");
        Require(!InventoryManager.CanEquip(sword, EquipmentSlot.LeftHand), "Left hand rejects swords.");
        Require(InventoryManager.CanEquip(shield, EquipmentSlot.LeftHand), "Shield fits the left hand.");
        Require(!InventoryManager.CanEquip(shield, EquipmentSlot.RightHand), "Right hand rejects shields.");
        Require(!InventoryManager.CanEquip(apple, EquipmentSlot.Helmet), "Ordinary loot cannot become armor.");
        Require(InventoryPlacement.Cells(new Vector2Int(2, 0), InventoryPlacement.Size(sword, 1))
            .SequenceEqual(new[] { new Vector2Int(2, 0), new Vector2Int(3, 0) }), "Rotated cells preserve their origin.");
        Require(!InventoryPlacement.Fits(Vector2Int.zero, new Vector2Int(5, 1), new Vector2Int(4, 4)), "Oversized items fail even in an empty grid.");

        int world = manager.AddNewGridList(InventoryType.SixByThreeMinimal);
        var record = Record(fixture, sword, Vector2Int.zero);
        Require(manager.TryAddItem(0, record, new Vector2Int(4, 4)), "Add initial record.");
        var blocker = Record(fixture, apple, new Vector2Int(4, 0));
        Require(manager.TryAddItem(world, blocker, new Vector2Int(6, 3)), "Add destination blocker.");
        Require(!manager.TryMoveItem(0, record, world, new Vector2Int(4, 0), 1, new Vector2Int(6, 3)), "Blocked transfer fails.");
        Require(manager.GetItems(0).list.Contains(record) && record.slotPosition == Vector2Int.zero && record.rotateIndex == 0,
            "Failed transfer preserves its source record and rotation.");
        Require(manager.TryMoveItem(0, record, world, new Vector2Int(2, 0), 1, new Vector2Int(6, 3)), "Transfer to the requested cells.");
        Require(ReferenceEquals(manager.GetItems(world).list[1], record) && record.slotPosition == new Vector2Int(2, 0) && record.rotateIndex == 1,
            "Transfer preserves identity, exact coordinates, and rotation.");
        Require(!manager.TryMoveItem(world, record, (int)EquipmentSlot.Helmet, Vector2Int.zero, 0, Vector2Int.one), "Invalid equipment drop fails.");
        Require(record.data == sword && !record.isSlotType && manager.GetItems(world).list.Contains(record), "Failed equip does not convert/remove the item.");
        Require(manager.TryMoveItem(world, record, (int)EquipmentSlot.RightHand, Vector2Int.zero, 0, Vector2Int.one), "Equip right hand.");
        Require(InventoryManager.Definition(record) == sword && record.isSlotType, "Equipped representation retains its original definition.");
        Require(manager.TryMoveItem((int)EquipmentSlot.RightHand, record, world, new Vector2Int(2, 0), 1, new Vector2Int(6, 3)), "Unequip at requested rotation.");
        Require(manager.GetItems((int)EquipmentSlot.RightHand).list.Count == 0 && record.data == sword && record.PreviousItemData == null,
            "Unequipping clears the correct slot and restores the real definition.");

        var other = Record(fixture, apple, Vector2Int.zero);
        Require(manager.TryAddItem(0, other, new Vector2Int(4, 4)), "Add swap partner.");
        Require(!manager.TrySwapItems(world, record, 0, other, new Vector2Int(3, 3), 1,
            new Vector2Int(6, 3), new Vector2Int(4, 4)), "Invalid swap fails atomically.");
        Require(manager.GetItems(world).list.Contains(record) && manager.GetItems(0).list.Contains(other), "Failed swap preserves both owners.");
        Require(manager.TrySwapItems(world, record, 0, other, Vector2Int.zero, 1,
            new Vector2Int(6, 3), new Vector2Int(4, 4)), "Valid cross-container swap.");
        Require(record.slotPosition == Vector2Int.zero && other.slotPosition == new Vector2Int(2, 0), "Swap commits both requested positions.");
        Require(manager.TryMoveItem(0, record, world, new Vector2Int(2, 1), 1, new Vector2Int(6, 3)), "Move back for view reconstruction.");

        var canvas = new GameObject("Fixture canvas", typeof(RectTransform), typeof(Canvas));
        canvas.transform.SetParent(fixture.transform);
        var inventory = canvas.AddComponent<Inventory>();
        inventory.inventoryType = AssetDatabase.LoadAssetAtPath<InventoryTypeCollection>("Assets/_Game/Features/Inventory/Data/InvTypeCollection.asset");
        inventory.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Features/Inventory/Prefabs/ItemPrefab.prefab").GetComponent<Item>();
        Set(inventory, "inventoryManager", manager);
        Set(inventory, "canvas", canvas.GetComponent<Canvas>());
        Set(inventory, "playerGridHolder", canvas.transform);
        Set(inventory, "worldGridHolder", canvas.transform);
        inventory.CreateGrid(world, false);
        var grid = canvas.GetComponentsInChildren<InventoryGrid>(true).First(g => g.id == world);
        Require(grid.items[2, 1] != null && grid.items[2, 1] == grid.items[3, 1], "Reopened view occupies the same rotated cells.");
        var held = grid.items[2, 1];
        inventory.SelectItem(held);
        held.Rotate();
        Require(record.rotateIndex == 1, "Drag rotation remains a preview.");
        inventory.CloseAllGrids();
        Require(inventory.selectedItem == null && record.slotPosition == new Vector2Int(2, 1) && record.rotateIndex == 1,
            "Closing a drag cancels it without changing inventory data.");
        inventory.CreateGrid(world, false);
        grid = canvas.GetComponentsInChildren<InventoryGrid>(true).First(g => g.id == world);
        Require(grid.items[2, 1] != null && grid.items[3, 1] != null, "Cancelled item reconstructs correctly on reopen.");
        inventory.CloseAllGrids();
    }

    private static void CheckStats(GameObject fixture)
    {
        var stats = fixture.AddComponent<PlayerStatsManager>();
        var data = new PlayerStatsData { currentHealth = 37f, currentStamina = 22f, currentMana = 13f, currentXP = 123 };
        Set(stats, "statsData", data);

        Image Bar()
        {
            var child = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(fixture.transform);
            return child.GetComponent<Image>();
        }

        Image health = Bar(), stamina = Bar(), mana = Bar();
        stats.BindHUD(health, stamina, mana, null);
        stats.BindHUD(health, stamina, mana, null);
        Require(ReferenceEquals(stats.GetPlayerStats(), data) && data.currentXP == 123, "HUD binding/getter preserve the live session object and XP.");
        Require(data.currentHealth == 37f && data.currentStamina == 22f && data.currentMana == 13f, "HUD binding does not refill resources.");
        Require(Mathf.Approximately(health.fillAmount, .37f) && Mathf.Approximately(stamina.fillAmount, .22f) && Mathf.Approximately(mana.fillAmount, .13f), "Rebound bars reflect current values.");
        stats.ResetResources();
        Require(data.currentHealth == data.maxHealth && data.currentStamina == data.maxStamina && data.currentMana == data.maxMana, "Explicit resource reset still works.");
    }

    private static void CheckInteraction(GameObject fixture)
    {
        var interaction = fixture.AddComponent<InteractionController>();
        var a = new GameObject("Target A");
        a.transform.SetParent(fixture.transform);
        var b = new GameObject("Target B");
        b.transform.SetParent(fixture.transform);
        var first = a.AddComponent<FeatureCheckInteractable>();
        var second = b.AddComponent<FeatureCheckInteractable>();
        foreach (var target in new[] { first, second })
        {
            Set(target, "isInteractable", true, typeof(InteractableBase));
            Set(target, "holdInteract", true, typeof(InteractableBase));
            Set(target, "holdDuration", 1f, typeof(InteractableBase));
        }
        Call(interaction, "SetTarget", first);
        Call(interaction, "ProcessInput", true, false, .6f);
        Call(interaction, "SetTarget", second);
        Call(interaction, "ProcessInput", false, false, 1f);
        Require(second.Count == 0, "Switching targets cancels hold and requires a fresh press.");
        Call(interaction, "SetTarget", (object)null);
        Call(interaction, "ProcessInput", false, true, .1f);
        Call(interaction, "SetTarget", first);
        Call(interaction, "ProcessInput", false, false, 1f);
        Require(first.Count == 0, "Release while no target cannot leave interaction armed.");
        Call(interaction, "ProcessInput", true, false, 1f);
        Require(first.Count == 1, "A complete fresh hold interacts once.");
        Call(interaction, "ProcessInput", false, false, 1f);
        Require(first.Count == 1, "Completed hold does not repeat.");
        Call(interaction, "SetTarget", second);
        Call(interaction, "ProcessInput", true, false, .5f);
        Set(second, "isInteractable", false, typeof(InteractableBase));
        Call(interaction, "ProcessInput", false, false, .5f);
        Set(second, "isInteractable", true, typeof(InteractableBase));
        Call(interaction, "ProcessInput", false, false, 1f);
        Require(second.Count == 0, "Locking the current target cancels its hold.");
        Set(second, "holdDuration", 0f, typeof(InteractableBase));
        Call(interaction, "ProcessInput", true, false, .01f);
        Require(second.Count == 1, "Zero-duration interactions complete without division by zero.");
    }

    private static void CheckModes(GameObject fixture)
    {
        var mode = fixture.AddComponent<PlayerModeController>();
        int changes = 0;
        mode.ModeChanged += _ => changes++;
        mode.SetMode(PlayerMode.Inventory);
        Call(mode, "HandleMenuInput", true, false, false);
        Require(mode.Mode == PlayerMode.Gameplay && changes == 2, "Escape closes inventory with one transition, without opening pause.");
        Call(mode, "HandleMenuInput", true, false, false);
        Require(mode.Mode == PlayerMode.Paused && Time.timeScale == 0f && !mode.AllowsGameplay, "Escape pauses from gameplay.");
        Call(mode, "HandleMenuInput", true, false, false);
        Require(mode.Mode == PlayerMode.Gameplay && Time.timeScale > 0f, "Escape resumes and restores time scale.");
        mode.OpenLoot(8);
        Require(mode.Mode == PlayerMode.Looting && mode.LootGridId == 8 && !mode.AllowsGameplay && Cursor.visible, "Looting shares the cursor/gameplay policy.");
        Call(mode, "HandleMenuInput", false, false, true);
        Require(mode.Mode == PlayerMode.Gameplay && mode.LootGridId == -1, "E closes loot through the same mode controller.");
        mode.SetMode(PlayerMode.Dead);
        Call(mode, "HandleMenuInput", true, true, true);
        mode.SetMode(PlayerMode.Gameplay);
        Require(mode.Mode == PlayerMode.Dead && !mode.AllowsGameplay, "Menus and ordinary mode requests cannot unlock a dead player.");
        mode.ResetToGameplay();
        Require(mode.AllowsGameplay, "Explicit new-life reset returns to gameplay.");
    }

    private static ItemSaveData Record(GameObject fixture, ItemData data, Vector2Int position)
    {
        var go = new GameObject("Item record");
        go.transform.SetParent(fixture.transform);
        var record = go.AddComponent<ItemSaveData>();
        record.data = data;
        record.slotPosition = position;
        return record;
    }
    private static void Set(object target, string name, object value, Type type = null) =>
        (type ?? target.GetType()).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Require(bool success, string message)
    {
        if (!success)
            throw new InvalidOperationException(message);
        checks++;
    }
}

public class FeatureCheckInteractable : InteractableBase
{
    public int Count { get; private set; }
    public override void OnInteract() => Count++;
}
