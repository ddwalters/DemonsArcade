# Dialogue

## Current

- `DialogueSession` connects Node Tree to the shared player mode and input bindings.
- `GameUI` contains the `DialogueUI` prefab; the test scene uses that same UI.
- Graphs are under `Data`; portraits and the owner database belong to this feature.
- Current continue inputs: configured interaction/E, left mouse, Space, and right arrow.
- Current back inputs: X, right mouse, and left arrow. Escape/gamepad cancel closes dialogue.
- Gameplay and saving are blocked while a conversation or its reward confirmation is open.
- Node Tree's runner has an optional `persistAcrossScenes` setting, disabled on the scene-owned UI.
- The embedded package's `DialogOwnerEditorWindow` exposes the `ExtendCharacterDetails` and `CharacterDeletionWarning` hooks used by the game character linker.

## Agreed

- Use Node Tree for graph execution, portraits/moods, and rewind rules.
- Keep game-specific behavior in game features and presentation in shared GameUI.
- Backtracking must not undo a dialogue choice.
- Preserve identifiable package integration changes when updating the embedded package.

## Unfinished

- Check package updates for the two character-editor hooks and runner lifetime setting.
- Node Tree has its own optional generated history UI; the game has no authored history panel or history binding.
- Verify dialogue choices, portrait sides, rewind boundaries, and reward confirmation in Play Mode.
