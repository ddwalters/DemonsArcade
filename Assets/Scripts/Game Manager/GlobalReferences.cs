using UnityEditor.PackageManager;
using UnityEngine;

public static class GlobalReferences
{
    public static GridManager GridManager { get; private set; }
    public static Grid PlayerInventoryGrid { get; private set; }
    public static Grid PlayerEquipmentGrid { get; private set; }

    public static void Initialize()
    {
        var inventoryManager = GameObject.Find("InventoryManager");
        var gridManager = inventoryManager.GetComponent<GridManager>();
        GridManager = gridManager;

        var playerGrids = inventoryManager.GetComponentsInChildren<Grid>();
        PlayerInventoryGrid = playerGrids[0];
        PlayerEquipmentGrid = playerGrids[1];
    }
}