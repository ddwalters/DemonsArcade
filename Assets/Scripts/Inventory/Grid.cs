using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum GridType
{
    Player,
    Equipment,
    Chest,
    Vendor
}

public class Grid : MonoBehaviour
{
    private IGridReader gridReader;

    private int gridId;
    private List<((int x, int y), int itemData)> items;
    private List<GameObject> itemSlotPrefabs = new List<GameObject>();

    [SerializeField] GridType gridType;

    [SerializeField] GameObject slotPrefab; //@DW this needs to become a scriptable object

    private GridLayoutGroup playerLayoutGroup;
    private GridLayoutGroup armourLayoutGroup;
    private GridLayoutGroup weaponLayoutGroup;
    private GridLayoutGroup accessLayoutGroup;

    public bool isOpen = false;

    private void Awake()
    {
        //gridReader = FindAnyObjectByType<GridManager>(); //changed may not work
        gridReader = GetComponentInParent<IGridReader>();
    }

    private void OnEnable()
    {
        gridId = gridReader.GetNewGridId();
    }

    void Start()
    {
        items = gridReader.GetGridItems(gridId);

        if (gridType == GridType.Player)
        {
            playerLayoutGroup = GetComponentInChildren<GridLayoutGroup>();
        }

        if (gridType == GridType.Equipment)
        {
            armourLayoutGroup = GameObject.Find("InventoryManager/Panel/Player_Equipment/ItemParent/Armour").GetComponent<GridLayoutGroup>();
            weaponLayoutGroup = GameObject.Find("InventoryManager/Panel/Player_Equipment/ItemParent/Hands").GetComponent<GridLayoutGroup>();
            accessLayoutGroup = GameObject.Find("InventoryManager/Panel/Player_Equipment/ItemParent/Accessory").GetComponent<GridLayoutGroup>();
        }
    }

    private void Update()
    {

    }

    #region Actions
    public void OpenGridMenu()
    {
        switch (gridType)
        {
            case GridType.Player:
                OpenPlayer();
                break;
            case GridType.Equipment:
                OpenEquipment();
                break;
            case GridType.Chest:
                OpenChest();
                break;
            case GridType.Vendor:
                OpenVendor();
                break;
        }
    }

    public void CloseGridMenu()
    {
        switch (gridType)
        {
            case GridType.Player:
                ClosePlayer();
                break;
            case GridType.Equipment:
                CloseEquipment();
                break;
            case GridType.Chest:
                CloseChest();
                break;
            case GridType.Vendor:
                CloseVendor();
                break;
        }
    }
    #endregion

    #region Player Grid __ NOT IMPLEMENTED
    void OpenPlayer()
    {
        isOpen = true;
        for (int i = 1; i <= 25; i++)
        {
            GameObject go = Instantiate(slotPrefab, playerLayoutGroup.transform);
            itemSlotPrefabs.Add(go);
        }
    }

    void ClosePlayer()
    {
        isOpen = false;
        foreach (GameObject go in itemSlotPrefabs)
        {
            Destroy(go);
        }
    }
    #endregion

    #region Equipment Grid __ NOT IMPLEMENTED
    void OpenEquipment()
    {
        isOpen = true;
        for (int i = 1; i <= 7; i++)
        {
            if (i !<= 4)
            {
                GameObject go = Instantiate(slotPrefab, armourLayoutGroup.transform);
                itemSlotPrefabs.Add(go);
            }
            else if (i == 6)
            {
                GameObject go = Instantiate(slotPrefab, accessLayoutGroup.transform);
                itemSlotPrefabs.Add(go);
            }
            else
            {
                GameObject go = Instantiate(slotPrefab, weaponLayoutGroup.transform);
                itemSlotPrefabs.Add(go);
            }
        }
    }

    void CloseEquipment()
    {
        isOpen = false;
        foreach (GameObject go in itemSlotPrefabs)
        {
            Destroy(go);
        }
    }
    #endregion

    #region Chest Grid __ NOT IMPLEMENTED
    void OpenChest()
    {

    }

    void CloseChest()
    {

    }
    #endregion

    #region Vendor Grid __ NOT IMPLEMENTED
    void OpenVendor()
    {

    }

    void CloseVendor()
    {

    }
    #endregion
}