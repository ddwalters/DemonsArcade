using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public static class SaveFeatureChecks
{
    private static int checks;

    [MenuItem("Tools/Validation/Save and Menus")]
    public static void RunAll()
    {
        checks = 0;
        string directory = Path.Combine("Temp", "SaveChecks", Guid.NewGuid().ToString("N"));
        try
        {
            Require(SaveManager.IsSaveScene(SaveManager.TownScene) && !SaveManager.IsSaveScene("Assets/_Game/Scenes/Dungeon/_Dungeon.unity"),
                "Town permits saving and the dungeon does not.");
            var store = new SaveStore(directory);
            var first = store.CreatePlaythrough();
            var second = store.CreatePlaythrough();
            Require(first.characterName == "user_0" && second.characterName == "user_1", "Placeholder names increment.");
            Require(first.id != second.id, "Playthroughs have independent identifiers.");
            var catalog = Resources.Load<ItemCatalog>("ItemCatalog");
            Require(catalog != null && catalog.definitions.Count > 0, "Build includes the item catalog.");
            Require(catalog.definitions.Select(item => item.SaveId).Distinct().Count() == catalog.definitions.Count,
                "Item identifiers are unique.");
            ItemData definition = catalog.definitions[0];
            var items = new System.Collections.Generic.List<ItemInstance> { new ItemInstance(definition, new Vector2Int(2, 1), 1) };
            var saved = catalog.Capture(items);
            var restored = catalog.Restore(saved);
            Require(restored[0].Definition == definition && restored[0].Position == new Vector2Int(2, 1) && restored[0].Rotation == 1,
                "Items round trip through stable identifiers with layout preserved.");
            Require(!ReferenceEquals(restored[0], items[0]), "Loading creates independent item instances.");
            var save = new SaveData
            {
                id = Guid.NewGuid().ToString("N"), playthrough = first,
                savedAtUtc = DateTime.UtcNow.ToString("O"), scenePath = SaveManager.TownScene,
                stats = new PlayerStatsData(), gold = 123, position = new Vector3(1, 2, 3)
            };
            save.inventory.Add(new SavedContainer { slot = 0, items = saved });
            save.world.Add(new SavedWorldObject { id = "pickup", collected = true });
            store.Write(save);
            var loaded = store.Read(first.id, save.id);
            Require(loaded.gold == 123 && loaded.position == save.position && loaded.stats.currentHealth == 10,
                "Player data survives disk serialization.");
            Require(loaded.inventory[0].items[0].definitionId == definition.SaveId && loaded.world[0].collected,
                "Inventory and collected world objects survive disk serialization.");
            save.gold = 456;
            store.Write(save);
            Require(store.Read(first.id, save.id).gold == 456, "Atomic replacement writes valid data.");
            Require(Directory.GetFiles(directory, "*.bak", SearchOption.AllDirectories).Length == 1, "Replacement preserves a backup.");
            save.id = Guid.NewGuid().ToString("N");
            save.playthrough = second;
            store.Write(save);
            Require(store.List().Count == 2, "Saves from both playthroughs appear in the collection.");
            File.WriteAllText(Path.Combine(directory, second.id, "broken.save.json"), "{}");
            Require(store.List().Count == 2, "An invalid file does not hide valid saves.");
            store.Delete(first.id, loaded.id);
            Require(store.List().Count == 1 && store.List()[0].playthrough.id == second.id, "Deleting one save preserves other playthroughs.");
            bool rejected = false;
            try { store.Read("../", save.id); }
            catch (InvalidDataException) { rejected = true; }
            Require(rejected, "File identifiers cannot escape the save directory.");
            CheckConfirmation();
            CheckPrefabs();
            CheckInventoryLayout();
            Debug.Log($"[Save Features] PASS: {checks} regression checks.");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static void CheckConfirmation()
    {
        var root = new GameObject("Confirmation check");
        root.SetActive(false);
        try
        {
            var dialog = root.AddComponent<ConfirmationDialog>();
            dialog.mainText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            dialog.mainText.transform.SetParent(root.transform);
            dialog.okButton = new GameObject("OK", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            dialog.okButton.transform.SetParent(root.transform);
            dialog.cancelButton = new GameObject("Cancel", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            dialog.cancelButton.transform.SetParent(root.transform);
            bool confirmed = false;
            dialog.Show("Delete save?", () => confirmed = true);
            Require(dialog.IsOpen && !confirmed && dialog.mainText.text == "Delete save?", "Opening confirmation cannot delete a save.");
            dialog.Cancel();
            Require(!confirmed && !dialog.IsOpen, "Cancel dismisses without taking action.");
            dialog.Show("Delete save?", () => confirmed = true);
            typeof(ConfirmationDialog).GetMethod("Confirm", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dialog, null);
            Require(confirmed && !dialog.IsOpen, "OK runs the action and dismisses the dialog.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CheckPrefabs()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/Menus/Prefabs/GameUI.prefab");
        var ui = root.GetComponent<UIController>();
        Require(root.GetComponentsInChildren<Canvas>(true).Any(canvas => canvas.name == "InventoryCanvas"), "Inventory owns a separate Canvas.");
        Require(ui.saveLoadScreen.confirmation == ui.confirmation, "Save deletion uses the shared confirmation dialog.");
        Require(ui.deathScreen.respawnButton.GetComponent<RectTransform>().anchoredPosition.y < 0, "Respawn appears below center.");
        Require(!ui.deathScreen.gameObject.activeSelf && !ui.confirmation.gameObject.activeSelf, "Death and confirmation start hidden.");
        Require(ui.saveLoadScreen.content.GetComponentInParent<ScrollRect>(true) != null, "Save collection is scrollable.");
        Require(root.GetComponentInChildren<Inventory>(true) != null, "The gameplay UI retains inventory wiring.");
        Require(ui.settingsScreen.qualityDropdown != null && ui.settingsScreen.fullscreenToggle != null &&
            ui.settingsScreen.resolutionDropdown != null, "Settings reference their actual controls, rather than similarly named labels.");
        Require(ui.saveLoadScreen.groupPrefab != null && ui.saveLoadScreen.groupPrefab.headerButton != null,
            "Profile groups have a clickable collapse header.");
        var holder = root.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == "PlayerInventory");
        Require(holder.localScale.z > 0f, "Inventory transforms are invertible for cursor hit testing.");
    }

    private static void CheckInventoryLayout()
    {
        var root = new GameObject("Inventory layout check", typeof(RectTransform), typeof(Canvas));
        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = root.GetComponent<RectTransform>();
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            var art = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(panel.transform, false);
            var artRect = art.GetComponent<RectTransform>();
            artRect.sizeDelta = new Vector2(400, 650);
            artRect.anchoredPosition = new Vector2(-121, -7);
            var layout = panel.AddComponent<InventoryPanelLayout>();
            var corners = new Vector3[4];
            foreach (var size in new[] { new Vector2(800, 600), new Vector2(1600, 900), new Vector2(600, 900) })
            {
                canvasRect.sizeDelta = size;
                layout.Configure(canvas, true, false);
                layout.Fit();
                artRect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector3 point = canvasRect.InverseTransformPoint(corner);
                    Require(canvasRect.rect.Contains((Vector2)point), "Inventory artwork fits the viewport at " + size + ".");
                }

                Vector3 center = canvasRect.InverseTransformPoint(artRect.TransformPoint(artRect.rect.center));
                Require(((Vector2)center - canvasRect.rect.center).magnitude < 0.1f, "Inventory artwork is visually centered at " + size + ".");
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
            throw new Exception(description);
        checks++;
    }
}
