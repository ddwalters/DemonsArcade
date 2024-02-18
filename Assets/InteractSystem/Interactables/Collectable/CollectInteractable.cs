using Inventory;
using UnityEngine;

public class CollectInteractable : InteractableBase
{
    [SerializeField] private InventoryItem _itemPrefab; // gonna be the same for everything, is there a way to do this in code?
    [SerializeField] private InventoryItemToPlace _itemToPlace;
    public override void OnInteract()
    {
        base.OnInteract();

        //grid is always deactivated. How can I add item while grid is off?
        ItemGrid grid = FindFirstObjectByType<ItemGrid>();

        Debug.Log(grid);

        if (grid.AddInventoryItem(_itemToPlace, _itemPrefab))
        {
            Debug.Log("Added: " + gameObject.name);
            Destroy(gameObject);
            //success!
            
        }
        else
        {
            Debug.Log("Inventory is full");
            //inventory is full
        }
    }
}
