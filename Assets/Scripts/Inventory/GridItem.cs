using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridItem", menuName = "Inventory/GridItem")]
public class GridItem : ScriptableObject
{
    int parentGridId;

    (int x, int y) _slotPosition;

    public enum ItemType  { ExampleGroup1, ExampleGroup2, ExampleGroup3 }
    public ItemType itemtype;
    [SerializeField] string itemName;
    [SerializeField] Sprite itemSprite;
    [SerializeField] GameObject itemPrefab;

    //Example inspector attribute that will change based on itemtype
    [SerializeField] int exampleAttribute1;

    // Will appear dependant on item type
    public bool exampleAttribute2;
    [SerializeField] int exampleAttribute3;

    public (int x, int y) GetSlotPosition() => _slotPosition;
}