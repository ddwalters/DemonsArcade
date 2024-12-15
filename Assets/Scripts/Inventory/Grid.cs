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

interface IGridCreator
{
    void OpenGridMenu();

    void CloseGridMenu();
}

public class Grid : MonoBehaviour, IGridCreator
{
    private IGridReader gridReader;

    private int gridId;
    private List<GridItem> items;
    private List<GameObject> itemSlotPrefabs = new List<GameObject>();

    [SerializeField] GridType gridType;

    //It should be a script with both the slot and list?
    GameObject slotPrefab;

    private GridLayoutGroup playerLayoutGroup;
    private GridLayoutGroup armourLayoutGroup;
    private GridLayoutGroup weaponLayoutGroup;
    private GridLayoutGroup accessLayoutGroup;

    private bool isOpen = false;

    private void Awake()
    {
        gridReader = GetComponentInParent<IGridReader>();
    }

    private void OnEnable()
    {
        gridId = gridReader.GetNewGridId();
    }

    void Start()
    {
        items = gridReader.GetGridItems(gridId);
        slotPrefab = gridReader.GetSlotPrefab();

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

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        Debug.Log("Chest!");
    }

    void CloseChest()
    {

    }
    #endregion

    #region Vendor Grid __ NOT IMPLEMENTED
    void OpenVendor()
    {
        Debug.Log("Chest!");
    }

    void CloseVendor()
    {

    }
    #endregion
}