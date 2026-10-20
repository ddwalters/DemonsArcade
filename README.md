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
