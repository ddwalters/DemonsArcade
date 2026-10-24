# Movement and camera

## Current

- `PlayerController` supplies input to `CharacterMotor`; the motor owns character translation.
- Player objects belong to their scenes and bind to the persistent character record.
- The player prefab includes its mode, equipment, smoothing, and model IK components.
- Humanoid locomotion tuning uses `DefaultHumanoidLocomotionProfile` unless overridden.
- Camera FOV currently defaults to 60 in the Inspector and is absent from player settings.
- Animation, step smoothing, foot placement, and camera presentation use the motor's state.
- `PlayerModeController` gates gameplay, inventory, looting, pause, death, and dialogue input.

## Agreed

- Use responsive code-driven movement, with animation presenting it.
- Turn toward movement rather than walking backward in normal movement.
- Reuse the motor for NPC controllers.
- Keep camera tuning editable in the Inspector and presentation separate from movement rules.

## Unfinished

- Continue polishing stair and ledge grounding and animation transitions.
- Replacement humanoid models need their Animator, IK component, and equipment mappings authored.
- Verify movement and model IK in Play Mode after prefab component changes.
