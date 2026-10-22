using UnityEngine;

public class ItemInteractable : InteractableBase
{
    [SerializeField] ItemData itemData;

    public override string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ConfiguredDisplayName))
                return ConfiguredDisplayName.Trim();

            string itemName = itemData != null && itemData.itemStats != null
                ? itemData.itemStats.GetItemName()
                : null;
            if (!string.IsNullOrWhiteSpace(itemName))
                return itemName.Trim();

            if (string.IsNullOrWhiteSpace(TooltipMessage))
                return base.DisplayName;

            const string pickupPrefix = "Pick up ";
            string tooltipName = TooltipMessage.Trim();
            return tooltipName.StartsWith(pickupPrefix, System.StringComparison.OrdinalIgnoreCase)
                ? tooltipName.Substring(pickupPrefix.Length).Trim()
                : tooltipName;
        }
    }

    Inventory inventory;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
    }

    public override void OnInteract()
    {
        base.OnInteract();

        if (inventory.AddItem(0, itemData))
        {
            SaveManager.Instance?.RecordCollected(GetComponent<WorldSaveObject>());
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
