# Equipment authoring

Create each wearable as an `ItemData` asset. Set its `ItemStatsData.weaponType` to `NonWeapon` and choose the matching `armorType` (`Helmet`, `Chestpiece`, `Leggings`, `Boots`, or `Necklace`). Set `defense` for flat incoming damage reduction or increased damage when negative. Add signed integer entries to `equipmentBonuses` for stats such as `ColdResistance`, `Attack`, or `StaminaRecovery`; negative values are penalties.

For a set, create an `EquipmentSetDefinition`, give it a display name, and add one or more tiers with a `requiredPieces` count and bonuses. Assign the same definition to every piece in the set. A tier activates when that many assigned pieces are equipped; its bonuses are recomputed from the equipped items.

Choose a sprite for the inventory icon. To show a model attachment, create an `EquipmentVisualDefinition` and assign it through `ItemStatsData.equipmentVisual`; the player receives an `EquipmentVisualController` automatically. Body renderers are hidden only when their regions are explicitly mapped, so empty mappings leave the player model visible.

Use `RigidBone` for a helmet or accessory attached to a humanoid bone. Use `SkinnedRoot` for clothing exported against the character's matching skeleton and bind pose; matching bone names alone cannot make an incompatible mesh fit. Body regions need separate renderers to hide them independently. Real armor assets still need a visual fit check during movement.

Defense reduces incoming damage by its point total, Attack adds points to weapon hits, and StaminaRecovery adds points per second. Negative Defense increases positive incoming damage by its absolute value; zero or negative damage amounts still cause no damage. Resistance totals and requirement queries are ready for future temperature, elemental damage, and climbing systems; those effects are not implemented here. Equipment bonuses and combined totals may be any signed integer, and requirement checks use the signed net total. Set tiers are cumulative. Equipping and loading recompute totals without changing saved base stats.

Keep each item's `SaveId` in the `ItemCatalog`. The editor menu **Tools > Save System > Refresh Item Catalog** rebuilds the catalog from project item assets, and the build hook refreshes it before player builds.

The example Winter Gear assets demonstrate per-piece defense and cold resistance plus a three-piece slip resistance tier. In Play Mode, use **Tools > Prototyping > Give Example Armor** to place the hat, coat, and boots in the player's inventory through the normal inventory API.
