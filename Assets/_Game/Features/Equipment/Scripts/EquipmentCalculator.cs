using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EquipmentCalculator
{
    public static EquipmentTotals Calculate(IEnumerable<ItemData> equippedItems)
    {
        var totals = new Dictionary<EquipmentStat, int>();
        var setPieceCounts = new Dictionary<EquipmentSetDefinition, int>();
        var setOrder = new List<EquipmentSetDefinition>();

        if (equippedItems != null)
        {
            foreach (ItemData item in equippedItems)
            {
                if (item == null || item.itemStats == null)
                    continue;

                ItemStatsData stats = item.itemStats;
                Add(totals, EquipmentStat.Defense, stats.GetDefense());
                Add(totals, stats.GetEquipmentBonuses());

                EquipmentSetDefinition set = stats.GetEquipmentSet();
                if (set == null)
                    continue;

                if (!setPieceCounts.TryGetValue(set, out int pieceCount))
                {
                    setPieceCounts.Add(set, 1);
                    setOrder.Add(set);
                }
                else
                {
                    setPieceCounts[set] = pieceCount + 1;
                }
            }
        }

        var setStatuses = new List<EquipmentSetStatus>(setOrder.Count);
        foreach (EquipmentSetDefinition set in setOrder)
        {
            int pieceCount = setPieceCounts[set];
            EquipmentSetBonus[] setBonuses = set.bonuses ?? System.Array.Empty<EquipmentSetBonus>();
            var tiers = new List<EquipmentSetTierStatus>(setBonuses.Length);

            foreach (EquipmentSetBonus tier in setBonuses
                .Where(tier => tier != null)
                .OrderBy(tier => Mathf.Max(2, tier.requiredPieces)))
            {
                int requiredPieces = Mathf.Max(2, tier.requiredPieces);
                bool isActive = pieceCount >= requiredPieces;
                var tierStatus = new EquipmentSetTierStatus(requiredPieces, isActive, tier.bonuses);
                tiers.Add(tierStatus);

                if (isActive)
                    Add(totals, tier.bonuses);
            }

            setStatuses.Add(new EquipmentSetStatus(set, pieceCount, tiers));
        }

        return new EquipmentTotals(totals, setStatuses);
    }

    private static void Add(Dictionary<EquipmentStat, int> totals, IReadOnlyList<EquipmentBonus> bonuses)
    {
        if (bonuses == null)
            return;

        for (int i = 0; i < bonuses.Count; i++)
        {
            EquipmentBonus bonus = bonuses[i];
            Add(totals, bonus.stat, Mathf.Clamp(bonus.points, 0, 10));
        }
    }

    private static void Add(Dictionary<EquipmentStat, int> totals, EquipmentStat stat, int points)
    {
        if (points == 0)
            return;

        totals.TryGetValue(stat, out int currentTotal);
        totals[stat] = currentTotal + points;
    }
}

public sealed class EquipmentTotals
{
    private readonly Dictionary<EquipmentStat, int> totals;
    private readonly IReadOnlyList<EquipmentSetStatus> setStatuses;

    internal EquipmentTotals(Dictionary<EquipmentStat, int> totals, List<EquipmentSetStatus> setStatuses)
    {
        this.totals = totals;
        this.setStatuses = setStatuses.AsReadOnly();
    }

    public int Get(EquipmentStat stat) => totals.TryGetValue(stat, out int points) ? points : 0;

    public bool MeetsRequirement(EquipmentStat stat, int requiredPoints) => Get(stat) >= requiredPoints;

    public int GetSetPieceCount(EquipmentSetDefinition set)
    {
        if (set == null)
            return 0;

        foreach (EquipmentSetStatus status in setStatuses)
        {
            if (status.Set == set)
                return status.PieceCount;
        }

        return 0;
    }

    public IReadOnlyList<EquipmentSetStatus> GetSetStatuses() => setStatuses;
}

public sealed class EquipmentSetStatus
{
    public EquipmentSetDefinition Set { get; }
    public int PieceCount { get; }
    public IReadOnlyList<EquipmentSetTierStatus> TierStatuses { get; }

    internal EquipmentSetStatus(EquipmentSetDefinition set, int pieceCount, List<EquipmentSetTierStatus> tiers)
    {
        Set = set;
        PieceCount = pieceCount;
        TierStatuses = tiers.AsReadOnly();
    }
}

public sealed class EquipmentSetTierStatus
{
    public int requiredPieces { get; }
    public bool isActive { get; }
    public IReadOnlyList<EquipmentBonus> bonuses { get; }

    internal EquipmentSetTierStatus(int requiredPieces, bool isActive, EquipmentBonus[] bonuses)
    {
        this.requiredPieces = requiredPieces;
        this.isActive = isActive;
        this.bonuses = bonuses != null
            ? System.Array.AsReadOnly((EquipmentBonus[])bonuses.Clone())
            : System.Array.AsReadOnly(System.Array.Empty<EquipmentBonus>());
    }
}
