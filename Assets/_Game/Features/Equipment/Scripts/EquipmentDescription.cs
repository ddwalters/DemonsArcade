using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class EquipmentDescription
{
    public static string Build(ItemData item, EquipmentTotals totals)
    {
        if (item == null)
            return string.Empty;

        var text = new StringBuilder();
        if (item.itemStats != null)
            text.Append(item.itemStats.CreateItemDescriptionText());

        EquipmentSetDefinition set = item.itemStats != null ? item.itemStats.GetEquipmentSet() : null;
        if (set == null)
            return text.ToString();

        int pieceCount = totals != null ? totals.GetSetPieceCount(set) : 0;
        EquipmentSetBonus[] tiers = set.bonuses ?? System.Array.Empty<EquipmentSetBonus>();
        if (tiers.Length == 0)
            return text.ToString();

        text.Append("\nSet progress: ")
            .Append(string.IsNullOrWhiteSpace(set.displayName) ? "Equipment Set" : set.displayName)
            .Append(" (").Append(pieceCount).Append(" equipped)");

        foreach (EquipmentSetBonus tier in tiers
            .Where(tier => tier != null)
            .OrderBy(tier => Mathf.Max(2, tier.requiredPieces)))
        {
            int requiredPieces = Mathf.Max(2, tier.requiredPieces);
            bool isActive = pieceCount >= requiredPieces;

            text.Append("\n")
                .Append(requiredPieces).Append(" pieces (")
                .Append(isActive ? "active" : "inactive")
                .Append(')');

            AppendBonusSummary(text, tier.bonuses);
        }

        return text.ToString();
    }

    internal static void AppendBonusLines(StringBuilder text, IReadOnlyList<EquipmentBonus> bonuses, string prefix)
    {
        if (bonuses == null || bonuses.Count == 0)
            return;

        foreach (var bonus in SumBonuses(bonuses))
        {
            if (bonus.Value == 0)
                continue;

            text.Append('\n').Append(prefix).Append(GetStatLabel(bonus.Key)).Append(": ")
                .Append(FormatPoints(bonus.Value));
        }
    }

    internal static void AppendSetRequirements(StringBuilder text, EquipmentSetDefinition set)
    {
        if (set == null || set.bonuses == null || set.bonuses.Length == 0)
            return;

        var requirements = set.bonuses
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

    private static void AppendBonusSummary(StringBuilder text, IReadOnlyList<EquipmentBonus> bonuses)
    {
        var summedBonuses = SumBonuses(bonuses);
        if (summedBonuses.Count == 0)
            return;

        text.Append(": ");
        bool first = true;
        foreach (var bonus in summedBonuses)
        {
            if (bonus.Value == 0)
                continue;

            if (!first)
                text.Append(", ");

            text.Append(GetStatLabel(bonus.Key)).Append(' ').Append(FormatPoints(bonus.Value));
            first = false;
        }

        if (first)
            text.Append(": no bonus");
    }

    private static Dictionary<EquipmentStat, int> SumBonuses(IReadOnlyList<EquipmentBonus> bonuses)
    {
        var sums = new Dictionary<EquipmentStat, int>();
        if (bonuses == null)
            return sums;

        for (int i = 0; i < bonuses.Count; i++)
        {
            EquipmentBonus bonus = bonuses[i];
            int points = Mathf.Clamp(bonus.points, 0, 10);
            if (points == 0)
                continue;

            sums.TryGetValue(bonus.stat, out int current);
            sums[bonus.stat] = current + points;
        }

        return sums;
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
