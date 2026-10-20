using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class InventoryGrid : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int id;

    /// <summary>
    /// Size of slots that the grid will have 1X1
    /// </summary>
    [Header("Grid Config")]
    public Vector2Int gridSize = new(5, 5);

    /// <summary>
    /// Grid main rect transform reference.
    /// </summary>
    public RectTransform rectTransform;

    /// <summary>
    /// Item array. The matrix determining what items are where
    /// </summary>
    public Item[,] items { get; set; }

    /// <summary>
    /// Main inventory reference.
    /// </summary>
    public Inventory inventory { get; private set; }

    /// <summary>
    /// Main inventory manager reference.
    /// </summary>
    public InventoryManager inventoryManager { get; private set; }

    private void Awake()
    {
        if (rectTransform != null)
        {
            inventory = FindAnyObjectByType<Inventory>();
            inventoryManager = FindAnyObjectByType<InventoryManager>();
            InitializeGrid();
        }
        else
        {
            Debug.LogError("(InventoryGrid) RectTransform not found!");
        }
    }

    /// <summary>
    /// Initialize matrices and grid size.
    /// </summary>
    private void InitializeGrid()
    {
        // Set items matrices
        items = new Item[gridSize.x, gridSize.y];

        // Set grid size
        Vector2 size =
            new(
                gridSize.x * InventorySettings.slotSize.x,
                gridSize.y * InventorySettings.slotSize.y
            );
        rectTransform.sizeDelta = size;
    }

    public void Bind(Inventory owner, InventoryManager manager)
    {
        inventory = owner;
        inventoryManager = manager;
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        InitializeGrid();
    }

    public void UpdateItemsMatrix(int slotX, int slotY, Item newItem, ItemSaveData itemData)
    {
        items[slotX, slotY] = newItem;
        items[slotX, slotY].saveData = itemData;
    }

    public void CloseGrid()
    {
        inventory.CloseAllGrids();
    }

    /// <summary>
    /// Add grid as main mouse grid.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventory.gridOnMouse = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventory.gridOnMouse == this)
            inventory.gridOnMouse = null;
    }
}
