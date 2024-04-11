using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    /// <summary>
    /// Size in width and height of the item.
    /// </summary>
    [Header("Main")]
    public SizeInt size = new(0, 0);

    /// <summary>
    /// First grid position the item is found in a matrix.
    /// </summary>
    public Vector2Int slotPosition;

    /// <summary>
    /// Indicates whether the item is rotated. True being rotated.
    /// </summary>
    public bool isRotated;

    /// <summary>
    /// Item icon.
    /// </summary>
    [Header("Visual")]
    public Sprite icon;

    /// <summary>
    /// Background color of the item icon.
    /// </summary>
    public Color backgroundColor;

    /// <summary>
    /// The stats for the item, editable in the inspector..
    /// </summary>
    public ItemStatsData itemStats;
}