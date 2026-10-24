# Equipment

## Current

- Equipment definitions reference replaceable visual prefabs and attachment/bone settings.
- `PlayerEquipment` presents calculated equipment effects to actor components.
- `EquipmentVisualController` applies model attachments and explicitly mapped body visibility changes.
- Temporary geometric armor assets are under `Placeholders`; they use the regular equipment path.
- Equipment UI and character previews are under `UI/Inventory`.

## Agreed

- Equipment behavior is independent of artwork.
- Replacing a model or material must not require replacing the equipping system.
- Keep attachment and body-renderer mappings editable.

## Unfinished

- Final armor artwork and fitted models can replace the placeholders.
- Armor balancing and additional gameplay effects are deferred.
