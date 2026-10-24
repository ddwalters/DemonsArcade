# Inventory

## Current

- `InventoryManager` validates item placement, moves, swaps, and equipment changes.
- Personal bags and equipment live in `CharacterRecord`; the manager owns scene container registrations only.
- `ItemData` describes an item; `ItemInstance` holds a particular item's position and rotation.
- Character bags currently default to eight columns and four rows. Both are editable on `InvTypeCollection`.
- Reducing dimensions retains occupied rows and columns until their items are moved.
- Equipment slots include two rings, weapon, and shield. Presentation is under `UI/Inventory`.
- Item IDs and catalog validation support save restoration.

## Agreed

- Characters have individual inventories and equipment.
- Keep item definitions, owned items, and UI objects separate.
- Dimensions are configurable. UI reads data and requests changes from the inventory service.
- Scene containers unload with their scene; character belongings survive.

## Unfinished

- General stacking and the planned gold-as-an-item work are deferred.
- Recheck drag/drop, member switching, resizing, and looting in Play Mode after the structural changes.
