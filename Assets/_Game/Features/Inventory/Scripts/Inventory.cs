using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InventorySettings
{
    public static readonly Vector2Int slotSize = new(72, 72);
    public static readonly float rotationAnimationSpeed = 30f;
}

public class Inventory : MonoBehaviour
{
    public Item itemPrefab;
    public InventoryGrid gridOnMouse { get; set; }
    public InventoryTypeCollection inventoryType;
    public Item selectedItem { get; private set; }
    public InventoryItemTooltip tooltip;

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
    private Vector2Int GridSize(int id) => InventoryManager.IsEquipmentGrid(id)
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

                    var record = new ItemInstance(definition, position, rotation);
                    return inventoryManager.TryAddItem(gridId, record, size);
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
        var main = MainGrid(root);
        main.id = gridId;
        if (!isPlayerGrid)
            foreach (Transform child in root.transform)
                if (child != main.transform)
                    child.gameObject.SetActive(false);

        InventoryPlayerPanel playerPanel = null;
        if (isPlayerGrid && gridId == 0)
        {
            InventoryGrid[] equipmentSlots = root.GetComponentsInChildren<InventoryGrid>(true)
                .Where(slot => slot != main && InventoryManager.IsEquipmentGrid(slot.id)).ToArray();
            GameObject panelRoot = new("PlayerInventoryPanel", typeof(RectTransform), typeof(CanvasRenderer));
            panelRoot.layer = 5;
            RectTransform panelRect = (RectTransform)panelRoot.transform;
            panelRect.SetParent(playerGridHolder, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            playerPanel = panelRoot.AddComponent<InventoryPlayerPanel>();
            playerPanel.Initialize(root, main, equipmentSlots, inventoryManager,
                FindAnyObjectByType<PlayerController>());
            openGrids[gridId] = panelRoot;

            // Populate after the view builder has added ring grid clones so every grid gets
            // the inventory owner binding and any saved records.
            PopulateGrid(main);
            foreach (InventoryGrid slot in playerPanel.EquipmentGrids)
                PopulateGrid(slot);
        }
        else
        {
            openGrids[gridId] = root;
            PopulateGrid(main);
        }

        foreach (var entry in openGrids)
        {
            var layout = entry.Value.GetComponent<InventoryPanelLayout>() ?? entry.Value.AddComponent<InventoryPanelLayout>();
            InventoryPlayerPanel entryPlayerPanel = entry.Value.GetComponent<InventoryPlayerPanel>();
            layout.Configure(canvas, entryPlayerPanel != null, openGrids.Count > 1, entryPlayerPanel);
        }
    }

    private void PopulateGrid(InventoryGrid grid)
    {
        grid.Bind(this);
        foreach (var record in inventoryManager.GetItems(grid.id).list)
        {
            Item view = Instantiate(itemPrefab, grid.rectTransform);
            view.Bind(record, this, grid);
            view.rectTransform.localPosition = IndexToInventoryPosition(view);
            foreach (var cell in InventoryPlacement.Cells(record.Position,
                         InventoryManager.IsEquipmentGrid(grid.id) ? Vector2Int.one : InventoryPlacement.Size(record.Definition, record.Rotation)))
                grid.items[cell.x, cell.y] = view;
        }
    }

    public Sprite SlotIcon(Item item) => inventoryManager.GetSlotIcon(item.inventoryGrid.id,
        item.Instance.Definition);

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

    public void PlaceSelectedItem()
    {
        if (gridOnMouse == null || selectedItem == null)
            return;

        Item overlap = GetItemAtMouseCoords();
        Vector2Int position = InventoryManager.IsEquipmentGrid(gridOnMouse.id) ? Vector2Int.zero : GetSlotAtMouseCoords();
        bool moved;
        if (overlap == null)
            moved = inventoryManager.TryMoveItem(selectedItem.inventoryGrid.id, selectedItem.Instance,
                gridOnMouse.id, position, selectedItem.rotateIndex, gridOnMouse.gridSize);
        else
            moved = inventoryManager.TrySwapItems(selectedItem.inventoryGrid.id, selectedItem.Instance,
                gridOnMouse.id, overlap.Instance, overlap.indexPosition, selectedItem.rotateIndex,
                selectedItem.inventoryGrid.gridSize, gridOnMouse.gridSize);

        if (moved)
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

    public void RemoveItem(Item item)
    {
        if (item == null)
            return;

        inventoryManager.RemoveItem(item.inventoryGrid.id, item.Instance);
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
        (item.indexPosition.x + item.Footprint.x / 2f) * InventorySettings.slotSize.x,
        -(item.indexPosition.y + item.Footprint.y / 2f) * InventorySettings.slotSize.y, 0);

    public Vector2Int GetSlotAtMouseCoords()
    {
        if (gridOnMouse == null)
            return Vector2Int.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(gridOnMouse.rectTransform,
            MenuInput.PointerPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
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

}
