# DemonsArcade

currently on hold due to severe scope creep...

## Asset organization

`Assets` has three content roots:

- `_Game`: game-owned scenes, features, world content, UI, shared assets, settings, and editor extensions.
- `ThirdParty`: imported asset packs and libraries. Preserve each pack's internal layout when updating it.
- `_Development`: test scenes, prototypes, and development notes.

Under `_Game`, `Features` groups Player, Combat, Enemies, Inventory, Interaction, Stats, and DungeonGeneration. Keep a feature's scripts, prefabs, animations, and data together. `World` contains Town, Dungeon, terrain, and shared world objects. `UI` separates menus, HUD, and shared interface assets. Use `Shared` only for content used across features.

The enabled build scenes remain in this order:

1. `_Game/Scenes/Menus/_StartScreen.unity`
2. `_Game/Scenes/Town/_Town.unity`
3. `_Game/Scenes/Dungeon/_Dungeon.unity`

Scene filenames and indices are preserved because scene transitions use build indices. Scene companion folders hold their lighting and navigation data. Render pipelines and volume profiles live under `_Game/Settings/Rendering`; input assets and their generated wrapper live under `_Game/Settings/Input`.

Move assets in Unity's Project window, or move their `.meta` files with them, to preserve references. Keep special `Editor` and `Resources` folder behavior intact. Builds and generated logs belong outside `Assets` and are ignored by Git.

## Menus and saves

`GameUI.prefab` contains the gameplay HUD, inventory, menu, and overlay Canvases. `FrontEndUI.prefab` supplies the start scene's menu pages without the gameplay HUD. Their screen layouts can differ while sharing the same scripts. `ConfirmationDialog.prefab` is shared by both and accepts main text plus an OK action; Cancel and Escape dismiss it.

Escape opens Pause. Save, Load, and Settings are separate pages; Back or Escape returns to Pause without resuming. Save creates a new entry. Load and Delete operate on the selected entry. All deletions require OK/Cancel confirmation. Respawn appears after one second and loads the newest save in the active playthrough. If that playthrough has no remaining saves, Respawn returns to the main menu.

New Game creates a profile named `user_0`, `user_1`, and so on, with an initial Town save. Starting Play Mode directly in Town also creates a new profile. Saves live in `Application.persistentDataPath/Saves`, grouped by stable playthrough ID. Global audio, display, and sensitivity settings are separate from character saves.

Saving is permitted only in Town. Dungeons are regenerated on entry and are not saved. Loading, death, held inventory items, and temporary stat effects also prevent saving. Other gameplay systems can add independent restrictions:

```csharp
SaveManager.Instance.BlockSaving(this, "Saving is unavailable during combat.");
// When this system is ready to permit saving again:
SaveManager.Instance.AllowSaving(this);
```

Each owner releases only its own restriction. Destroyed owners no longer block saving. Loading a session clears restrictions from the previous session.

Attach `WorldSaveObject` to persistent Town pickups, containers, merchants, and doors. Each scene instance needs its own stable identifier; do not copy an identifier between objects or regenerate existing identifiers after distributing saves. The current Town objects have identifiers assigned. Collected pickups and remaining container contents are restored with player stats, gold, location, inventory, and equipment. Ordinary enemies are recreated by the scene; dungeon seeds and future progression systems are not included yet.

After adding item definitions, run **Tools > Save System > Refresh Item Catalog** before testing saves in the Editor. Builds refresh the catalog automatically. Item identifiers come from asset GUIDs, so keep `.meta` files when moving assets.

Checks are available under **Tools > Validation > Save and Menus**, **Save and Menus (Play Mode)**, and **Player Features**. The Play Mode checks use an isolated temporary save directory and do not alter the player's save collection.

Save browser profile headers collapse or expand all saves for that playthrough together. Selection is cleared when its profile collapses. Inventory uses height-based Canvas scaling and `InventoryPanelLayout` to center the visible artwork within the viewport; looting gives each panel half the available width. Grid binding preserves the prefab's assigned child grid surface so drawn items and pointer coordinates use the same origin.

Settings controls change the live player's sensitivity, audio mixer volume, and Unity quality level. Resolution and fullscreen target the built game window. A current window size missing from the monitor's resolution list is included explicitly, avoiding a misleading default selection.
