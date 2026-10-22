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
Jump accepted -> normal or sprint jump pose sampled from motor velocity -> contact/recovery pose
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

Jump movement stays code-driven, with root motion disabled. After the motor
accepts a jump, `Jumping` or `Running Jump` is selected from the launch gait and
sampled through ascent and descent using the `JumpTime` Animator parameter. The
motor maps its vertical velocity to authored takeoff, apex and contact markers,
so the pose follows the actual ballistic arc rather than playing at a fixed clip
speed. It holds just before the contact pose while airborne. On grounded contact,
the same source clip continues through its recovery segment, then returns to the
walk/run blend. Landing recovery is about 0.18 seconds from idle and 0.10 seconds
while moving; movement remains owned by the motor during this visual recovery.

Jump FBX root translation is extracted and not applied as actor travel. This
keeps the motor as the only owner of vertical and planar movement while the
retargeted clip supplies the body pose. A fall from a ledge enters `Falling Idle`
directly. If a voluntary jump stays airborne beyond its expected arc plus a
short grace period, it also switches to that fall loop; contact then uses the
normal jump clip's contact/recovery segment. `Falling To Landing` is not used by
the current controller graph.

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
Jump phase markers are normalized source-clip times, editable on CharacterMotor
in the Inspector. The current `Jumping` markers (takeoff/apex/contact/recovered)
are approximately 0.281 / 0.421 / 0.596 / 0.842; `Running Jump` uses 0.074 / 0.370
/ 0.704 / 1.0. These initial calibrations come from authored pose markers, not
measured BOTW data. Landing recovery, contact lead, long-jump fall grace and
ground-probe contact tolerance are also Inspector controls. Runtime and Editor
assemblies compile with Unity's cached Roslyn compiler. The revised poses were
captured at 30 fps and visually reviewed in an isolated Unity project, using the
actual Humanoid Animator and motor. All eight focused mechanical checks passed:
idle stability, flat walk/sprint speed, a 0.2 m step, a 30-degree ramp, normal and
sprint jump contact, and a buffered landing jump. Grounded animation remains
stable before takeoff, and no transition lasts longer than five capture frames.

## Review and validation

Use `Assets/_Development/Scenes/PlayerLocomotionTest.unity` for live movement and
the overview camera. Editor tools under Tools / Player reproduce the setup;
Tools / Validation contains asset and Play Mode checks.

Review starts, stops, circles, reversals, analog movement, diagonal speed, wall
contact, steps, slopes, normal/sprint jumps, buffered jumps and movement through
landing recovery. Check that character travel still works with animation
disabled, and that pause, inventory, knockback and save/load retain their
existing behavior. The isolated review covered idle/walk/sprint jumps, ledge
falls and repeated jumps; it does not replace integration checks in every game
scene. Use Tools / Player / Capture Jump Review to reproduce pose sheets and
telemetry without entering Play Mode or changing the open scene. Its output is
under Temp/JumpReview, which Unity may remove when the editor closes.

## Scope

This pass establishes free exploration. Target-facing strafing/backpedaling,
combat, and authored traversal remain later work. Root motion may be chosen for
individual future actions without changing the ownership of ordinary walking
and running.

Inventory equipment data remains. The player has no attack handler or held weapon
model. The unused full-screen RenderTexture overlay remains saved inactive in
GameUI to avoid the previously documented Town blackout.
