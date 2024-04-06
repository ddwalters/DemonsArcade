using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class InventoryGrid : MonoBehaviour, IPointerEnterHandler
{
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
    /// Item array.
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

    public void InitializeFakeGrid()
    {
        items = new Item[gridSize.x, gridSize.y];
    }

    public void ConfigureFake(InventoryGrid newGrid)
    {
        items = newGrid.items;
        gridSize = newGrid.gridSize;
    }

    public void CloseGrid()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Add grid as main mouse grid.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventory.gridOnMouse = this;
    }
}