using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public static class InventorySettings
{
    /// <summary>
    /// Size that each slot has.
    /// </summary>
    public static readonly Vector2Int slotSize = new(96, 96);

    /// <summary>
    /// Slot scale for external changes. Do not touch.
    /// </summary>
    public static readonly float slotScale = 1f;

    /// <summary>
    /// Speed ​​at which the item will return to its target.
    /// </summary>
    public static readonly float rotationAnimationSpeed = 30f;
}

public class Inventory : MonoBehaviour
{
    /// <summary>
    /// Prefab used to instantiate new items.
    /// </summary>
    [Header("Settings")]
    public Item itemPrefab;

    /// <summary>
    /// Returns the InventoryGrid the mouse is currently on.
    /// </summary>
    public InventoryGrid gridOnMouse { get; set; }

    /// <summary>
    /// Dynamic list that has all inventory grids automatically when starting the game.
    /// </summary>
    public InventoryGrid[] grids { get; private set; }

    /// <summary>
    /// Prefabs of all inventoryTypes
    /// </summary>
    public InventoryTypeCollection inventoryType;

    /// <summary>
    /// Currently selected item.
    /// </summary>
    public Item selectedItem { get; private set; }

    /// <summary>
    /// Tooltip used for info on hovering of an item.
    /// </summary>
    public ItemToolTip tooltip;

    private InventoryManager inventoryManager;

    /// <summary>
    /// UI Canvas used for scaling.
    /// </summary>
    private Canvas canvas;

    private GameObject playerGridHolder;

    private GameObject worldGridHolder;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        inventoryManager = GetComponent<InventoryManager>();
        grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.InstanceID);
        canvas = GetComponentInParent<Canvas>();

        playerGridHolder = GameObject.Find("PlayerInventory");
        worldGridHolder = GameObject.Find("WorldInventory");
    }

    /// <summary>
    /// Selects the item and turns everything necessary to select it and make room for another item on and off
    /// </summary>
    /// <param name="item">Item to be selected.</param>
    public void SelectItem(Item item)
    {
        ClearItemReferences(item);
        selectedItem = item;
        selectedItem.GetComponent<CanvasGroup>().blocksRaycasts = false;
        selectedItem.rectTransform.SetParent(transform);
        selectedItem.rectTransform.SetAsLastSibling();
    }

    /// <summary>
    /// Deselects the current item.
    /// </summary>
    private void DeselectItem()
    {
        selectedItem = null;
    }

    /// <summary>
    /// Checks to see if adding the item will overlap with any currently existing items.
    /// </summary>
    /// <param name="gridSize"></param>
    /// <param name="checkItem">Item used for checking overlaps</param>
    /// <param name="previousItems">All items that grid already contains</param>
    /// <returns>True if no items overlap</returns>
    private bool CheckItemSpot(Vector2Int gridSize, ItemData checkItem, List<ItemData> previousItems, bool isRotated)
    {
        int[,] matrix = new int[gridSize.x, gridSize.y];

        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                matrix[i, j] = 0;

        var width = isRotated ? checkItem.size.height : checkItem.size.width;
        var height = isRotated ? checkItem.size.width : checkItem.size.height;

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                matrix[checkItem.slotPosition.x + i, checkItem.slotPosition.y + j] = 1;

        foreach (ItemData item in previousItems)
        {
            for (int i = 0; i < item.size.width; i++)
            {
                for (int j = 0; j < item.size.height; j++)
                {
                    var slotX = item.slotPosition.x + i;
                    var slotY = item.slotPosition.y + j;

                    if (matrix[slotX, slotY] == 1)
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="itemData"></param>
    /// <returns>False if no item was added</returns>
    public bool AddItem(int gridId, ItemData itemData)
    {
        var inventoryGrid = inventoryType.AllPrefabs.FirstOrDefault(x => x.invType == inventoryManager.GetItems(gridId).type).prefab.GetComponent<InventoryGrid>();
        for (int y = 0; y < inventoryGrid.gridSize.y; y++)
        {
            for (int x = 0; x < inventoryGrid.gridSize.x; x++)
            {
                var currentItems = inventoryManager.GetItems(gridId);
                if (currentItems.list.Count == 0)
                {
                    inventoryManager.AddItem(gridId, itemData);
                    Debug.Log(inventoryManager.GetItems(gridId).list.Count);

                    return true;
                }

                Vector2Int slotPosition = new Vector2Int(x, y);
                if (CheckItemSpot(inventoryGrid.gridSize, itemData, currentItems.list, itemData.isRotated))
                {
                    inventoryManager.AddItem(gridId, itemData);
                    Debug.Log(inventoryManager.GetItems(gridId).list.Count);

                    return true;
                }
            }
        }
        Debug.Log("Failed to add");
        return false;
    }

    public void CreateGrid(int gridId, bool isPlayerGrid)
    {
        // Create grid
        var prefab = inventoryType.AllPrefabs.FirstOrDefault(x => x.invType == inventoryManager.GetItems(gridId).type).prefab;
        var inventory = Instantiate(prefab, isPlayerGrid ? playerGridHolder.transform : worldGridHolder.transform).GetComponent<InventoryGrid>();

        // Get Items if any
        var items = inventoryManager.GetItems(gridId);

        if (items.list.Count == 0)
            return;

        // Add items to grid
        foreach (var itemData in items.list)
        {
            // checking for rotation will need to happen @DW
            Item newItem = Instantiate(itemPrefab);
            newItem.rectTransform = newItem.GetComponent<RectTransform>();
            newItem.rectTransform.SetParent(inventory.rectTransform);
            newItem.rectTransform.sizeDelta = new Vector2(
                itemData.size.width * InventorySettings.slotSize.x,
                itemData.size.height * InventorySettings.slotSize.y
            );

            newItem.indexPosition = new Vector2Int(itemData.slotPosition.x, itemData.slotPosition.y);
            newItem.inventory = this;
            newItem.rectTransform.localScale = new Vector2(itemData.size.width, itemData.size.width);

            for (int xx = 0; xx < itemData.size.width; xx++)
            {
                for (int yy = 0; yy < itemData.size.height; yy++)
                {
                    int slotX = itemData.slotPosition.x + xx;
                    int slotY = itemData.slotPosition.y + yy;

                    inventory.UpdateItemsMatrix(slotX, slotY, newItem, itemData);
                }
            }

            newItem.rectTransform.localPosition = IndexToInventoryPosition(newItem);
            newItem.inventoryGrid = inventory;
        }
    }

    /// <summary>
    /// Remove item from inventory completely.
    /// </summary>
    /// <param name="item">Item to be removed.</param>
    public void RemoveItem(Item item)
    {
        if (item != null)
        {
            ClearItemReferences(item);
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// Moves an item to a new position, first checking if the item is outside the grid or if there is an item in the desired slot.
    /// </summary>
    /// <param name="slotPosition">Position that will be checked as the new position of the item in the inventory.</param>
    /// <param name="item">Item that will be moved to the new position.</param>
    /// <param name="deselectItemInEnd">Boolean that indicates whether the object will be deselected when it finishes moving.</param>
    public void MoveItem(Item item, bool deselectItemInEnd = true)
    {
        Vector2Int slotPosition = GetSlotAtMouseCoords();

        if (ReachedBoundary(slotPosition, gridOnMouse, item.correctedSize.width, item.correctedSize.height))
        {
            Debug.Log("Bounds");
            return;
        }

        if (ExistsItem(slotPosition, gridOnMouse, item.correctedSize.width, item.correctedSize.height))
        {
            Debug.Log("Item");
            return;
        }

        item.indexPosition = slotPosition;
        item.rectTransform.SetParent(gridOnMouse.rectTransform);

        for (int x = 0; x < item.correctedSize.width; x++)
        {
            for (int y = 0; y < item.correctedSize.height; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                gridOnMouse.items[slotX, slotY] = item;
            }
        }

        item.rectTransform.localPosition = IndexToInventoryPosition(item);
        item.inventoryGrid = gridOnMouse;

        if (deselectItemInEnd)
        {
            item.GetComponent<CanvasGroup>().blocksRaycasts = true;
            DeselectItem();
        }
    }

    /// <summary>
    /// Swaps the selected item with the item overlaid by the mouse.
    /// </summary>
    /// <param name="overlapItem">Overlapping item.</param>
    public void SwapItem(Item overlapItem, Item oldSelectedItem)
    {
        if (ReachedBoundary(overlapItem.indexPosition, gridOnMouse, oldSelectedItem.correctedSize.width, oldSelectedItem.correctedSize.height))
        {
            return;
        }

        ClearItemReferences(overlapItem);

        if (ExistsItem(overlapItem.indexPosition, gridOnMouse, oldSelectedItem.correctedSize.width, oldSelectedItem.correctedSize.height))
        {
            RevertItemReferences(overlapItem);
            return;
        }

        SelectItem(overlapItem);
        MoveItem(oldSelectedItem, false);
    }

    /// <summary>
    /// Clears the item position references in the inventory.
    /// </summary>
    /// <param name="item">Item to have references removed.</param>
    public void ClearItemReferences(Item item)
    {
        for (int x = 0; x < item.correctedSize.width; x++)
        {
            for (int y = 0; y < item.correctedSize.height; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                item.inventoryGrid.items[slotX, slotY] = null;
            }
        }
    }

    /// <summary>
    /// Clears the item position references in the inventory.
    /// </summary>
    /// <param name="item">Item to have references removed.</param>
    public void RevertItemReferences(Item item)
    {
        for (int x = 0; x < item.correctedSize.width; x++)
        {
            for (int y = 0; y < item.correctedSize.height; y++)
            {
                int slotX = item.indexPosition.x + x;
                int slotY = item.indexPosition.y + y;

                item.inventoryGrid.items[slotX, slotY] = item;
            }
        }
    }

    /// <summary>
    /// Checks if there is an item in the indicated position.
    /// </summary>
    /// <param name="slotPosition">Position to be checked.</param>
    /// <param name="width">Item width</param>
    /// <param name="height">Item height</param>
    /// <param name="grid">Grid in which the verification should occur.</param>
    /// <returns></returns>
    public bool ExistsItem(Vector2Int slotPosition, InventoryGrid grid, int width = 1, int height = 1)
    {
        if (grid.items == null)
            return false;

        if (ReachedBoundary(slotPosition, grid, width, height))
        {
            return true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int slotX = slotPosition.x + x;
                int slotY = slotPosition.y + y;

                if (grid.items[slotX, slotY] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the indicated position is outside the inventory limit.
    /// </summary>
    /// <param name="slotPosition">Position to be checked.</param>
    /// <param name="width">Item width.</param>
    /// <param name="height">Item height.</param>
    /// <param name="gridReference">Grid in which the verification should occur.</param>
    /// <returns></returns>
    public bool ReachedBoundary(Vector2Int slotPosition, InventoryGrid gridReference, int width = 1, int height = 1)
    {
        if (slotPosition.x + width > gridReference.gridSize.x || slotPosition.x < 0)
        {
            return true;
        }

        if (slotPosition.y + height > gridReference.gridSize.y || slotPosition.y < 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the value of an array sequence at the inventory position.
    /// </summary>
    /// <param name="item">Item to be checked.</param>
    /// <returns></returns>
    public Vector3 IndexToInventoryPosition(Item item)
    {
        Vector3 inventorizedPosition =
            new()
            {
                x = item.indexPosition.x * InventorySettings.slotSize.x
                    + InventorySettings.slotSize.x * item.correctedSize.width / 2,
                y = -(item.indexPosition.y * InventorySettings.slotSize.y
                    + InventorySettings.slotSize.y * item.correctedSize.height / 2
                )
            };

        return inventorizedPosition;
    }

    /// <summary>
    /// Returns the screen of the matrix the mouse is on top of.
    /// </summary>
    /// <returns></returns>
    public Vector2Int GetSlotAtMouseCoords()
    {
        if (gridOnMouse == null)
        {
            return Vector2Int.zero;
        }

        float scaleFactor = canvas.scaleFactor;

        Vector2 gridPosition = new(
            (Input.mousePosition.x - gridOnMouse.rectTransform.position.x) / scaleFactor,
            (gridOnMouse.rectTransform.position.y - Input.mousePosition.y) / scaleFactor);

        Vector2Int slotPosition =
            new(
                (int)(gridPosition.x / InventorySettings.slotSize.x * InventorySettings.slotScale),
                (int)(gridPosition.y / InventorySettings.slotSize.y * InventorySettings.slotScale)
            );

        return slotPosition;
    }

    /// <summary>
    /// Returns an item based on the mouse position.
    /// </summary>
    /// <returns></returns>
    public Item GetItemAtMouseCoords()
    {
        Vector2Int slotPosition = GetSlotAtMouseCoords();

        if (!ReachedBoundary(slotPosition, gridOnMouse))
        {
            return GetItemFromSlotPosition(slotPosition);
        }

        return null;
    }

    /// <summary>
    /// Returns an item based on the slot position.
    /// </summary>
    /// <param name="slotPosition">Slot position to check.</param>
    /// <returns></returns>
    public Item GetItemFromSlotPosition(Vector2Int slotPosition)
    {
        return gridOnMouse.items[slotPosition.x, slotPosition.y];
    }

    /// <summary>
    /// Returns the ItemToolTip script from the tooltip GameObject. Mainly for easy access from new inventory items.
    /// </summary>
    /// <returns></returns>
    public ItemToolTip GetItemTooltip()
    {
        return tooltip;
    }
}