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

    private Sword sword;
    private Axe axe;
    private Shield shield;
    private Staff staff;

    private bool playerAttackingRight;
    private float attackRightHandCoolDown;
    private float currentRightHandCoolDownTime = 0f;

    private bool playerAttackingLeft;
    private float attackLeftHandCoolDown;
    private float currentLeftHandCoolDownTime = 0f;


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
        if (!_hasRightHandWeapon && !_hasLeftHandWeapon) return;

        if (currentRightHandCoolDownTime > 0f)
        {
            currentRightHandCoolDownTime -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            playerAttackingLeft = true;
            playerAttackingRight = true;
            currentRightHandCoolDownTime = attackRightHandCoolDown;
        }

        //if (currentLeftHandCoolDownTime > 0f)
        //{
        //    currentLeftHandCoolDownTime -= Time.deltaTime;
        //    return;
        //}
        //
        //if (Input.GetKeyDown(KeyCode.Mouse1))
        //{
        //    playerAttackingLeft = true;
        //    currentLeftHandCoolDownTime = attackLeftHandCoolDown;
        //}
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

        attackRightHandCoolDown = itemStats.GetCoolDownTime();
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
            default:
                Debug.Log("Not a weapon.");
                return;
        }

        attackLeftHandCoolDown = itemStats.GetCoolDownTime();
        _hasLeftHandWeapon = true;
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

        _hasRightHandWeapon = false;
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
    }

    public ItemStatsData GetCurrentLeftHandItemStats()
    {
        return inventoryManager.GetItems(6).list.FirstOrDefault().PreviousItemData.itemStats;
    }

    public ItemStatsData GetCurrentRightHandItemStats()
    {
        return inventoryManager.GetItems(7).list.FirstOrDefault().PreviousItemData.itemStats;
    }

    private void TriggerAttack(ItemStatsData weaponStats, Collider other)
    {
        EnemyStats enemyStats = other.GetComponent<EnemyStats>(); // this only attacks one enemy
        switch (weaponStats.GetWeaponType())
        {
            case WeaponType.ShortSword:
                sword.SwordAttack(weaponStats, enemyStats);
                break;
            case WeaponType.Axe:
                axe.AxeAttack(weaponStats, enemyStats);
                break;
        }
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

        if (!playerAttackingLeft && !playerAttackingRight)
            return;

        ItemStatsData weaponStats = null;
        if (playerAttackingLeft && _hasLeftHandWeapon)
        {
            weaponStats = GetCurrentLeftHandItemStats();
            if (weaponStats == null) return;

            TriggerAttack(weaponStats, other);

            playerAttackingLeft = false;
        }

        if (playerAttackingRight && _hasRightHandWeapon)
        {
            weaponStats = GetCurrentRightHandItemStats();
            if (weaponStats == null) return;

            TriggerAttack(weaponStats, other);

            playerAttackingRight = false;
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
