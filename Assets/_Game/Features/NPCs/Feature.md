# NPCs

## Current

- `PatrolNpc` uses A* path requests and the shared character motor for movement.
- Routes, repath timing, and reach distances are Inspector settings.
- `NpcNavigation` exposes grid bounds, node size, climb, slope, and erosion settings.
- NPCs can reference dialogue graphs and bind to game character definitions.
- Patrol/dialogue behavior is a game feature, even when exercised only in the test scene.

## Agreed

- Reuse character behavior where appropriate; do not create separate prototype NPC implementations.
- Being an NPC does not automatically make a character a party member.
- Scene route and navigation assignments are configurable content.

## Unfinished

- Companion following and other AI decisions are deferred.
- The current navigation component creates its grid on startup; support for other graph authoring workflows can be added when needed.
