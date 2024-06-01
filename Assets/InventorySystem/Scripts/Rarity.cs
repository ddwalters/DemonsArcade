using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum itemRarity
{
    common,
    uncommon,
    rare,
    epic,
    legendary
}

[CreateAssetMenu(fileName = "Rarity", menuName = "Inventory/Rarity")]
public class Rarity : ScriptableObject
{
    public itemRarity itemRarity;

    public Color rarityColor;
}
