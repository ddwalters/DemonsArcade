using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class ItemStatsData
{
    [SerializeField] string name;
    [SerializeField] float damage;
    [SerializeField] int value;
    [SerializeField] float weaponCoolDownTime;
    [SerializeField] int manaUsage;
    [SerializeField] int defense;
    [SerializeField] List<EquipmentBonus> equipmentBonuses = new();
    [SerializeField] EquipmentSetDefinition equipmentSet;
    [SerializeField] EquipmentVisualDefinition equipmentVisual;

    [SerializeField] GameObject prefab;
    [SerializeField] WeaponType weaponType;
    [SerializeField] ArmorType armorType = ArmorType.None;

    public ItemStatsData()
    {
        equipmentBonuses = new List<EquipmentBonus>();
    }

    public ItemStatsData(ItemStatsData itemStats)
    {
        if (itemStats == null)
        {
            equipmentBonuses = new List<EquipmentBonus>();
            return;
        }

        name = itemStats.name;
        damage = itemStats.damage;
        value = itemStats.value;
        manaUsage = itemStats.manaUsage;
        weaponCoolDownTime = itemStats.weaponCoolDownTime;
        prefab = itemStats.prefab;
        weaponType = itemStats.weaponType;
        armorType = itemStats.armorType;
        defense = itemStats.defense;
        equipmentBonuses = itemStats.equipmentBonuses != null
            ? new List<EquipmentBonus>(itemStats.equipmentBonuses)
            : new List<EquipmentBonus>();
        equipmentSet = itemStats.equipmentSet;
        equipmentVisual = itemStats.equipmentVisual;
    }

    public string GetItemName() => name;
    public float GetDamage() => damage;
    public float GetCoolDownTime() => weaponCoolDownTime;
    public int GetValue() => value;
    public WeaponType GetWeaponType() => weaponType;
    public ArmorType GetArmorType() => armorType;
    public GameObject GetPrefab() => prefab;
    public int GetDefense() => defense;
    public IReadOnlyList<EquipmentBonus> GetEquipmentBonuses() => equipmentBonuses;
    public EquipmentSetDefinition GetEquipmentSet() => equipmentSet;
    public EquipmentVisualDefinition GetEquipmentVisual() => equipmentVisual;

    public string CreateItemDescriptionText()
    {
        var text = new StringBuilder();

        if (weaponType != WeaponType.NonWeapon && damage > 0f)
            text.Append("\nDamage: ").Append(damage);

        if (value > 0)
            text.Append("\nValue: ").Append(value);

        if (GetDefense() != 0)
            text.Append("\nDefense: ")
                .Append(GetDefense() > 0 ? $"+{GetDefense()}" : GetDefense().ToString());

        AppendBonusLines(text, equipmentBonuses);
        AppendSetRequirements(text, equipmentSet);

        return text.ToString();
    }

    private static void AppendBonusLines(StringBuilder text, IReadOnlyList<EquipmentBonus> bonuses)
    {
        if (bonuses == null || bonuses.Count == 0)
            return;

        var totals = new Dictionary<EquipmentStat, int>();
        for (int i = 0; i < bonuses.Count; i++)
        {
            EquipmentBonus bonus = bonuses[i];
            int points = bonus.points;
            totals.TryGetValue(bonus.stat, out int currentTotal);
            totals[bonus.stat] = currentTotal + points;
        }

        foreach (var total in totals.OrderBy(entry => entry.Key))
        {
            if (total.Value == 0)
                continue;

            text.Append('\n').Append(GetStatLabel(total.Key)).Append(": ")
                .Append(total.Value > 0 ? $"+{total.Value}" : total.Value.ToString());
        }
    }

    private static void AppendSetRequirements(StringBuilder text, EquipmentSetDefinition set)
    {
        if (set == null || set.bonuses == null || set.bonuses.Length == 0)
            return;

        int[] requirements = set.bonuses
            .Where(tier => tier != null)
            .Select(tier => Mathf.Max(2, tier.requiredPieces))
            .Distinct()
            .OrderBy(requiredPieces => requiredPieces)
            .ToArray();

        if (requirements.Length == 0)
            return;

        text.Append("\nSet: ")
            .Append(string.IsNullOrWhiteSpace(set.displayName) ? "Equipment Set" : set.displayName)
            .Append(" (requires ")
            .Append(string.Join(", ", requirements))
            .Append(" pieces)");
    }

    private static string GetStatLabel(EquipmentStat stat)
    {
        switch (stat)
        {
            case EquipmentStat.Defense:
                return "Defense";
            case EquipmentStat.Attack:
                return "Attack";
            case EquipmentStat.ColdResistance:
                return "Cold Resistance";
            case EquipmentStat.HeatResistance:
                return "Heat Resistance";
            case EquipmentStat.FireResistance:
                return "Fire Resistance";
            case EquipmentStat.ShockResistance:
                return "Shock Resistance";
            case EquipmentStat.SlipResistance:
                return "Slip Resistance";
            case EquipmentStat.StaminaRecovery:
                return "Stamina Recovery";
            default:
                return stat.ToString();
        }
    }
}
