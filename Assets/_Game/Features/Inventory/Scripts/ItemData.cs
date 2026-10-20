using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Main")]
    public SizeInt size = new(0, 0);

    [Header("Visual")]
    public Sprite icon;

    public Color backgroundColor;

    public Rarity rarity;

    public ItemStatsData itemStats;
}