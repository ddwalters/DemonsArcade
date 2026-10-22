# Player locomotion and camera

## Design

Free exploration uses code-driven movement inspired by the responsiveness of
Breath of the Wild. This is a design choice for this project, not a claim about
Nintendo's implementation. The movement motor owns travel, facing, collision,
gravity and jumping. Animation follows the resolved movement and never determines
walking or running speed.

The player prefab is `Assets/_Game/Features/Player/Prefabs/player.prefab`.

```text
Input + camera yaw
  -> movement direction and desired speed
  -> CharacterMotor / CharacterController collision
  -> resolved movement
  -> idle / walk / run animation while grounded
Jump accepted -> normal or sprint takeoff -> fall on descent -> landing on contact
```

The motor must work without an Animator. Changing animation playback speed must
not change travel distance. The model remains attached to the collision capsule.
The motor owns the jump arc. The Animator receives jump state only after the motor
accepts the request; sprinting at takeoff selects the run-jump clip. Falling from
a ledge skips the takeoff clip, then shares the airborne and landing states.

## Movement and camera

Movement is relative to the camera's horizontal direction. The character turns
toward travel, including when moving toward the camera. Pressing backward turns
the character; it does not select a backpedal animation. Mouse/right-stick input
orbits the camera independently of character facing. Sprint works in any direction.

Analog input controls speed; diagonal input is clamped. Acceleration, braking and
turning have independent motor settings so animation blending cannot delay a
movement request. Collision-resolved speed drives the forward gait, so pressing
against a wall does not sustain a full-speed running animation.

The motor handles ground probing, walkable slopes, steps, buffered jumping,
coyote time, air steering and external impulses. Menus and death stop player
locomotion. Teleporting clears stale motion before moving the collision capsule.

The Animator uses `Jumping` and `Running Jump` as one-shot takeoff clips,
`Falling Idle` while descending, and `Falling To Landing` when grounded again.
Root translation remains baked out so CharacterMotor stays the movement owner.

The existing third-person camera keeps its shoulder offset and obstruction
handling. Its 60-degree FOV default is editable on PlayerCameraRig in the
Inspector and is not saved with character data. Camera yaw does not change when
the body turns.

- WASD / left stick: camera-relative movement
- Mouse / right stick: orbit
- Shift / sprint binding: sprint
- Space / jump binding: jump
- Tab: inventory
- Escape: pause

## Tuning

Tune movement in the player's CharacterMotor: walk/sprint speeds, acceleration,
braking, turning, jump height, gravity and air steering. Animation settings tune
visual stride and blending independently. Match the in-place walk and run cycles
to measured travel speed before adding more animation polish.

Initial motor settings:

| Setting | Value |
| --- | --- |
| Walk speed | 2.2 m/s |
| Sprint speed | 5 m/s |
| Acceleration | 30 m/s² |
| Braking | 40 m/s² |
| Turn speed | 720 degrees/s |
| Visual blend time | 0.08 s |

These are starting values for play review, not measured BOTW values. Changing
them changes motor response; changing clip playback does not change travel.

The checked-in walk/run clips use an initial playback scale of 1.3. The explicit
Rebuild Locomotion Setup command measures authored clip travel before baking it
in place and recalculates gait playback for the configured baseline speeds.
That calibration and visual foot-slide review have not been run in this pass:
the project was already open in Unity, preventing a separate batch Editor from
running. Runtime and Editor C# were compiled separately using Unity's cached
compiler settings; the new Play Mode checks still need an Editor run.

## Review and validation

Use `Assets/_Development/Scenes/PlayerLocomotionTest.unity` for live movement and
the overview camera. Editor tools under Tools / Player reproduce the setup;
Tools / Validation contains asset and Play Mode checks.

Review starts, stops, circles, reversals, analog movement, diagonal speed, wall
contact, steps, slopes and jumping. Check that character travel still works with
animation disabled, and that pause, inventory, knockback and save/load retain
their existing behavior. Automated checks verify mechanics; movement feel and
foot sliding still require a visual play review.

## Scope

This pass establishes free exploration. Target-facing strafing/backpedaling,
combat, and authored traversal remain later work. Root motion may be chosen for
individual future actions without changing the ownership of ordinary walking
and running.

Inventory equipment data remains. The player has no attack handler or held weapon
model. The unused full-screen RenderTexture overlay remains saved inactive in
GameUI to avoid the previously documented Town blackout.
