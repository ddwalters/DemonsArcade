using UnityEngine;

public class InputController : MonoBehaviour
{
    [SerializeField] ItemToolTip itemToolTip;

    private Inventory inventory;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.Confined;
            inventory.CreateGrid(0, true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);

            foreach (var grid in grids)
                grid.CloseGrid();

            itemToolTip.HideToolTip();
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}