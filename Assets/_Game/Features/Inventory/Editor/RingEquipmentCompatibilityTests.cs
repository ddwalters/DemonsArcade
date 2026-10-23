using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RingEquipmentCompatibilityTests
{
    private ItemCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = Resources.Load<ItemCatalog>("ItemCatalog");
        Assert.IsNotNull(catalog);
    }

    [Test]
    public void RingItemsFitBothRingSlots()
    {
        ItemData copperRing = FindItem("Copper Ring");
        ItemData moonstoneRing = FindItem("Moonstone Ring");

        Assert.IsTrue(InventoryManager.CanEquip(copperRing, EquipmentSlot.RingLeft));
        Assert.IsTrue(InventoryManager.CanEquip(copperRing, EquipmentSlot.RingRight));
        Assert.IsTrue(InventoryManager.CanEquip(moonstoneRing, EquipmentSlot.RingLeft));
        Assert.IsTrue(InventoryManager.CanEquip(moonstoneRing, EquipmentSlot.RingRight));
    }

    [Test]
    public void RingSlotsRejectWeaponsAndOtherArmorTypes()
    {
        Assert.IsFalse(InventoryManager.CanEquip(FindItem("Short Sword"), EquipmentSlot.RingLeft));
        Assert.IsFalse(InventoryManager.CanEquip(FindItem("Winter Hat"), EquipmentSlot.RingRight));
        Assert.IsFalse(InventoryManager.CanEquip(FindItem("Copper Ring"), EquipmentSlot.Necklace));
    }

    [Test]
    public void ExistingWeaponSlotsStillAcceptTheirMatchingWeapons()
    {
        Assert.IsTrue(InventoryManager.CanEquip(FindItem("Short Sword"), EquipmentSlot.RightHand));
        Assert.IsTrue(InventoryManager.CanEquip(FindItem("Shield"), EquipmentSlot.LeftHand));
    }

    [Test]
    public void LegacyAndCurrentSaveLayoutsDoNotOverlapLootGridIds()
    {
        var legacyInventory = new List<SavedContainer>();
        for (int slot = 0; slot <= 7; slot++)
            legacyInventory.Add(new SavedContainer { slot = slot });

        var currentInventory = new List<SavedContainer>();
        for (int slot = 0; slot <= 9; slot++)
            currentInventory.Add(new SavedContainer { slot = slot });

        Assert.IsTrue(SaveManager.HasValidInventoryLayout(legacyInventory));
        Assert.IsTrue(SaveManager.HasValidInventoryLayout(currentInventory));
        Assert.IsFalse(SaveManager.HasValidInventoryLayout(legacyInventory.GetRange(0, 7)));
        Assert.IsFalse(SaveManager.HasValidInventoryLayout(currentInventory.GetRange(0, 9)));
        Assert.AreEqual(10, InventoryManager.FirstLootGridId);
        Assert.IsFalse(InventoryManager.IsEquipmentGrid(InventoryManager.FirstLootGridId));
    }

    private ItemData FindItem(string itemName)
    {
        ItemData item = catalog.definitions.Find(definition =>
            definition != null && definition.itemStats != null && definition.itemStats.GetItemName() == itemName);
        Assert.IsNotNull(item, "Missing catalog item: " + itemName);
        return item;
    }
}
