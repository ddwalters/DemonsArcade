using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class InventoryController : MonoBehaviour
{
    public Inventory inventory { get; private set; }
    public GameObject goldCounter;
    private PlayerModeController mode;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        goldCounter = GameObject.Find("GoldCounter");
    }

    private void Start()
    {
        mode = PlayerModeController.ForPlayer(FindAnyObjectByType<PlayerController>());
        mode.ModeChanged += ApplyMode;
        ApplyMode(mode.Mode);
    }

    private void OnDestroy()
    {
        if (mode != null)
            mode.ModeChanged -= ApplyMode;
    }

    private void ApplyMode(PlayerMode next)
    {
        inventory.CloseAllGrids();
        bool inventoryOpen = next == PlayerMode.Inventory || next == PlayerMode.Looting;
        if (goldCounter != null)
            goldCounter.SetActive(inventoryOpen);
        if (!inventoryOpen)
            return;

        inventory.CreateGrid(0, true);
        if (next == PlayerMode.Looting)
            inventory.CreateGrid(mode.LootGridId, false);
    }

    private void Update()
    {
        if (mode == null || (mode.Mode != PlayerMode.Inventory && mode.Mode != PlayerMode.Looting))
            return;

        if (Input.GetKeyDown(KeyCode.Mouse0) && inventory.gridOnMouse != null)
        {
            if (inventory.selectedItem != null)
                inventory.PlaceSelectedItem();
            else
                inventory.SelectItem(inventory.GetItemAtMouseCoords());
        }

        if (inventory.selectedItem == null)
            return;

        if (Input.GetKeyDown(KeyCode.R))
            inventory.selectedItem.Rotate();
        var size = inventory.selectedItem.Footprint;
        float scale = GetComponentInParent<Canvas>().scaleFactor;
        inventory.selectedItem.rectTransform.position = Input.mousePosition + new Vector3(
            (size.x - 1) * InventorySettings.slotSize.x * scale / 2f,
            -(size.y - 1) * InventorySettings.slotSize.y * scale / 2f, 0);
    }
}
