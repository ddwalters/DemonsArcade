using UnityEngine;

interface IInventoryMenu
{
    bool GetIsOpen();

    void OpenInventory();

    void Resume();
}

public class InventoryManager : MonoBehaviour, IInventoryMenu
{
    IGridCreator _inventoryCreator;
    IGridCreator _equipmentCreator;

    bool isOpen;

    private void Start()
    {
        _inventoryCreator = GlobalReferences.PlayerInventoryGrid;
        _equipmentCreator = GlobalReferences.PlayerEquipmentGrid;
    }

    public bool GetIsOpen() => isOpen;

    public void OpenInventory()
    {
        if (isOpen)
            return;

        _inventoryCreator.OpenGridMenu();
        _equipmentCreator.OpenGridMenu();
    }

    public void Resume()
    {
        if (!isOpen)
            return;

        _inventoryCreator.OpenGridMenu();
        _equipmentCreator.OpenGridMenu();
    }
}
