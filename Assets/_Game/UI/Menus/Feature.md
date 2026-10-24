# Menus

## Current

- Authored prefabs contain frontend, pause, settings, save/load, death, confirmation, and loading presentation. `SceneTransition` is nested in GameUI and FrontEndUI. Save entries use reusable prefabs. CanvasGroups are authored rather than added at startup.

## Agreed

- Fixed pages live in the UI hierarchy. Input and player mode gate interactions. Settings stay separate from playthrough data.

## Unfinished

- Verify menu selection, confirmations, loading overlays, and cursor restoration after scene changes.
