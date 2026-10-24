# Inventory presentation

## Current

- Both inventory layouts are fixed prefab instances under GameUI/InventoryCanvas/Inventory/PlayerInventory.
- Tab or I opens inventory. Only roster members receive tabs.
- The selected character's appearance, equipment, and bag are shown.
- Weapon/shield slots currently sit side by side below the rings; XP sits lower in the panel.
- Both panels are 600 × 860 authored units, fitted to the available screen. The 576-wide bag has eight columns and a 288-high viewport showing four complete default rows, with narrow border padding. Character tabs and equipment sit higher to make room.
- The character name and portrait sit directly above XP. Inventory, gold, and bag-size headings are removed.
- The bag scrollbar sits beside its viewport and matches its height. Opening or changing characters starts at the first bag row.
- The tooltip's border, text, stat row, and set label are authored. Variable stat entries use `InventoryStatChip`.
- Stat icon sprites/colors, fonts, and spacing are editable assets. Tooltip positioning and fitting remain behavior.
- `CharacterPreviewRig` supplies the editable preview camera and lighting on the named InventoryPreview layer; code clones and frames the chosen model and manages its render texture.
- Preview and portrait image crops use Unity version-2 rectangle serialization. Missing that version caused the crop to load at zero size and display no character.
- Preview framing uses the actual posed mesh bounds for skinned characters. Preview skinning stays active while offscreen, and initial rendering does not depend on the image's previous canvas culling state.

## Agreed

- Fixed UI stays in the authored hierarchy and is shown/hidden.
- UI sends operations to gameplay systems and does not own item state.
- Replacing artwork does not replace inventory behavior.

## Unfinished

- Main inventory preview and the complete 8 × 4 layout were visually verified in Unity Play Mode.
- Verify tooltip appearance, drag/drop, member switching, larger bags, and the looting layout in Unity.
- Current layouts can be changed without changing the project-wide design document.
