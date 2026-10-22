using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentSet", menuName = "Equipment/Set Definition")]
public class EquipmentSetDefinition : ScriptableObject
{
    public string displayName;
    public EquipmentSetBonus[] bonuses = Array.Empty<EquipmentSetBonus>();
}

[Serializable]
public class EquipmentSetBonus
{
    [Min(2)]
    public int requiredPieces = 3;

    public EquipmentBonus[] bonuses = Array.Empty<EquipmentBonus>();
}
