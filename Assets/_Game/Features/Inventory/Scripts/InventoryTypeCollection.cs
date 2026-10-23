using UnityEngine;
using System;
using System.Collections.Generic;

public enum InventoryType
{
    // minimal: Doesn't include player layout
    // full: Includes full player layout
    OneByOneMinimal,
    EightColumnFull,
    SixByThreeMinimal
}

[CreateAssetMenu(fileName = "InvTypeCollection", menuName = "Inventory/InvTypeCollection")]
public class InventoryTypeCollection : ScriptableObject
{
    [Serializable]
    public class InventoryPrefab
    {
        public GameObject prefab;
        public InventoryType invType;
    }

    [Header("Character Bag")]
    [SerializeField, Min(1), Tooltip("Rows in each eight-column character bag. Occupied rows are retained when reducing this value until their items are moved.")]
    private int bagRows = 4;
    public int BagRows => Mathf.Max(1, bagRows);

    // One source of dimensions for views, placement, pickups, and saved contents.
    public Vector2Int GetGridSize(InventoryType type, IEnumerable<ItemInstance> items = null)
    {
        var grid = Array.Find(this[type].GetComponentsInChildren<InventoryGrid>(true), candidate => candidate.id == 0);
        Vector2Int size = type == InventoryType.EightColumnFull ? new Vector2Int(8, BagRows) : grid.gridSize;
        if (type == InventoryType.EightColumnFull && items != null)
        {
            foreach (var item in items)
            {
                int occupiedRows = checked(item.Position.y + InventoryPlacement.Size(item.Definition, item.Rotation).y);
                size.y = Mathf.Max(size.y, occupiedRows);
            }
        }
        return size;
    }

    private Dictionary<InventoryType, GameObject> dict;
    public List<InventoryPrefab> AllPrefabs;
    public GameObject this[InventoryType type]
    {
        get
        {
            Init();
            return dict[type];
        }
    }

    private void Init()
    {
        if (dict != null)
            return;

        dict = new Dictionary<InventoryType, GameObject>();
        foreach (var inventory in AllPrefabs)
        {
            dict[inventory.invType] = inventory.prefab;
        }
    }
}