# Scenes and object lifetimes

## Current

- `SceneFlow.Load` is the shared scene activation path. Portals, menu/session actions, and transition wrappers use it.
- Current build scenes: Start Screen (0), Town (1), and Dungeon (2). Prototyping is opened directly for development.
- `SceneConfiguration` exposes gameplay participation, progress saving, and initial-save creation.
- Prototyping opts out of progress saving through configuration. The frontend opts out of gameplay and progress saving.
- Save/session services are authored in scene hierarchies. Duplicate instances are discarded when the existing session continues.
- Scene-owned players bind to character records; Dungeon now has its own player and session-service entry points.
- The transition service persists during scene activation. Its authored loading canvas lives in GameUI/FrontEndUI and reconnects to the service.

## Agreed

- Scenes configure reusable features rather than separate versions of them.
- Opt-outs are feature settings, not scene-name checks.
- Persistent session data must not depend on keeping the character actor alive.
- Keep one scene-loading path. Current scene boundaries may change.

## Unfinished

- Verify menu-to-Town, Town-to-Dungeon, and return transitions in Play Mode.
- The unused Dungeon 3D scene is not part of the current build flow.
- Main-game saving restrictions remain undecided; current scene settings are configuration, not a permanent game rule.
