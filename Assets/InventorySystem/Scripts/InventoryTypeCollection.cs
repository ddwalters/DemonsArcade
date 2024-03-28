using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryType
{
    public enum InvType
    {
        ThreeByThree,
        SixByThree
    }
}

[CreateAssetMenu(fileName = "InvTypeCollection", menuName = "Inventory/InvTypeCollection")]
public class InventoryTypeCollection : ScriptableObject
{
    [Serializable]
    public class InventoryPrefab
    {
        public GameObject prefab;
        public InventoryType.InvType invType;
    }

    private Dictionary<InventoryType.InvType, GameObject> dict;
    public InventoryPrefab[] AllPrefabs;
    public GameObject this[InventoryType.InvType type]
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

        dict = new Dictionary<InventoryType.InvType, GameObject>();
        foreach (var inventory in AllPrefabs)
        {
            dict[inventory.invType] = inventory.prefab;
        }
    }
}