using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class InventorySettings
{
    public static readonly Vector2Int slotSize = new(72, 72);
    public static readonly float slotScale = 1f;
    public static readonly float rotationAnimationSpeed = 30f;
}

public class Inventory : MonoBehaviour
{
    public Item itemPrefab;
    public InventoryGrid gridOnMouse { get; set; }
    public InventoryGrid[] grids => GetComponentsInChildren<InventoryGrid>();
    public InventoryTypeCollection inventoryType;
    public Item selectedItem { get; private set; }
    public ItemToolTip tooltip;
    public WeaponsHandler weaponsHandler;
    public Image border;

    private InventoryManager inventoryManager;
    private Canvas canvas;
    private Transform playerGridHolder;
    private Transform worldGridHolder;
    private readonly Dictionary<int, GameObject> openGrids = new();

    private static void DestroyView(Object view)
    {
        if (Application.isPlaying)
            Destroy(view);
        else
            DestroyImmediate(view);
    }

    private void Awake() => BindComponents();

    private void BindComponents()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (playerGridHolder == null)
            playerGridHolder = GameObject.Find("PlayerInventory")?.transform;

        if (worldGridHolder == null)
            worldGridHolder = GameObject.Find("WorldInventory")?.transform;
    }

    private InventoryGrid MainGrid(GameObject prefab) =>
        prefab.GetComponentsInChildren<InventoryGrid>(true).First(g => g.id == 0);

    private GameObject GridPrefab(int id) => inventoryType[inventoryManager.GetItems(id).type];
    public Vector2Int GridSize(int id) => InventoryManager.IsEquipmentGrid(id)
        ? Vector2Int.one : MainGrid(GridPrefab(id)).gridSize;

    public bool AddItem(int gridId, ItemData definition)
    {
        BindComponents();
        if (definition == null)
            return false;

        Vector2Int size = GridSize(gridId);
        for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
                for (int rotation = 0; rotation < 2; rotation++)
                {
                    Vector2Int position = new(x, y);
                    if (!inventoryManager.CanPlace(gridId, definition, position, rotation, size))
                        continue;

                    var record = new GameObject("Inventory item: " + definition.name).AddComponent<ItemSaveData>();
                    record.data = definition;
                    record.slotPosition = position;
                    record.rotateIndex = rotation;
                    record.isRotated = rotation != 0;
                    if (inventoryManager.TryAddItem(gridId, record, size))
                        return true;

                    Destroy(record.gameObject);
                    return false;
                }

        return false;
    }

    public void CreateGrid(int gridId, bool isPlayerGrid)
    {
        BindComponents();
        if (openGrids.TryGetValue(gridId, out var previous))
        {
            previous.SetActive(false);
            DestroyView(previous);
        }
        var root = Instantiate(GridPrefab(gridId), isPlayerGrid ? playerGridHolder : worldGridHolder);
        openGrids[gridId] = root;
        var main = MainGrid(root);
        main.id = gridId;
        if (!isPlayerGrid)
            foreach (Transform child in root.transform)
                if (child != main.transform)
                    child.gameObject.SetActive(false);

        PopulateGrid(main);

        if (isPlayerGrid)
            foreach (var slot in root.GetComponentsInChildren<InventoryGrid>())
                if (slot != main && InventoryManager.IsEquipmentGrid(slot.id))
                    PopulateGrid(slot);
    }

    private void PopulateGrid(InventoryGrid grid)
    {
        grid.Bind(this, inventoryManager);
        foreach (var record in inventoryManager.GetItems(grid.id).list)
        {
            Item view = Instantiate(itemPrefab, grid.rectTransform);
            view.rectTransform = view.GetComponent<RectTransform>();
            view.rectTransform.localScale = Vector3.one;
            view.saveData = record;
            view.inventory = this;
            view.inventoryGrid = grid;
            view.indexPosition = record.slotPosition;
            view.rotateIndex = record.rotateIndex;
            view.StartRotate();
            view.SetHeld(false);
            view.rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * view.rotateIndex);
            view.rectTransform.localPosition = IndexToInventoryPosition(view);
            foreach (var cell in InventoryPlacement.Cells(record.slotPosition,
                         InventoryPlacement.Size(record.data, record.rotateIndex)))
                grid.UpdateItemsMatrix(cell.x, cell.y, view, record);
        }
    }

    public Sprite SlotIcon(Item item) => inventoryManager.GetSlotIcon(item.inventoryGrid.id,
        InventoryManager.Definition(item.saveData));

    public void SelectItem(Item item)
    {
        if (selectedItem != null || item == null)
            return;

        selectedItem = item;
        ClearItemReferences(item);
        item.GetComponent<CanvasGroup>().blocksRaycasts = false;
        item.rectTransform.SetParent(transform, true);
        item.rectTransform.SetAsLastSibling();
        item.SetHeld(true);
        tooltip?.HideToolTip();
    }

    public void MoveItem(Item item, bool deselectItemInEnd = true)
    {
        if (gridOnMouse == null || item == null)
            return;

        Vector2Int position = InventoryManager.IsEquipmentGrid(gridOnMouse.id)
            ? Vector2Int.zero : GetSlotAtMouseCoords();
        if (!inventoryManager.TryMoveItem(item.inventoryGrid.id, item.saveData, gridOnMouse.id,
                position, item.rotateIndex, gridOnMouse.gridSize))
            return;

        CompleteDrop();
    }

    public void SwapItem(Item overlapItem, Item heldItem)
    {
        if (gridOnMouse == null || overlapItem == null || heldItem == null)
            return;

        if (!inventoryManager.TrySwapItems(heldItem.inventoryGrid.id, heldItem.saveData,
                gridOnMouse.id, overlapItem.saveData, overlapItem.indexPosition, heldItem.rotateIndex,
                heldItem.inventoryGrid.gridSize, gridOnMouse.gridSize))
            return;

        CompleteDrop();
    }

    private void CompleteDrop()
    {
        DestroyView(selectedItem.gameObject);
        selectedItem = null;
        gridOnMouse = null;
        foreach (int id in openGrids.Keys.ToArray())
            CreateGrid(id, id == 0);
    }

    public void CloseAllGrids()
    {
        if (selectedItem != null)
            DestroyView(selectedItem.gameObject);

        selectedItem = null;
        gridOnMouse = null;
        foreach (var root in openGrids.Values)
        {
            if (root == null)
                continue;

            root.SetActive(false);
            DestroyView(root);
        }

        openGrids.Clear();
        tooltip?.HideToolTip();
    }

    public void ClosingGrid() => CloseAllGrids();

    public void RemoveItem(Item item)
    {
        if (item == null)
            return;

        inventoryManager.RemoveItem(item.inventoryGrid.id, item.saveData.Id);
        if (selectedItem == item)
            selectedItem = null;

        Destroy(item.gameObject);
        foreach (int id in openGrids.Keys.ToArray())
            CreateGrid(id, id == 0);
    }

    public void ClearItemReferences(Item item)
    {
        var matrix = item.inventoryGrid.items;
        for (int x = 0; x < matrix.GetLength(0); x++)
            for (int y = 0; y < matrix.GetLength(1); y++)
                if (matrix[x, y] == item)
                    matrix[x, y] = null;
    }

    public bool ReachedBoundary(Vector2Int position, InventoryGrid grid, int width = 1, int height = 1) =>
        grid == null || (!InventoryManager.IsEquipmentGrid(grid.id) &&
            !InventoryPlacement.Fits(position, new Vector2Int(width, height), grid.gridSize));

    public Vector3 IndexToInventoryPosition(Item item) => new(
        (item.indexPosition.x + item.correctedSize.width / 2f) * InventorySettings.slotSize.x,
        -(item.indexPosition.y + item.correctedSize.height / 2f) * InventorySettings.slotSize.y, 0);

    public Vector2Int GetSlotAtMouseCoords()
    {
        if (gridOnMouse == null)
            return Vector2Int.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(gridOnMouse.rectTransform,
            Input.mousePosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 local);

        return new Vector2Int(Mathf.FloorToInt(local.x / InventorySettings.slotSize.x),
            Mathf.FloorToInt(-local.y / InventorySettings.slotSize.y));
    }

    public Item GetItemAtMouseCoords()
    {
        if (gridOnMouse == null)
            return null;

        Vector2Int cell = InventoryManager.IsEquipmentGrid(gridOnMouse.id) ? Vector2Int.zero : GetSlotAtMouseCoords();
        return ReachedBoundary(cell, gridOnMouse) ? null : gridOnMouse.items[cell.x, cell.y];
    }

    public ItemToolTip GetItemTooltip() => tooltip;
}
