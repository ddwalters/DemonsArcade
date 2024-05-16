using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InventorySettings
{
    /// <summary>
    /// Size that each slot has.
    /// </summary>
    public static readonly Vector2Int slotSize = new(72, 72);

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

    public WeaponsHandler weaponsHandler;

    private InventoryManager inventoryManager;

    /// <summary>
    /// UI Canvas used for scaling.
    /// </summary>
    private Canvas canvas;

    /// <summary>
    /// Player grid location in ui
    /// </summary>
    private GameObject playerGridHolder;

    /// <summary>
    /// General grid location in ui
    /// </summary>
    private GameObject worldGridHolder;

    /// <summary>
    /// Location used for holding Inventory item during movement
    /// </summary>
    private GameObject heldItemHolder;

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
        heldItemHolder = GameObject.Find("HeldItemHolder");
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
    private (bool available, Vector2Int? slotPosition) CheckItemSpot(Vector2Int gridSize, Vector2Int slotPosition, ItemSaveData checkItem, List<ItemSaveData> previousItems, bool isRotated)
    {
        int[,] matrix = new int[gridSize.x, gridSize.y];

        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                matrix[i, j] = 0;

        var width = slotPosition.x + (isRotated ? checkItem.data.size.height : checkItem.data.size.width);
        var height = slotPosition.y + (isRotated ? checkItem.data.size.width : checkItem.data.size.height);

        // checks if item will go out of bounds
        if (width > matrix.GetLength(0) || height > matrix.GetLength(1))
            return (false, null);

        for (int i = slotPosition.x; i < width; i++)
            for (int j = slotPosition.y; j < height; j++)
                matrix[checkItem.slotPosition.x + i, checkItem.slotPosition.y + j] = 1;

        foreach (ItemSaveData item in previousItems)
        {
            for (int xx = 0; xx < item.data.size.width; xx++)
            {
                for (int yy = 0; yy < item.data.size.height; yy++)
                {
                    var slotX = item.isRotated ? item.slotPosition.x + yy : item.slotPosition.x + xx;
                    var slotY = item.isRotated ? item.slotPosition.y + xx : item.slotPosition.y + yy;

                    if (matrix[slotX, slotY] == 1)
                        return (false, null);
                }
            }
        }

        return (true, new Vector2Int(slotPosition.x, slotPosition.y));
    }

    /// <summary>
    /// Adds an item to a specific grid list in the inventory manager.
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="itemData"></param>
    /// <returns>False if no item was added</returns>
    public bool AddItem(int gridId, ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.Log("No Data");
            return false;
        }

        GameObject saveObject = new GameObject();
        var itemSaveData = saveObject.AddComponent<ItemSaveData>();
        itemSaveData.data = itemData;

        var inventoryGrid = inventoryType.AllPrefabs.FirstOrDefault(x => x.invType == inventoryManager.GetItems(gridId).type).prefab.transform.GetChild(8).GetComponent<InventoryGrid>();
        for (int y = 0; y < inventoryGrid.gridSize.y; y++)
        {
            for (int x = 0; x < inventoryGrid.gridSize.x; x++)
            {
                var currentItems = inventoryManager.GetItems(gridId);
                if (currentItems.list.Count == 0)
                {
                    itemSaveData.slotPosition = new Vector2Int(x, y);
                    if (!inventoryManager.AddItem(gridId, itemSaveData, IsSlotgrid(gridId)))
                        return false;

                    Destroy(saveObject);
                    return true;
                }

                (bool available, Vector2Int? slotPosition) position = CheckItemSpot(inventoryGrid.gridSize, new Vector2Int(x, y), itemSaveData, currentItems.list, itemSaveData.isRotated);
                if (!position.available)
                {
                    itemSaveData.isRotated = true;
                    itemSaveData.rotateIndex = 1;
                    position = CheckItemSpot(inventoryGrid.gridSize, new Vector2Int(x, y), itemSaveData, currentItems.list, itemSaveData.isRotated);
                    if (!position.available)
                    {
                        itemSaveData.isRotated = false;
                        itemSaveData.rotateIndex = 0;
                    }
                }

                if (position.available)
                {
                    itemSaveData.slotPosition = new Vector2Int(x, y);
                    if (!inventoryManager.AddItem(gridId, itemSaveData, IsSlotgrid(gridId)))
                        return false;

                    Destroy(saveObject);
                    return true;
                }
            }
        }
        Destroy(saveObject);

        Debug.Log("Failed to add");
        return false;
    }

    /// <summary>
    /// Adds an item to a specific slot grid list in the inventory manager.
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="itemData"></param>
    /// <returns>False if no item was added</returns>
    public bool AddItem(int gridId, ItemSaveData itemSaveData)
    {
        if (itemSaveData.data == null)
        {
            Debug.Log("No Data");
            return false;
        }

        var currentItems = inventoryManager.GetItems(gridId);
        if (currentItems.list.Count == 0)
        {
            // Checks if item was already in a slot grid
            if (itemSaveData.isSlotType)
                return false;

            itemSaveData.slotPosition = new Vector2Int(0, 0);

            if (!inventoryManager.AddItem(gridId, itemSaveData, IsSlotgrid(gridId)))
                return false;
            return true;
        }

        //item in slot
        Debug.Log("Failed to add");
        return false;
    }

    public void CreateGrid(int gridId, bool isPlayerGrid)
    {
        var prefab = inventoryType.AllPrefabs.FirstOrDefault(x => x.invType == inventoryManager.GetItems(gridId).type).prefab;
        prefab.transform.GetChild(8).GetComponent<InventoryGrid>().id = gridId;
        var backpack = Instantiate(prefab, isPlayerGrid ? playerGridHolder.transform : worldGridHolder.transform);

        #region Main Grid
        var inventoryGrid = backpack.transform.GetChild(8).GetComponent<InventoryGrid>();

        // removes grid decoration and item slots
        // doesn't remove main grid for use in other areas
        if (!isPlayerGrid)
        {
            var parentGameObject = inventoryGrid.transform.parent.gameObject;
            for (int i = 0; i < parentGameObject.transform.childCount - 1; i++)
            {
                parentGameObject.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        var items = inventoryManager.GetItems(gridId).list;

        foreach (var itemSaveData in items)
        {
            ItemData selectItemData;
            if (itemSaveData.PreviousItemData != null)
                selectItemData = itemSaveData.PreviousItemData;
            else
                selectItemData = itemSaveData.data;

            Item newItem = Instantiate(itemPrefab);
            newItem.rectTransform = newItem.GetComponent<RectTransform>();
            newItem.rectTransform.SetParent(inventoryGrid.rectTransform);
            newItem.rectTransform.sizeDelta = new Vector2(
                selectItemData.size.width * InventorySettings.slotSize.x,
                selectItemData.size.height * InventorySettings.slotSize.y
            );

            newItem.saveData = itemSaveData;
            newItem.indexPosition = new Vector2Int(itemSaveData.slotPosition.x, itemSaveData.slotPosition.y);
            newItem.inventory = this;
            newItem.rectTransform.localScale = new Vector2(1, 1);
            newItem.rotateIndex = itemSaveData.rotateIndex;
            newItem.StartRotate(); // gives fun rotation animation when opening inv (Do we want this? no)

            for (int xx = 0; xx < selectItemData.size.width; xx++)
            {
                for (int yy = 0; yy < selectItemData.size.height; yy++)
                {
                    int slotX = itemSaveData.isRotated ? itemSaveData.slotPosition.y + xx : itemSaveData.slotPosition.x + xx;
                    int slotY = itemSaveData.isRotated ? itemSaveData.slotPosition.x + yy : itemSaveData.slotPosition.y + yy;

                    inventoryGrid.UpdateItemsMatrix(slotX, slotY, newItem, itemSaveData);
                }
            }

            newItem.rectTransform.localPosition = IndexToInventoryPosition(newItem);
            newItem.inventoryGrid = inventoryGrid;
        }
        #endregion

        #region Slot Grids
        if (isPlayerGrid)
        {
            var inventoryGrids = backpack.GetComponentsInChildren<InventoryGrid>();

            for (int i = 7; i > 0; i--)
            {
                var items2 = inventoryManager.GetItems(i).list;

                if (items2.Count == 0)
                    continue;

                var inventoryGrid2 = inventoryGrids[Mathf.Abs(i - 7)];
                foreach (var itemSaveData in items2)
                {
                    Item newItem = Instantiate(itemPrefab);
                    newItem.rectTransform = newItem.GetComponent<RectTransform>();
                    newItem.rectTransform.SetParent(inventoryGrid2.rectTransform);
                    newItem.rectTransform.sizeDelta = new Vector2(
                        itemSaveData.data.size.width * InventorySettings.slotSize.x,
                        itemSaveData.data.size.height * InventorySettings.slotSize.y
                    );

                    newItem.saveData = itemSaveData;
                    newItem.indexPosition = new Vector2Int(itemSaveData.slotPosition.x, itemSaveData.slotPosition.y);
                    newItem.inventory = this;
                    newItem.rectTransform.localScale = new Vector2(itemSaveData.data.size.width, itemSaveData.data.size.width);
                    newItem.rotateIndex = itemSaveData.rotateIndex;
                    newItem.StartRotate(); // gives fun rotation animation when opening inv (Do we want this?)

                    for (int xx = 0; xx < itemSaveData.data.size.width; xx++)
                    {
                        for (int yy = 0; yy < itemSaveData.data.size.height; yy++)
                        {
                            int slotX = itemSaveData.isRotated ? itemSaveData.slotPosition.y + xx : itemSaveData.slotPosition.x + xx;
                            int slotY = itemSaveData.isRotated ? itemSaveData.slotPosition.x + yy : itemSaveData.slotPosition.y + yy;

                            inventoryGrid2.UpdateItemsMatrix(slotX, slotY, newItem, itemSaveData);
                        }
                    }

                    newItem.rectTransform.localPosition = IndexToInventoryPosition(newItem);
                    newItem.inventoryGrid = inventoryGrid2;
                }
            }
        }
        #endregion
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
        
        bool isSlotGrid = IsSlotgrid(gridOnMouse.id);

        // if item is already in a slot we dont allow moving it
        if (isSlotGrid && item.saveData.isSlotType) return;

        if (gridOnMouse == item.inventoryGrid)
        {
            //adding item to same grid
            item.indexPosition = slotPosition;
            item.saveData.slotPosition = slotPosition;
            item.rectTransform.SetParent(gridOnMouse.rectTransform);

            if (!isSlotGrid)
            {
                for (int x = 0; x < item.correctedSize.width; x++)
                {
                    for (int y = 0; y < item.correctedSize.height; y++)
                    {
                        int slotX = item.indexPosition.x + x;
                        int slotY = item.indexPosition.y + y;

                        gridOnMouse.items[slotX, slotY] = item;
                    }
                }
            }

            item.rectTransform.localPosition = IndexToInventoryPosition(item);
            item.inventoryGrid = gridOnMouse;

            inventoryManager.UpdateItemData(gridOnMouse.id, item.saveData);
        }
        else if (isSlotGrid)
        {
            // adding item to slot grid
            int storedItemGridId = item.inventoryGrid.id;

            item.indexPosition = slotPosition;
            item.saveData.slotPosition = slotPosition;
            item.rectTransform.SetParent(gridOnMouse.rectTransform);
            item.rectTransform.localPosition = IndexToInventoryPosition(item);
            item.inventoryGrid = gridOnMouse;

            if (gridOnMouse.inventory.AddItem(gridOnMouse.id, item.saveData))
                inventoryManager.RemoveItem(storedItemGridId, item.saveData.Id);
        }
        else
        {
            // adding item to different grid
            int storedItemGridId = item.inventoryGrid.id;

            item.indexPosition = slotPosition;
            item.saveData.slotPosition = slotPosition;
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

            // adding to grid
            bool success;
            if (!item.saveData.isSlotType)
            {
                // moving between normal grids
                success = gridOnMouse.inventory.AddItem(gridOnMouse.id, item.saveData.data);
            }
            else
            {
                // moving to a grid from a slot
                success = gridOnMouse.inventory.AddItem(gridOnMouse.id, item.saveData.PreviousItemData);

                if (success)
                    weaponsHandler.RemoveItemFromPlayerMainHand();
            }

            // removed item from previous grid
            if (success)
                inventoryManager.RemoveItem(storedItemGridId, item.saveData.Id);
        }

        // deletes grid
        if (isSlotGrid)
            gridOnMouse.GetComponent<InventoryGrid>().CloseGrid();
        else
            gridOnMouse.CloseGrid();

        // re-creates grids
        if (isSlotGrid || gridOnMouse.id == 0)
            CreateGrid(0, true);
        else
            CreateGrid(gridOnMouse.id, false);

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
        bool isSlotGrid = false;
        for (int i = 1; i < 8; i++)
        {
            if (gridOnMouse.id == i)
                isSlotGrid = true;
        }

        if (grid.items == null || ReachedBoundary(slotPosition, grid, width, height) || isSlotGrid)
            return false;

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
        // If its a slot grid we ignore this due to it's unique property of using a placeholder item
        for (int i = 1; i < 8; i++)
        {
            if (gridOnMouse.id == i)
                return false;
        }

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
            for (int i = 1; i < 8; i++)
            {
                if (gridOnMouse.id == i)
                    return GetItemFromSlotPosition(new Vector2Int(0, 0));
            }

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
    /// Removes item currently being held by the player when closing the grid.
    /// Prevents lingering item after grid closes.
    /// </summary>
    public void ClosingGrid()
    {
        // not a good route to go. Difficult with how mouse is tracked
        //Destroy(heldItemHolder.transform.GetChild(0));
    }

    private bool IsSlotgrid(int gridId)
    {
        if (gridId == 1 || gridId == 2 || gridId == 3 || gridId == 4 || gridId == 5 || gridId == 6 || gridId == 7)
            return true;
        else 
            return false;
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