using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ItemSaveData saveData;

    /// <summary>
    /// Image element responsible for showing the item icon.
    /// </summary>
    public Image icon;

    /// <summary>
    /// Image element responsible for showing the item icon background.
    /// </summary>
    public Image background;

    /// <summary>
    /// Target rotation of the 
    /// </summary>
    private Vector3 rotateTarget;

    /// <summary>
    /// Boolean that indicates whether the item was rotated.
    /// </summary>
    public bool isRotated;

    /// <summary>
    /// Rotation index, used to tell the item when to rotate at the right time.
    /// </summary>
    public int rotateIndex;

    /// <summary>
    /// The indexed position is the position of the item relative to the grid in which the item is located.
    /// </summary>
    public Vector2Int indexPosition { get; set; }

    /// <summary>
    /// Reference of the main inventory to which the script communicates.
    /// </summary>
    public Inventory inventory { get; set; }

    /// <summary>
    /// Reference of the RectTransform.
    /// </summary>
    public RectTransform rectTransform { get; set; }

    /// <summary>
    /// Grid the item is currently in.
    /// </summary>
    public InventoryGrid inventoryGrid { get; set; }

    /// <summary>
    /// Correct position using the rotation ratio.
    /// </summary>
    public SizeInt correctedSize
    {
        get => new(!isRotated ? saveData.data.size.width : saveData.data.size.height, !isRotated ? saveData.data.size.height : saveData.data.size.width);
    }

    private ItemToolTip tooltip;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    private void Start()
    {
        tooltip = inventory.GetItemTooltip();
        icon.sprite = saveData.data.icon;    
        background.color = saveData.data.backgroundColor;
    }

    /// <summary>
    /// LateUpdate is called every frame, if the Behaviour is enabled.
    /// It is called after all Update functions have been called.
    /// </summary>
    private void LateUpdate()
    {
        UpdateRotateAnimation();
    }

    public void StartRotate()
    {
        UpdateRotation();
    }

    /// <summary>
    /// Rotates the item to the correct position the player needs.
    /// </summary>
    public void Rotate()
    {
        if (rotateIndex < 3)
        {
            rotateIndex++;
        }
        else if (rotateIndex >= 3)
        {
            rotateIndex = 0;
        }

        UpdateRotation();
    }

    /// <summary>
    /// Reset the rotate index.
    /// </summary>
    public void ResetRotate()
    {
        rotateIndex = 0;
        UpdateRotation();
    }

    /// <summary>
    /// Update rotation movement.
    /// </summary>
    private void UpdateRotation()
    {
        switch (rotateIndex)
        {
            case 0:
                rotateTarget = new(0, 0, 0);
                isRotated = false;
                break;

            case 1:
                rotateTarget = new(0, 0, -90);
                isRotated = true;
                break;

            case 2:
                rotateTarget = new(0, 0, -180);
                isRotated = false;
                break;

            case 3:
                rotateTarget = new(0, 0, -270);
                isRotated = true;
                break;
        }

        // Might want to store rotation solely on the item data @DW
        saveData.isRotated = isRotated;
        saveData.rotateIndex = rotateIndex;
    }

    /// <summary>
    /// Updates the item rotation animation.
    /// </summary>
    private void UpdateRotateAnimation()
    {
        if (rectTransform == null)
            return;

        Quaternion targetRotation = Quaternion.Euler(rotateTarget);
        if (rectTransform.localRotation != targetRotation)
        {
            rectTransform.localRotation = Quaternion.Slerp(
                rectTransform.localRotation,
                targetRotation,
                InventorySettings.rotationAnimationSpeed * Time.deltaTime
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (saveData.isSlotType == true)
            tooltip.ShowToolTip(saveData.PreviousItemData.itemStats.GetItemName(), saveData.PreviousItemData.itemStats.CreateItemDescriptionText(), saveData.PreviousItemData.rarity.rarityColor);
        else
            tooltip.ShowToolTip(saveData.data.itemStats.GetItemName(), saveData.data.itemStats.CreateItemDescriptionText(), saveData.data.rarity.rarityColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.HideToolTip();
    }
}