using System;
using UnityEngine;

public class WorldSaveObject : MonoBehaviour
{
    [SerializeField] private string saveId;
    public string SaveId => saveId;

    public SavedWorldObject Capture(ItemCatalog catalog)
    {
        var state = new SavedWorldObject { id = saveId };
        var door = GetComponent<doorInteractable>();
        if (door != null)
        {
            state.opened = door.opened;
            state.interactable = door.IsInteractable;
        }

        var loot = GetComponent<LootingInteractable>();
        var merchant = GetComponent<MerchantInteractable>();
        var inventory = FindAnyObjectByType<InventoryManager>();
        if (loot != null)
            state.items = catalog.Capture(inventory.GetItems(loot.GridId).list);
        else if (merchant != null)
            state.items = catalog.Capture(inventory.GetItems(merchant.GridId).list);

        return state;
    }

    public void Restore(SavedWorldObject state, ItemCatalog catalog)
    {
        if (state.collected)
        {
            gameObject.SetActive(false);
            return;
        }

        var door = GetComponent<doorInteractable>();
        if (door != null)
            door.RestoreState(state.opened, state.interactable);

        var loot = GetComponent<LootingInteractable>();
        var merchant = GetComponent<MerchantInteractable>();
        var inventory = FindAnyObjectByType<InventoryManager>();
        if (loot != null)
            inventory.ReplaceItems(loot.GridId, catalog.Restore(state.items));
        else if (merchant != null)
            inventory.ReplaceItems(merchant.GridId, catalog.Restore(state.items));
    }

#if UNITY_EDITOR
    private void Reset() => saveId = Guid.NewGuid().ToString("N");
#endif
}
