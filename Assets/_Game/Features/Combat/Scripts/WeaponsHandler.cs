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

    private void Awake()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        combatHandler = GetComponent<PlayerCombatHandler>();
    }

    #region right hand
    public void AddItemToPlayerRightHand(ItemStatsData itemStats)
    {
        if (_hasRightHandWeapon)
            return;

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
        if (!_hasRightHandWeapon)
            return;

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

    private void CreateWeapon(ItemStatsData itemStats, GameObject prefabLocation)
    {
        GameObject weapon;
        weapon = Instantiate(itemStats.GetPrefab());

        WorldSaveObject worldSave = weapon.GetComponent<WorldSaveObject>();
        if (worldSave != null)
            DestroyImmediate(worldSave);

        Destroy(weapon.GetComponent<Rigidbody>());
        Destroy(weapon.GetComponent<MeshCollider>());
        weapon.transform.SetParent(prefabLocation.transform);
        weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        combatHandler.SetWeaponStats(itemStats);
    }

    public ItemStatsData GetCurrentRightHandItemStats()
    {
        if (!inventoryManager.GetItems(6).list.Any())
            return null;

        return inventoryManager.GetItems((int)EquipmentSlot.RightHand).list[0].Definition.itemStats;
    }
}
