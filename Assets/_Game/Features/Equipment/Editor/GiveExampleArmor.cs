using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GiveExampleArmor
{
    private const string MenuPath = "Tools/Prototyping/Give Example Armor";
    private static readonly string[] ExampleItemNames =
    {
        "Winter Hat",
        "Winter Coat",
        "Winter Pants",
        "Winter Boots"
    };

    [MenuItem(MenuPath, true)]
    private static bool ValidateGiveExampleArmor() => EditorApplication.isPlaying;

    [MenuItem(MenuPath)]
    private static void GiveArmorToPlayer()
    {
        if (!EditorApplication.isPlaying)
            return;

        ItemCatalog catalog = Resources.Load<ItemCatalog>("ItemCatalog");
        if (catalog == null)
        {
            Debug.LogError("The ItemCatalog resource could not be found.");
            return;
        }

        Inventory inventory = Object.FindAnyObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("No active Inventory component was found in the current scene.");
            return;
        }

        ItemData[] definitions = ExampleItemNames
            .Select(name => catalog.definitions.FirstOrDefault(item =>
                item != null && item.itemStats != null && item.itemStats.GetItemName() == name))
            .ToArray();

        if (definitions.Any(item => item == null))
        {
            Debug.LogError("One or more Winter Gear items are missing from the ItemCatalog.");
            return;
        }

        int addedCount = 0;
        foreach (ItemData definition in definitions)
        {
            if (inventory.AddItem(0, definition))
            {
                addedCount++;
            }
            else
            {
                Debug.LogWarning($"Could not add {definition.itemStats.GetItemName()}; the player inventory may be full.");
            }
        }

        Debug.Log($"Added {addedCount} of {definitions.Length} Winter Gear items to the player inventory.");
    }
}
