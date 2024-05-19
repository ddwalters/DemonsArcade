using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class InventoryController : MonoBehaviour
{
    public Inventory inventory { get; private set; }

    private ItemToolTip itemToolTip;

    private PlayerController playerController;
    private bool gridOpen = false;
    private bool tabKeyPressed = false;

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
    }
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    /// 
    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        itemToolTip = inventory.GetItemTooltip();
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !tabKeyPressed)
        {
            tabKeyPressed = true;

            if (!gridOpen)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            tabKeyPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0) && inventory.gridOnMouse != null)
        {
            if (!inventory.ReachedBoundary(inventory.GetSlotAtMouseCoords(), inventory.gridOnMouse))
            {
                if (inventory.selectedItem)
                {
                    Item oldSelectedItem = inventory.selectedItem;
                    Item overlapItem = inventory.GetItemAtMouseCoords();

                    if (overlapItem != null)
                        inventory.SwapItem(overlapItem, oldSelectedItem);
                    else
                        inventory.MoveItem(oldSelectedItem);
                }
                else
                {
                    SelectItemWithMouse();
                }
            }
        }
    }

    private void OpenInventory()
    {
        Cursor.lockState = CursorLockMode.Confined;
        inventory.CreateGrid(0, true);
        gridOpen = true;
        playerController.DisableCameraMovement();
    }

    private void CloseInventory()
    {
        var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
        Cursor.lockState = CursorLockMode.Locked;

        foreach (var grid in grids)
        {
            grid.CloseGrid();
        }

        itemToolTip.HideToolTip();
        gridOpen = false;
        playerController.EnableCameraMovement();
    }

    /// <summary>
    /// Select a new item in the inventory.
    /// </summary>
    private void SelectItemWithMouse()
    {
        Item item = inventory.GetItemAtMouseCoords();

        if (item != null)
            inventory.SelectItem(item);
    }

    /// <summary>
    /// Removes the item from the inventory that the mouse is hovering over.
    /// </summary>
    private void RemoveItemWithMouse()
    {
        Item item = inventory.GetItemAtMouseCoords();

        if (item != null)
            inventory.RemoveItem(item);
    }

    /// <summary>
    /// Moves the currently selected object to the mouse.
    /// </summary>
    private void MoveSelectedItemToMouse()
    {
        inventory.selectedItem.rectTransform.position = new Vector3(
                Input.mousePosition.x
                    + (inventory.selectedItem.correctedSize.width * InventorySettings.slotSize.x / 2)
                    - InventorySettings.slotSize.x / 2,
                Input.mousePosition.y
                    - (inventory.selectedItem.correctedSize.height * InventorySettings.slotSize.y / 2)
                    + InventorySettings.slotSize.y / 2,
                Input.mousePosition.z
            );
    }
}