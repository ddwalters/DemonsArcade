using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Save/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public List<ItemData> definitions = new();
    public InventoryTypeCollection inventoryTypes;

    public ItemData Resolve(string id)
    {
        ItemData definition = definitions.Find(item => item != null && item.SaveId == id);
        if (definition == null)
            throw new InvalidOperationException("This save references an unavailable item: " + id);

        return definition;
    }

    public List<SavedItem> Capture(List<ItemInstance> items)
    {
        var result = new List<SavedItem>();
        foreach (var item in items)
        {
            if (item.Definition == null || string.IsNullOrEmpty(item.Definition.SaveId))
                throw new InvalidOperationException("An item has no save identifier.");

            Resolve(item.Definition.SaveId);
            result.Add(new SavedItem
            {
                definitionId = item.Definition.SaveId,
                position = item.Position,
                rotation = item.Rotation
            });
        }

        return result;
    }

    public void ValidateContainer(SavedContainer container, InventoryType type)
    {
        var items = Restore(container.items);
        if (InventoryManager.IsEquipmentGrid(container.slot))
        {
            if (items.Count > 1 || items.Exists(item => !InventoryManager.CanEquip(item.Definition, (EquipmentSlot)container.slot)))
                throw new InvalidOperationException("Invalid equipment in the save.");

            return;
        }

        var grids = inventoryTypes[type].GetComponentsInChildren<InventoryGrid>(true);
        Vector2Int size = Array.Find(grids, grid => grid.id == 0).gridSize;
        var occupied = new HashSet<Vector2Int>();
        foreach (var item in items)
        {
            Vector2Int footprint = InventoryPlacement.Size(item.Definition, item.Rotation);
            if (!InventoryPlacement.Fits(item.Position, footprint, size))
                throw new InvalidOperationException("An item is outside its saved inventory.");

            foreach (var cell in InventoryPlacement.Cells(item.Position, footprint))
                if (!occupied.Add(cell))
                    throw new InvalidOperationException("Saved items overlap.");
        }
    }

    public List<ItemInstance> Restore(List<SavedItem> items)
    {
        var result = new List<ItemInstance>();
        foreach (var item in items)
            result.Add(new ItemInstance(Resolve(item.definitionId), item.position, item.rotation));

        return result;
    }
}
