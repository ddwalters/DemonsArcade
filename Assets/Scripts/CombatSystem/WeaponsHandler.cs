using System.Linq;
using UnityEngine;

public class WeaponsHandler : MonoBehaviour
{

    InventoryManager inventoryManager;

    [Header("Main Hand")]
    private bool _hasRightHandWeapon;
    [SerializeField] GameObject rightHandShortSwordLocation;
    [SerializeField] GameObject rightHandAxeLocation;
    [SerializeField] GameObject rightHandSheildLocation;
    [SerializeField] GameObject rightHandStaffLocation;

    private bool _hasLeftHandWeapon;
    [SerializeField] GameObject leftHandShortSwordLocation;
    [SerializeField] GameObject leftHandAxeLocation;
    [SerializeField] GameObject leftHandSheildLocation;
    [SerializeField] GameObject leftHandStaffLocation;

    private Sword sword;
    private Axe axe;
    private Shield shield;
    private Staff staff;

    private float attackCoolDown;
    private float currentCoolDownTime = 0f;

    private bool playerAttacking;

    private void Start()
    {
        inventoryManager = FindAnyObjectByType<InventoryManager>();
        sword = GetComponentInChildren<Sword>();
        axe = GetComponentInChildren<Axe>();
        shield = GetComponentInChildren<Shield>();
        staff = GetComponentInChildren<Staff>();
    }

    private void Update()
    {
        if (!_hasRightHandWeapon) return;

        if (currentCoolDownTime > 0f)
        {
            currentCoolDownTime -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            playerAttacking = true;
            currentCoolDownTime = attackCoolDown;
        }
    }

    #region right hand
    public void AddItemToPlayerRightHand(ItemStatsData itemStats)
    {
        if (_hasRightHandWeapon) return;

        switch (itemStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                CreateWeapon(itemStats, rightHandShortSwordLocation);
                break;
            case WeaponType.Axe:
                CreateWeapon(itemStats, rightHandAxeLocation);
                break;
            case WeaponType.Shield:
                CreateWeapon(itemStats, rightHandSheildLocation);
                break;
            case WeaponType.Staff:
                CreateWeapon(itemStats, rightHandStaffLocation);
                break;
            default:
                Debug.Log("Not a weapon.");
                return;
        }

        attackCoolDown = itemStats.GetCoolDownTime();
        _hasRightHandWeapon = true;
    }
    public void RemoveItemFromPlayerRightHand()
    {
        if (!_hasRightHandWeapon) return;

        if (rightHandShortSwordLocation.transform.childCount > 0)
            Destroy(rightHandShortSwordLocation.transform.GetChild(0).gameObject);

        if (rightHandAxeLocation.transform.childCount > 0)
            Destroy(rightHandAxeLocation.transform.GetChild(0).gameObject);

        if (rightHandSheildLocation.transform.childCount > 0)
            Destroy(rightHandSheildLocation.transform.GetChild(0).gameObject);

        if (rightHandStaffLocation.transform.childCount > 0)
            Destroy(rightHandStaffLocation.transform.GetChild(0).gameObject);

        _hasRightHandWeapon = false;
    }
    #endregion


    #region left hand
    public void AddItemToPlayerLeftHand(ItemStatsData itemStats)
    {
        if (_hasLeftHandWeapon) return;

        switch (itemStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                CreateWeapon(itemStats, leftHandShortSwordLocation);
                break;
            case WeaponType.Axe:
                CreateWeapon(itemStats, leftHandAxeLocation);
                break;
            case WeaponType.Shield:
                CreateWeapon(itemStats, leftHandSheildLocation);
                break;
            case WeaponType.Staff:
                CreateWeapon(itemStats, leftHandStaffLocation);
                break;
            default:
                Debug.Log("Not a weapon.");
                return;
        }

        attackCoolDown = itemStats.GetCoolDownTime();
        _hasRightHandWeapon = true;
    }
    public void RemoveItemFromPlayerLeftHand()
    {
        if (!_hasLeftHandWeapon) return;

        if (leftHandShortSwordLocation.transform.childCount > 0)
            Destroy(leftHandShortSwordLocation.transform.GetChild(0).gameObject);

        if (leftHandAxeLocation.transform.childCount > 0)
            Destroy(leftHandAxeLocation.transform.GetChild(0).gameObject);

        if (leftHandSheildLocation.transform.childCount > 0)
            Destroy(leftHandSheildLocation.transform.GetChild(0).gameObject);

        if (leftHandStaffLocation.transform.childCount > 0)
            Destroy(leftHandStaffLocation.transform.GetChild(0).gameObject);

        _hasRightHandWeapon = false;
    }
    #endregion

    private void CreateWeapon(ItemStatsData itemStats, GameObject prefabLocation)
    {
        GameObject weapon;
        weapon = Instantiate(itemStats.GetPrefab());
        Destroy(weapon.GetComponent<Rigidbody>());
        weapon.transform.SetParent(prefabLocation.transform);
        weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public ItemStatsData GetCurrentMainHandItemStats()
    {
        return inventoryManager.GetItems(7).list.FirstOrDefault().PreviousItemData.itemStats;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Enemy")
            return;

        EnemyStats enemyStat;
        if (!other.TryGetComponent(out enemyStat))
            return;

        enemyStat.inRange = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag != "Enemy")
            return;

        if (playerAttacking)
        {
            EnemyStats enemyStats = other.GetComponent<EnemyStats>();
            var weaponStats = GetCurrentMainHandItemStats();
            if (weaponStats == null) return;

            switch (weaponStats.GetWeaponType())
            {
                case WeaponType.ShortSword:
                    sword.SwordAttack(weaponStats, enemyStats);
                    break;
                case WeaponType.Axe:
                    axe.AxeAttack(weaponStats, enemyStats);
                    break;
            }

            playerAttacking = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag != "Enemy")
            return;

        EnemyStats enemyStat;
        if (!other.TryGetComponent(out enemyStat))
            return;

        enemyStat.inRange = false;
    }
}
