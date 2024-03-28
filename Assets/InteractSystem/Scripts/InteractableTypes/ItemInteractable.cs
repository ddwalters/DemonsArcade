using UnityEngine;

public class CollectInteractable : InteractableBase
{
    [SerializeField] ItemData itemData;

    // will need to be found with tag or gotten through the child of Player gameobject under Inventory
    [SerializeField] InventoryGrid playerGrid;

    Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    public override void OnInteract()
    {
        base.OnInteract();
            
        inventory.AddItemGridSpecific(playerGrid, itemData);
        Destroy(gameObject);
    }
}
