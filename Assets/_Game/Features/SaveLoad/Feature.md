# Save and load

## Current

- Scene-authored `SaveSession` provides one persistent `SaveManager` instance.
- Save files contain character/item data, party records, actor location, and identified world-object states.
- Scene eligibility comes from `SceneConfiguration`, rather than a Town-only check.
- A save records its actual scene path; loading resolves that path in the build settings.
- Active conversations, held items, loading, death, and temporary effects currently block saving.
- `ItemCatalog` resolves stable item IDs. The editor builder refreshes it before builds.
- Settings are stored separately from playthrough saves.

## Agreed

- Persist session data independently of scene objects.
- Prototyping's progress opt-out must be configurable.
- Do not establish main-game saving-location rules here.
- Preserve existing save data and validate it before replacing the live session.

## Unfinished

- Procedurally generated layouts and full dungeon/encounter restoration are not implemented; saving in a scene does not reconstruct those systems.
- General quest persistence is not implemented.
- Verify old saves, actual-scene loading, and cross-scene record persistence in Play Mode.
