using UnityEngine;

public class ItemInteractable : InteractableBase
{
    [SerializeField] ItemData itemData;

    Inventory inventory;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory>();
    }

    public override void OnInteract()
    {
        base.OnInteract();

        if (inventory.AddItem(0, itemData))
            Destroy(gameObject);
    }
}
