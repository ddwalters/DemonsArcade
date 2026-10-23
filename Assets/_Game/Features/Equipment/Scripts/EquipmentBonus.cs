using System;

public enum EquipmentStat
{
    Defense = 0,
    Attack = 1,
    ColdResistance = 2,
    HeatResistance = 3,
    FireResistance = 4,
    ShockResistance = 5,
    SlipResistance = 6,
    StaminaRecovery = 7
}

[Serializable]
public struct EquipmentBonus
{
    public EquipmentStat stat;

    public int points;

    public EquipmentBonus(EquipmentStat stat, int points)
    {
        this.stat = stat;
        this.points = points;
    }
}
