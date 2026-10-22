using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    private EquipmentTotals totals;

    public EquipmentTotals Totals
    {
        get
        {
            if (totals == null)
                totals = EquipmentCalculator.Calculate(Array.Empty<ItemData>());

            return totals;
        }
    }

    public event Action<EquipmentTotals> EquipmentChanged;

    public int GetBonus(EquipmentStat stat) => Totals.Get(stat);

    public bool MeetsRequirement(EquipmentStat stat, int requiredPoints) =>
        Totals.MeetsRequirement(stat, requiredPoints);

    public void SetEquipment(EquipmentTotals equipmentTotals, IEnumerable<ItemData> equippedItems)
    {
        totals = equipmentTotals;

        EquipmentVisualController visualController = GetComponent<EquipmentVisualController>();
        if (visualController != null)
            visualController.ApplyEquipment(equippedItems);

        EquipmentChanged?.Invoke(Totals);
    }
}
