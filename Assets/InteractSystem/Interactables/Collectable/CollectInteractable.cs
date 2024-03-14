using UnityEngine;

public class CollectInteractable : InteractableBase
{
    //[SerializeField] private InventoryItem _itemPrefab; // gonna be the same for everything, is there a way to do this in code?
    //[SerializeField] private InventoryItemToPlace _itemToPlace;
    public override void OnInteract()
    {
        base.OnInteract();

        //var playerGrid = GameObject.Find("PlayerInventoryGrid").GetComponent<ItemGrid>();

        //if (playerGrid == null)
        //{
        //    Debug.Log("Player Grid Not Found. " + playerGrid);
        //    return;
        //}

        //if (playerGrid.AddInventoryItem(_itemToPlace, _itemPrefab))
        //{
        //    Debug.Log("Added: " + gameObject.name);
        //    Destroy(gameObject);
        //}
        //else
        //{
        //    Debug.Log("Inventory is full");
        //}
    }
}
