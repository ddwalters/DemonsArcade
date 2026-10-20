using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public Image background;
    public ItemInstance Instance { get; private set; }
    public Inventory inventory { get; private set; }
    public InventoryGrid inventoryGrid { get; private set; }
    public RectTransform rectTransform { get; private set; }
    public Vector2Int indexPosition => Instance.Position;
    public int rotateIndex { get; private set; }
    public bool IsHeld { get; private set; }
    private bool IsEquipment => InventoryManager.IsEquipmentGrid(inventoryGrid.id);
    public Vector2Int Footprint => IsEquipment && !IsHeld ? Vector2Int.one : InventoryPlacement.Size(Instance.Definition, rotateIndex);

    public void Bind(ItemInstance instance, Inventory owner, InventoryGrid grid)
    {
        Instance = instance;
        inventory = owner;
        inventoryGrid = grid;
        rectTransform = GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rotateIndex = instance.Rotation;
        SetHeld(false);
        rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * rotateIndex);
    }

    public void SetHeld(bool held)
    {
        IsHeld = held;
        ItemData definition = Instance.Definition;
        bool slotView = IsEquipment && !held;
        icon.sprite = slotView ? inventory.SlotIcon(this) : definition.icon;
        background.color = slotView ? Color.clear : definition.backgroundColor;
        Vector2Int size = slotView ? Vector2Int.one : InventoryPlacement.Size(definition, 0);
        rectTransform.sizeDelta = new Vector2(size.x * InventorySettings.slotSize.x, size.y * InventorySettings.slotSize.y);
    }

    public void Rotate() => rotateIndex = (rotateIndex + 1) % 4;

    private void LateUpdate()
    {
        rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation,
            Quaternion.Euler(0, 0, -90 * rotateIndex), InventorySettings.rotationAnimationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemData definition = Instance.Definition;
        if (IsHeld || inventory.tooltip == null || definition.itemStats == null)
            return;

        inventory.tooltip.ShowToolTip(definition.itemStats.GetItemName(), definition.itemStats.CreateItemDescriptionText(),
            definition.rarity != null ? definition.rarity.rarityColor : Color.white);
    }

    public void OnPointerExit(PointerEventData eventData) => inventory.tooltip?.HideToolTip();
}
