# Interaction

## Current

- The controller chooses the nearest eligible interactable in the character's forward interaction area.
- It supplies the action and current binding to `UI/Interaction` and places world-space target names.
- The player's head can look slightly toward the target.
- `NpcConversationFacing` turns an NPC toward its conversation participant and aims its head.
- Interactable types handle their own pickup, container, door, or other actions.

## Agreed

- Screen-space action prompts and world-space names have separate roles.
- Interaction must respect facing and the shared player mode.
- NPC conversation facing must not apply to ordinary objects such as signs.

## Unfinished

- Trading behavior and other deferred systems need their own later design work.
- Recheck prompt placement and input binding display in Play Mode with different devices.
