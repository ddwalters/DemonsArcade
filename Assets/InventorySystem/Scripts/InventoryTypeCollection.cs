using UnityEngine;
using System;
using System.Collections.Generic;

public enum InventoryType
{
    ThreeByThree,
    SixByThree
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

    private Dictionary<InventoryType, GameObject> dict;
    public InventoryPrefab[] AllPrefabs;
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