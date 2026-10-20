using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemSaveData saveData;
    public Image icon;
    public Image background;
    public bool isRotated;
    public int rotateIndex;
    public Vector2Int indexPosition { get; set; }
    public Inventory inventory { get; set; }
    public RectTransform rectTransform { get; set; }
    public InventoryGrid inventoryGrid { get; set; }
    public bool IsHeld { get; private set; }
    private ItemToolTip tooltip;
    private ItemData DisplayData => IsHeld ? InventoryManager.Definition(saveData) : saveData.data;
    public SizeInt correctedSize
    {
        get
        {
            Vector2Int size = InventoryPlacement.Size(DisplayData, rotateIndex);
            return new SizeInt(size.x, size.y);
        }
    }

    public void SetHeld(bool held)
    {
        IsHeld = held;
        tooltip = inventory.GetItemTooltip();
        icon.sprite = !held && saveData.isSlotType ? inventory.SlotIcon(this) : DisplayData.icon;
        background.color = DisplayData.backgroundColor;
        rectTransform.sizeDelta = new Vector2(DisplayData.size.width * InventorySettings.slotSize.x,
            DisplayData.size.height * InventorySettings.slotSize.y);
    }

    public void StartRotate() => isRotated = (rotateIndex & 1) != 0;
    public void Rotate()
    {
        rotateIndex = (rotateIndex + 1) % 4;
        StartRotate();
    }
    public void ResetRotate()
    {
        rotateIndex = 0;
        StartRotate();
    }

    private void LateUpdate()
    {
        if (rectTransform != null)
            rectTransform.localRotation = Quaternion.Slerp(rectTransform.localRotation,
                Quaternion.Euler(0, 0, -90 * rotateIndex), InventorySettings.rotationAnimationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemData definition = InventoryManager.Definition(saveData);
        if (IsHeld || tooltip == null || definition.itemStats == null)
            return;

        tooltip.ShowToolTip(definition.itemStats.GetItemName(), definition.itemStats.CreateItemDescriptionText(),
            definition.rarity != null ? definition.rarity.rarityColor : Color.white);
    }

    public void OnPointerExit(PointerEventData eventData) => tooltip?.HideToolTip();
}
