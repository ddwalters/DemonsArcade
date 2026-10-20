# Combat playground

Open `Assets/_Development/Scenes/CombatPlayground.unity` and enter Play Mode.
The gold player and red enemy use the imported yBot body, a hand-mounted ShortSword,
and a separate copy of the locomotion controller. Town and its combat setup are unchanged.

For this example the sword uses a hand socket fitted to yBot's palm, with the
ShortSword handle offset from its mesh pivot. A right-fingers-only animation layer
holds a closed grip through idle, movement, attacks, and death without overriding
the arm or root motion. These offsets are specific to this model and weapon;
reusable weapon grip definitions can replace them later.

| Control | Action |
| --- | --- |
| WASD / mouse | Move / face the enemy |
| Left click / existing gamepad Basic Attack binding | Commit one sword swing |
| Shift / Space | Run / jump |
| F5 | First / third person |
| R | Reset both fighters, resources, and positions |
| Escape | Pause / resume |

The enemy approaches, turns to face the player, commits an attack within range,
then waits briefly after recovery. It stops at obstacles; this open-arena prototype
does not yet find paths around them. Defeating either character ends the round.

Both fighters share `PlaygroundFighter`. A swing crossfades into the imported
Stable Sword Outward Slash, then back into combat locomotion after recovery.
During the swing the animation controls travel through the existing collision motor,
and facing is committed. The active window is 24–52% of the animation. A target must
be within 1.85 metres, inside a 110-degree forward arc, at a similar height, and
unobstructed by the environment. Each victim can take damage once per swing.
This is an animation-timed melee volume, not exact blade collision.

The player uses real PlayerStatsManager health and stamina. ShortSword damage is
copied from its item definition (15); each attack costs 12 stamina. The enemy has
60 health and deals 12 damage. Hits flash the body briefly; lethal hits play the
non-looping Falling Back Death with collision-constrained root travel.
Playground death uses R to reset, rather than the game's save/respawn flow.

All combat, AI, and HUD components live under `_Development`. The only shared
movement addition is an opt-in action motion gate on CharacterMotor; ordinary
locomotion retains its existing behavior. The playground moves its player out of
DontDestroyOnLoad so these scene overrides cannot follow it into Town.

Use **Tools > Player > Build Combat Playground** to regenerate the dedicated scene
and controller from the locomotion test. This replaces those generated playground
assets; it does not replace the original locomotion scene or production controller.
Use **Tools > Validation > Combat Playground (Play Mode)** for the focused checks.

Next: review attack timing, reach, recovery, and first-person presentation by hand.
Once those feel right, add hit reactions and defense, then integrate the selected
combat behavior with equipment and real enemy prefabs. Navigation, combos, and
production combat balance remain separate follow-ups.
