using JetBrains.Annotations;
using UnityEngine;

public class ItemSaveData : MonoBehaviour
{
    public int Id;

    /// <summary>
    /// Data of the item referenced in the Item script
    /// </summary>
    [SerializeField]
    public ItemData data;

    /// <summary>
    /// The previous item data is used to store what the item was prior to becoming a slot item.
    /// </summary>
    [CanBeNull]
    public ItemData PreviousItemData;

    /// <summary>
    /// Indicates if item has changed to fit slot inventory.
    /// </summary>
    public bool isSlotType;

    /// <summary>
    /// First grid position the item is found in a matrix.
    /// </summary>
    public Vector2Int slotPosition;

    /// <summary>
    /// Indicates whether the item is rotated. True being rotated.
    /// </summary>
    public bool isRotated;

    /// <summary>
    /// Indicates item rotation position.
    /// </summary>
    public int rotateIndex;
}