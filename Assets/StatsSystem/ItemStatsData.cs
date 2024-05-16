using System;
using UnityEngine;

[Serializable]
public class ItemStatsData
{
    [SerializeField] string name;
    [SerializeField] float damage;
    [SerializeField] int value;
    [SerializeField] float weaponCoolDownTime;
    [SerializeField] int manaUsage;

    [SerializeField] GameObject prefab;
    [SerializeField] WeaponType weaponType;

    public ItemStatsData(ItemStatsData itemStats)
    {
        name = itemStats.name;
        damage = itemStats.damage;
        manaUsage = itemStats.manaUsage;
    }

    public string GetItemName() => name;
    public float GetDamage() => damage;
    public float GetCoolDownTime() => weaponCoolDownTime;
    public int GetValue() => value;
    public WeaponType GetWeaponType() => weaponType;
    public GameObject GetPrefab() => prefab;

    public string CreateItemDescriptionText()
    {
        string text = "";

        text += "\nDamage: " + damage;
        text += "\nValue: " + value;

        return text;
    }
}