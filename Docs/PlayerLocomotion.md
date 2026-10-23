# Player locomotion

The player uses code-driven, camera-relative movement. `PlayerController` reads
input and stamina; `CharacterMotor` owns facing, travel, grounding, gravity,
jumping, collision, and external impulses. `PlayerCameraRig` provides the orbit
camera and movement direction. `PlayerModeController` gates gameplay and stops
movement when menus or death take control.

The gameplay prefab is `Assets/_Game/Features/Player/Prefabs/player.prefab`.
Its Animator uses `PlayerLocomotion.controller` with root motion disabled.
Animation follows movement and does not determine travel distance.

## Ground movement and camera

Movement turns the character toward the camera-relative travel direction,
including when pressing backward. There is no backpedal or strafe animation.
The ground blend uses actual forward travel: idle at 0, walk at 1, sprint at 2.
Collision therefore slows the animation when movement is blocked.

| Motor setting | Default |
| --- | --- |
| Walk speed | 2.2 m/s |
| Sprint speed | 5 m/s |
| Acceleration | 30 m/s² |
| Braking | 40 m/s² |
| Turn speed | 720 degrees/s |
| Animation blend time | 0.08 s |

The third-person camera orbits independently of character facing. A sphere cast
pulls it inward around obstructions, then smoothly restores its distance.
The FOV defaults to 60 degrees and remains editable on the camera rig in the
Inspector. There is no view-toggle action or FOV menu setting.

## Jump and landing

The motor accepts buffered and coyote-time jumps and computes the ballistic arc.
Sprinting at takeoff selects the sprint-jump clip. `JumpTime` samples the matching
clip's takeoff, apex, and contact poses from vertical velocity. After contact,
the same clip continues through a short recovery while movement stays responsive.
A ledge fall or unexpectedly long jump enters the falling loop.

Jump root translation is extracted from the imported clips and ignored by the
Animator, preventing it from adding travel on top of the motor's arc.

| Clip | Takeoff | Apex | Contact | Recovered |
| --- | --- | --- | --- | --- |
| Jumping | 0.281 | 0.421 | 0.596 | 0.842 |
| Running Jump | 0.074 | 0.370 | 0.704 | 1.0 |

These normalized markers, jump height, gravity, air acceleration, recovery times,
and contact tolerances are editable on `CharacterMotor` in the Inspector.

## Steps and foot placement

Every `CharacterMotor` gets step handling automatically, so the player and any humanoid NPC
share it. Nothing is wired per prefab. All tuning is in one asset,
`Features/Player/Resources/DefaultHumanoidLocomotionProfile` (assign another profile on a
motor to override it for one character).

- **Stepping:** the motor casts thin rays at ground height to find a riser, reads the tread
  height with a ray beyond it, and lifts the capsule onto the tread when it is within `Max Step
  Height`. Anything taller is a wall. The capsule only moves vertically, and the character
  counts as grounded for a short moment so normal movement carries it over the edge. It also
  snaps down onto lower steps instead of falling. Steps are found along the input direction, so angled approaches work.
  A small probe around the capsule's center detects an unsupported rim, while lower treads within
  `Max Step Height` remain valid. If the center is over open air, the motor nudges the character away
  from the remaining support at a minimum escape speed. The escape direction stays steady while the
  character clears the rim, and walking back toward support cancels the correction. Forced escape
  motion does not trigger a step-up attempt.
- **Smooth path:** the capsule still moves in steps, but the model and camera do not follow
  them. `StepSmoothing` averages the real ground height a short distance ahead of and behind
  the character along its travel direction (`Step Smoothing Distance`), which turns stairs into
  a smooth ramp that leads the capsule instead of lagging it. The effect fades out at low speed
  so standing on stairs does not lower the body. The camera follows the same height with a
  little extra smoothing (`Camera Ease Time`).
- **Feet:** `FootPlacementIK` runs in `OnAnimatorIK` (the Locomotion layer has IK Pass on).
  Each foot probes the ground under itself independently, so one foot can rest on a higher
  step than the other. A stepping foot looks ahead and clears the highest step in range. The
  hips follow part of the lower foot's drop and the knees are pushed forward.
## Retained animation assets

The Humanoid clip library contains only `CalmIdle`, `Walking`, `Running`,
`Jumping`, `Running Jump`, and `Falling Idle`. The current controller and imports
are checked-in assets; the one-off setup, showcase, and capture tools were removed.
Enemy animations remain separate from this player setup.

Inventory equipment data remains, but player attacks and held-weapon presentation
are reserved for a later combat design. KingSlime knockback continues through
`CharacterMotor.AddImpulse`, preserving the motor's ownership of displacement.
