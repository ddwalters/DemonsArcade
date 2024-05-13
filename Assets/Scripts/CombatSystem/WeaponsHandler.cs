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

    public void AddItemToPlayerMainHand(ItemStatsData itemStats)
    {
        if (_hasMainHandWeapon) return;

        switch (itemStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                CreateWeapon(itemStats, mainHandShortSwordLocation);
                break;
            case WeaponType.Axe:
                CreateWeapon(itemStats, mainHandAxeLocation);
                break;
            case WeaponType.Shield:
                CreateWeapon(itemStats, mainHandSheildLocation);
                break;
            case WeaponType.Staff:
                CreateWeapon(itemStats, mainHandStaffLocation);
                break;
            default:
                Debug.Log("Not a weapon.");
                return;

        }

        _hasMainHandWeapon = true;
    }
    public void RemoveItemFromPlayerMainHand()
    {
        if (!_hasMainHandWeapon) return;

        if (mainHandShortSwordLocation.transform.childCount > 0)
            Destroy(mainHandShortSwordLocation.transform.GetChild(0).gameObject);

        if (mainHandAxeLocation.transform.childCount > 0)
            Destroy(mainHandAxeLocation.transform.GetChild(0).gameObject);

        if (mainHandSheildLocation.transform.childCount > 0)
            Destroy(mainHandSheildLocation.transform.GetChild(0).gameObject);

        if (mainHandStaffLocation.transform.childCount > 0)
            Destroy(mainHandStaffLocation.transform.GetChild(0).gameObject);

        _hasMainHandWeapon = false;
    }

    private void CreateWeapon(ItemStatsData itemStats, GameObject prefabLocation)
    {
        GameObject weapon;
        weapon = Instantiate(itemStats.GetPrefab());
        Destroy(weapon.GetComponent<Rigidbody>());
        weapon.transform.SetParent(prefabLocation.transform);
        weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}
