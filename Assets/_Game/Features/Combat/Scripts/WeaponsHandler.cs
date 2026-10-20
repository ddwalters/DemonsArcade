using System.Linq;
using UnityEngine;

public class WeaponsHandler : MonoBehaviour
{
    InventoryManager inventoryManager;
    PlayerCombatHandler combatHandler;

    [Header("Main Hand")]
    private bool _hasRightHandWeapon;
    [SerializeField] GameObject rightHandShortSwordLocation;
    [SerializeField] GameObject rightHandAxeLocation;
    [SerializeField] GameObject rightHandStaffLocation;

    private void Start()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        combatHandler = GetComponent<PlayerCombatHandler>();
    }

    #region right hand
    public void AddItemToPlayerRightHand(ItemStatsData itemStats)
    {
        if (_hasRightHandWeapon) return;

        switch (itemStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                CreateWeapon(itemStats, rightHandShortSwordLocation);
                combatHandler.SetAttack(true);
                break;
            case WeaponType.Axe:
                CreateWeapon(itemStats, rightHandAxeLocation);
                break;
            case WeaponType.Staff:
                CreateWeapon(itemStats, rightHandStaffLocation);
                break;
            default:
                Debug.Log("Not a weapon.");
                return;
        }

        _hasRightHandWeapon = true;
    }
    public void RemoveItemFromPlayerRightHand()
    {
        if (!_hasRightHandWeapon) return;

        if (rightHandShortSwordLocation.transform.childCount > 0)
            Destroy(rightHandShortSwordLocation.transform.GetChild(0).gameObject);

        if (rightHandAxeLocation.transform.childCount > 0)
            Destroy(rightHandAxeLocation.transform.GetChild(0).gameObject);

        if (rightHandStaffLocation.transform.childCount > 0)
            Destroy(rightHandStaffLocation.transform.GetChild(0).gameObject);

        _hasRightHandWeapon = false;
        combatHandler.SetAttack(false);
    }
    #endregion

    #region left hand
    public void AddItemToPlayerLeftHand(ItemStatsData itemStats)
    {
        // check that it is a shield

        //if (_hasLeftHandWeapon) return;
        //CreateWeapon(itemStats, leftHandSheildLocation);
    }

    public void RemoveItemFromPlayerLeftHand()
    {
        // update to just work with shields for now

        //if (!_hasLeftHandWeapon) return;
        //
        //if (leftHandShortSwordLocation.transform.childCount > 0)
        //    Destroy(leftHandShortSwordLocation.transform.GetChild(0).gameObject);
    }
    #endregion

    private void CreateWeapon(ItemStatsData itemStats, GameObject prefabLocation)
    {
        GameObject weapon;
        weapon = Instantiate(itemStats.GetPrefab());
        Destroy(weapon.GetComponent<Rigidbody>());
        Destroy(weapon.GetComponent<MeshCollider>());
        weapon.transform.SetParent(prefabLocation.transform);
        weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        combatHandler.SetWeaponStats(itemStats);
    }

    public ItemStatsData GetCurrentLeftHandItemStats()
    {
        if (!inventoryManager.GetItems(7).list.Any())
            return null;

        return inventoryManager.GetItems(7).list.FirstOrDefault().PreviousItemData.itemStats;
    }

    public ItemStatsData GetCurrentRightHandItemStats()
    {
        if (!inventoryManager.GetItems(6).list.Any())
            return null;

        return inventoryManager.GetItems(6).list.FirstOrDefault().PreviousItemData.itemStats;
    }
}
