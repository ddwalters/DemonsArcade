using Inventory;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestInitializer : MonoBehaviour
{
    [SerializeField, Range(1, 5)]
    private int ChestLevel;

    [SerializeField] private InventoryItem _itemPrefab;
    [SerializeField] private List<InventoryItemToPlace> _chestLevelOne;
    [SerializeField] private List<InventoryItemToPlace> _chestLevelTwo;
    [SerializeField] private List<InventoryItemToPlace> _chestLevelThree;
    [SerializeField] private List<InventoryItemToPlace> _chestLevelFour;
    [SerializeField] private List<InventoryItemToPlace> _chestLevelFive;

    public List<InventoryItemToPlace> GetInventoryItems()
    {
        return ChestLevel switch
        {
            1 => _chestLevelOne,
            2 => _chestLevelTwo,
            3 => _chestLevelThree,
            4 => _chestLevelFour,
            5 => _chestLevelFive,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public InventoryItem GetItemPrefab() => _itemPrefab;
}
