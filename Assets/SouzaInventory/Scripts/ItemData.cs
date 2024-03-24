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
    /// Item icon.
    /// </summary>
    [Header("Visual")]
    public Sprite icon;

    /// <summary>
    /// Background color of the item icon.
    /// </summary>
    public Color backgroundColor;

    public ItemStatsData itemStats;
}