using System;
using System.Collections.Generic;
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
    [SerializeField, Range(0, 10)] int defense;
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
    public int GetDefense() => Mathf.Clamp(defense, 0, 10);
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

        if (GetDefense() > 0)
            text.Append("\nDefense: +").Append(GetDefense());

        EquipmentDescription.AppendBonusLines(text, equipmentBonuses, "");
        EquipmentDescription.AppendSetRequirements(text, equipmentSet);

        return text.ToString();
    }
}
