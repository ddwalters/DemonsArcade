using UnityEngine;

public class WeaponsHandler : MonoBehaviour
{
    [SerializeField] GameObject hitCone;
    Mesh hitMesh;

    [Header("Main Hand")]
    private bool _hasMainHandWeapon;
    [SerializeField] GameObject mainHandShortSwordLocation;
    [SerializeField] GameObject mainHandAxeLocation;
    [SerializeField] GameObject mainHandSheildLocation;
    [SerializeField] GameObject mainHandStaffLocation;

    //[Header("Off Hand")]
    //private bool _hasOffHandWeapon;
    //[SerializeField] GameObject offHandShortSwordLocation;
    //[SerializeField] GameObject offHandAxeLocation;
    //[SerializeField] GameObject offHandSheildLocation;
    //[SerializeField] GameObject offHandStaffLocation;

    public void AddItemToPlayerMainHand(ItemStatsData itemStats)
    {
        if (_hasMainHandWeapon) return;

        var weapon = Instantiate(itemStats.GetPrefab());

        // add item to select location
        switch (itemStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                weapon.transform.SetParent(mainHandShortSwordLocation.transform);
                break;
            case WeaponType.Axe:
                weapon.transform.SetParent(mainHandAxeLocation.transform);
                break;
            case WeaponType.Shield:
                weapon.transform.SetParent(mainHandSheildLocation.transform);
                break;
            case WeaponType.Staff:
                weapon.transform.SetParent(mainHandStaffLocation.transform);
                break;
        }

        _hasMainHandWeapon = true;
    }
    public void RemoveItemFromPlayerMainHand()
    {
        if (!_hasMainHandWeapon) return;

        if (mainHandShortSwordLocation.transform.childCount > 0)
            Destroy(mainHandShortSwordLocation.transform.GetChild(0));

        if (mainHandAxeLocation.transform.childCount > 0)
            Destroy(mainHandAxeLocation.transform.GetChild(0));

        if (mainHandSheildLocation.transform.childCount > 0)
            Destroy(mainHandSheildLocation.transform.GetChild(0));

        if (mainHandStaffLocation.transform.childCount > 0)
            Destroy(mainHandStaffLocation.transform.GetChild(0));

        _hasMainHandWeapon = false;
    }

    //public void AddItemToPlayerOffHand(ItemStatsData itemStats)
    //{
    //    if (_hasOffHandWeapon) return;
    //
    //    var weapon = Instantiate(itemStats.GetPrefab());
    //
    //    // add item to select location
    //    switch (itemStats.GetWeaponType())
    //    {
    //        case WeaponType.ShortSword:
    //            weapon.transform.SetParent(offHandShortSwordLocation.transform);
    //            break;
    //        case WeaponType.Axe:
    //            weapon.transform.SetParent(offHandAxeLocation.transform);
    //            break;
    //        case WeaponType.Shield:
    //            weapon.transform.SetParent(offHandSheildLocation.transform);
    //            break;
    //        case WeaponType.Staff:
    //            weapon.transform.SetParent(offHandStaffLocation.transform);
    //            break;
    //    }
    //
    //    _hasOffHandWeapon = true;
    //}

    //public void RemoveItemFromPlayerOffHand()
    //{
    //    if (!_hasOffHandWeapon) return;
    //
    //    if (offHandShortSwordLocation.transform.childCount > 0)
    //        Destroy(offHandShortSwordLocation.transform.GetChild(0));
    //
    //    if (offHandAxeLocation.transform.childCount > 0)
    //        Destroy(offHandAxeLocation.transform.GetChild(0));
    //
    //    if (offHandSheildLocation.transform.childCount > 0)
    //        Destroy(offHandSheildLocation.transform.GetChild(0));
    //
    //    if (offHandStaffLocation.transform.childCount > 0)
    //        Destroy(offHandStaffLocation.transform.GetChild(0));
    //
    //    _hasOffHandWeapon = false;
    //}
}
