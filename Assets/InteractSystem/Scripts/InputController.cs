using UnityEngine;

public class InputController : MonoBehaviour
{
    private Inventory inventory;

    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
        inventoryManager = FindAnyObjectByType<InventoryManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.Confined;
            inventory.OpenInventoryGrid(InventoryType.InvType.ThreeByThree, 0, true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);

            foreach (var grid in grids)
                grid.CloseGrid();

            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}