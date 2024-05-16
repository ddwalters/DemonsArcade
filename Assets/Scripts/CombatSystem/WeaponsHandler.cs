using System.Linq;
using UnityEngine;

public class WeaponsHandler : MonoBehaviour
{

    [SerializeField] InventoryManager inventoryManager;

    [Header("Main Hand")]
    private bool _hasMainHandWeapon;
    [SerializeField] GameObject mainHandShortSwordLocation;
    [SerializeField] GameObject mainHandAxeLocation;
    [SerializeField] GameObject mainHandSheildLocation;
    [SerializeField] GameObject mainHandStaffLocation;

    private Sword sword;
    private Axe axe;
    private Shield shield;
    private Staff staff;

    private float attackCoolDown;
    private float currentCoolDownTime = 0f;

    private bool playerAttacking;

    private void Start()
    {
        sword = GetComponentInChildren<Sword>();
        axe = GetComponentInChildren<Axe>();
        shield = GetComponentInChildren<Shield>();
        staff = GetComponentInChildren<Staff>();
    }

    private void Update()
    {
        if (!_hasMainHandWeapon) return;

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

        attackCoolDown = itemStats.GetCoolDownTime();
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
