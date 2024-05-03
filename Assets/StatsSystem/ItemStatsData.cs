using System;
using UnityEngine;

[Serializable]
public class ItemStatsData
{
    [SerializeField] string name;
    [SerializeField] int cost;
    [SerializeField] int damage;
    [SerializeField] int manaUsage;

    [Header("Level Growth Modifiers")]
    [SerializeField] float XpPercentBoost;

    [Header("Core Stats Modifiers")]
    [SerializeField] int maxHealthBonus;
    [SerializeField] int maxStaminaBonus;
    [SerializeField] int maxManaBonus;

    [Header("Stat Boost Modifiers")]
    [SerializeField] int strengthBoost;
    [SerializeField] int agilityBoost;
    [SerializeField] int vitalityBoost;
    [SerializeField] int enduranceBoost;
    [SerializeField] int intelligenceBoost;
    [SerializeField] int luckBoost;

    public ItemStatsData(ItemStatsData itemStats)
    {
        name = itemStats.name;
        damage = itemStats.damage;
        manaUsage = itemStats.manaUsage;

        XpPercentBoost = itemStats.XpPercentBoost;

        maxHealthBonus = itemStats.maxHealthBonus;
        maxStaminaBonus = itemStats.maxStaminaBonus;
        maxManaBonus = itemStats.maxManaBonus;

        strengthBoost = itemStats.strengthBoost;
        agilityBoost = itemStats.agilityBoost;
        vitalityBoost = itemStats.vitalityBoost;
        enduranceBoost = itemStats.enduranceBoost;
        intelligenceBoost = itemStats.intelligenceBoost;
        luckBoost = itemStats.luckBoost;
    }

    public string GetItemName() => name;
    public int GetCost() => cost;

    public string CreateItemDescriptionText()
    {
        string text = "";

        if (cost > 0)
            text += "\nCost: " + cost;
        if (XpPercentBoost > 0)
            text += "\nXP Boost: " + XpPercentBoost + "%";
        if (maxHealthBonus > 0)
            text += "\nHealth Bonus: +" + maxHealthBonus;
        if (maxStaminaBonus > 0)
            text += "\nStamina Bonus: +" + maxStaminaBonus;
        if (maxManaBonus > 0)
            text += "\nMana Bonus: +" + maxManaBonus;
        if (strengthBoost > 0)
            text += "\nStrength Boost: +" + strengthBoost;
        if (agilityBoost > 0)
            text += "\nAgility Boost: +" + agilityBoost;
        if (vitalityBoost > 0)
            text += "\nVitality Boost: +" + vitalityBoost;
        if (enduranceBoost > 0)
            text += "\nEndurance Boost: +" + enduranceBoost;
        if (intelligenceBoost > 0)
            text += "\nIntelligence Boost: +" + intelligenceBoost;
        if (luckBoost > 0)
            text += "\nLuck Boost: +" + luckBoost;
        if (text.Equals(""))
            text = "Basic Item";

        return text;
    }
}