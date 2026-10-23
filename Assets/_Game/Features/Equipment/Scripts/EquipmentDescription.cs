using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class EquipmentTooltipViewModel
{
    public string ItemName { get; set; }
    public string DetailText { get; set; }
    public string SetName { get; set; }
    public List<EquipmentTooltipStat> Stats { get; } = new();
}

public struct EquipmentTooltipStat
{
    public EquipmentStat Stat { get; }
    public string Label { get; }
    public int Value { get; }

    public EquipmentTooltipStat(EquipmentStat stat, string label, int value)
    {
        Stat = stat;
        Label = label;
        Value = value;
    }
}

public static class EquipmentDescription
{
    public static EquipmentTooltipViewModel Build(ItemData item)
    {
        var viewModel = new EquipmentTooltipViewModel();
        if (item == null)
            return viewModel;

        ItemStatsData stats = item.itemStats;
        if (stats == null)
            return viewModel;

        viewModel.ItemName = stats.GetItemName();
        var bonuses = new Dictionary<EquipmentStat, int>();

        int defense = stats.GetDefense();
        if (defense != 0)
            AddBonus(bonuses, EquipmentStat.Defense, defense);

        IReadOnlyList<EquipmentBonus> equipmentBonuses = stats.GetEquipmentBonuses();
        if (equipmentBonuses != null)
        {
            for (int i = 0; i < equipmentBonuses.Count; i++)
            {
                EquipmentBonus bonus = equipmentBonuses[i];
                AddBonus(bonuses, bonus.stat, bonus.points);
            }
        }

        foreach (var bonus in bonuses.OrderBy(entry => entry.Key))
        {
            if (bonus.Value == 0)
                continue;

            viewModel.Stats.Add(new EquipmentTooltipStat(
                bonus.Key,
                GetStatLabel(bonus.Key),
                bonus.Value));
        }

        var details = new List<string>();
        if (stats.GetWeaponType() != WeaponType.NonWeapon && stats.GetDamage() > 0f)
            details.Add("Damage " + stats.GetDamage().ToString("0.##"));

        if (stats.GetValue() > 0)
            details.Add("Value " + stats.GetValue());

        viewModel.DetailText = string.Join("  ·  ", details);

        EquipmentSetDefinition set = stats.GetEquipmentSet();
        viewModel.SetName = set != null && !string.IsNullOrWhiteSpace(set.displayName)
            ? set.displayName
            : null;

        return viewModel;
    }

    private static void AddBonus(Dictionary<EquipmentStat, int> bonuses, EquipmentStat stat, int value)
    {
        bonuses.TryGetValue(stat, out int currentValue);
        bonuses[stat] = currentValue + value;
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

    private static string FormatPoints(int points) => points > 0 ? $"+{points}" : points.ToString();
}
