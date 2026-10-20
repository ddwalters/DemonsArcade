using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class InventoryGrid : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int id;
    public Vector2Int gridSize = new(5, 5);
    public RectTransform rectTransform;
    public Item[,] items { get; private set; }
    private Inventory inventory;

    public void Bind(Inventory owner)
    {
        inventory = owner;
        rectTransform = GetComponent<RectTransform>();
        items = new Item[gridSize.x, gridSize.y];
        rectTransform.sizeDelta = new Vector2(gridSize.x * InventorySettings.slotSize.x, gridSize.y * InventorySettings.slotSize.y);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventory != null)
            inventory.gridOnMouse = this;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventory != null && inventory.gridOnMouse == this)
            inventory.gridOnMouse = null;
    }
}
