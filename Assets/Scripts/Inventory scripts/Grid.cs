using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField] GridType gridType;

    private void Awake()
    {
        gridReader = FindAnyObjectByType<GridManager>();
    }

    private void OnEnable()
    {
        gridId = gridReader.GetNewGridId();
    }

    void Start()
    {
        items = gridReader.GetGridItems(gridId);
    }

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

    #region Player Grid __ NOT IMPLEMENTED
    void OpenPlayer()
    {

    }

    void ClosePlayer()
    {

    }
    #endregion

    #region Equipment Grid __ NOT IMPLEMENTED
    void OpenEquipment()
    {

    }

    void CloseEquipment()
    {

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
