# Character movement and animation plan

Status: revised 2026-10-21. The current direction is code-driven third-person free exploration, with animation following movement. Combat and first-person presentation remain deferred.

## Direction

Use code-driven movement for ordinary walking and running. One character motor owns travel, facing, collision, grounding, gravity, jumping and external forces. Input selects camera-relative direction and speed; the motor resolves movement; animation follows actual travel. The motor must work with no Animator. See PlayerLocomotion.md for the current implementation and review plan.

The game is third-person only. Free exploration turns toward travel, including backward input toward the camera. Target-facing strafing is a separate future mode. Match in-place gait playback to travel speed; visual blending must not delay movement. Selected future attacks or traversal actions may use root motion through the same collision owner.

Keep the next review focused on one humanoid character and its locomotion set. Redesign combat after movement and animation feel right.

## What exists now

- The player prefab has a CharacterController, a Humanoid BodyModel and a CharacterMotor. In-place animation follows resolved movement; no animation callback drives locomotion.
- The camera freely orbits independently of the player's facing. Movement is camera-relative and the character turns toward travel. Its 60-degree FOV default is editable on PlayerCameraRig in the Inspector and is not saved with character data. Speed, acceleration, braking and turn speed are motor settings independent of animation playback.
- The recent combat prototype and custom hand-mounted weapon presentation were removed for a later redesign. Inventory equipment data remains, but the player has no attack handler or held weapon model.
- SkeletonAI lets NavMeshAgent move and rotate the actor, derives animation speed from agent velocity, and rotates the Transform separately when facing the target. Several enemy prefabs have Apply Root Motion enabled already. That checkbox alone does not establish a coordinated animation-driven motor; the clips and hierarchy need auditing before conversion.
- KingSlime applies player knockback through CharacterMotor.AddImpulse. Nonhumanoid creatures can retain specialized locomotion where it suits them.

## Optional future action root motion

This section describes a possible future action implementation. It does not drive the current player's walking and running.

An in-place walk changes the pose while code supplies travel. A root-motion walk also contains an authored trajectory. Unity evaluates and blends the clips and exposes the displacement and rotation for that evaluation as Animator.deltaPosition and Animator.deltaRotation.

Handle these in OnAnimatorMove on the Animator's GameObject, forwarding them to the motor on the actor root. Apply the movement once. The model child should not accumulate a separate travel offset, and neither the old Rigidbody movement nor a NavMeshAgent should also write the actor position.

Root deltas are already displacement, not metres per second. Do not multiply deltaPosition by deltaTime again. CharacterController.Move accepts displacement and resolves collisions, but does not apply gravity; vertical velocity must be integrated by the motor. Use an update schedule appropriate to the chosen motor. If retaining a Rigidbody motor, synchronize Animator evaluation and physics movement instead of mixing render-frame Transform writes with physics velocity writes.

Recommended import conventions:

- Humanoid avatars for compatible human-shaped rigs; Generic with an appropriate root node for creatures. Retargeting still needs checking for proportions, feet and weapon grips.
- Consistent world scale, forward direction, grounded height, rig mapping, and root origin across clips. Keep actor/model transforms at unit scale when practical.
- Ground locomotion: bake XZ, Y and rotation into pose for in-place playback; the motor supplies travel and facing.
- Idle: bake XZ and Y into pose to avoid accumulated drift.
- Attacks/dodges: retain intentional XZ movement. Retain root rotation only for actions allowed to own turning. Do not loop these clips.
- Turning clips may intentionally retain rotation. Jump takeoff/flight/landing start with code-driven vertical movement; fully authored traversal is a later exception.
- Check loop boundaries, feet alignment, strafe orientation and measured travel speed. Animation retargeting and root motion do not guarantee zero foot sliding.
- Animator must continue evaluating whenever its root motion drives gameplay, including when the body is off camera. Do not let renderer-based culling silently stop a gameplay motor.

## Shared actor responsibilities

Keep a few focused components instead of a large character framework:

1. Player input or AI steering supplies movement/look intent and action requests.
2. CharacterMotor owns the collision capsule, grounding, gravity, slopes, steps and external motion. Prefer CharacterController for the first prototype because this game needs deliberate character control rather than a freely tumbling physics body. Root motion also works with Rigidbody; this is a project recommendation, not a Unity requirement.
3. Animation maps collision-resolved movement to gait parameters and playback. Future action root motion requires explicit motor ownership; locomotion does not depend on animation callbacks.
4. CharacterCombat owns action choice, stamina checks, phase timing, interruptions, damage windows and per-swing hit tracking. An action definition can hold the animation reference/state, movement policy, timing and damage data.
5. PlayerCamera handles third-person presentation and does not own the actor's attack state or world movement.

Suggested hierarchy:

```text
ActorRoot                 collision motor, stats, combat
  Visual                  Animator, in-place locomotion
    Skeleton / Mesh
    Hand sockets          equipped world weapons
  ViewRoot                stable eye/shoulder reference
    CameraRig             third-person orbit camera
```

The Animator remains on a child imported model. ActorRoot moves independently of the Animator. Player and NPC input can be different sources feeding the same motor. Controllers and clips can differ by rig and character type.

## Implementation sequence and acceptance

### 1. Establish one body and a small compatible motion set

The humanoid body and idle/walk/run set are in place, along with normal and sprint takeoff, falling, and landing clips. Review their transitions and movement feel. The motor selects the jump clip only after accepting a jump; sprint at launch selects the run-jump clip. Descent switches to the falling loop, and ground contact selects the landing clip. Add backward/strafe/turn clips only if play reveals a need. Leave combat animation for its later redesign.

Match animation stride and playback to measured motor speed. Keep the current shoulder camera and tune player movement, collision, and facing together. The jump arc stays code-driven; takeoff and landing clips affect pose only, while the fall state covers ledge drops and the descending part of a jump.

Acceptance: the body travels at the intended scale, loops without drifting, and blends between idle, walk, run, and jump cleanly.

### 2. Replace movement behind the existing gameplay integrations

Build one motor and connect it to the current input, stamina and PlayerModeController gates. Add acceleration/braking, input clamping, robust ground probing, slopes/steps, ground adhesion, modest air control and buffered jumping. Preserve save/load teleporting, inventory/pause/death stops, interactions, and stamina behavior. Replace Rigidbody-only external-force callers with a motor impulse/knockback entry point if using CharacterController.

Use a forward idle/walk/run speed blend for free exploration. Turn toward requested travel without waiting for an animation loop. Derive visual gait from resolved speed so walls suppress running in place. Prove movement remains correct with animation disabled or absent, and prove changing playback speed does not change travel distance.

Acceptance: slopes, stairs, diagonal movement, jumping, walls, knockback, pause and save/load work. Compare walking/stopping feel at different frame rates. Body and collision capsule stay together.

### 3. Tune third-person movement and camera

Keep the game in third person. Tune the orbit distance, shoulder offset, camera collision and body-facing response while testing the locomotion blend. The FOV defaults to 60 degrees and can be edited on PlayerCameraRig in the Inspector. Leave combat animations and weapon placement for a separate redesign.

Acceptance: camera collision keeps the view clear, the camera stays responsive while the body turns, and changing camera settings does not change movement or saved character data.

### 4. Build one complete root-motion sword attack

Use explicit windup, active and recovery phases. Define when movement/turning are allowed, when the next input can be buffered, and which interruptions can cancel an action. Damage is active only during the strike, with each target hit at most once per swing. Prefer swept weapon queries between poses for fast swings rather than relying only on trigger enter events.

Animation events are useful for sound/VFX and initially marking phases, but gameplay must have explicit timing/state and cleanup on interruption, death or unequip. Do not infer combat completion from a clip-name string. Collision, stamina and damage rules stay in gameplay. Limited target-facing assistance can come later; start without target-snapping lunges or complex motion warping.

Acceptance: the feet/body and attack travel agree; walls stop the lunge; windup/recovery do not damage; repeated colliders do not multiply hits; interruption always closes the damage window. Combat remains deferred until its animations and weapon presentation are redesigned.

### 5. Apply the pattern to one skeleton and one NPC

Let NavMeshAgent plan paths and supply steering intent. When the motor owns movement, disable the agent's automatic position/rotation writes, synchronize nextPosition with the resolved actor position, and constrain motion to traversable navigation space. Test avoidance, corners, stopping distances and animation/navigation drift explicitly. Attack phases temporarily own facing and movement as required.

Use shared Humanoid clips where they fit. NPC interactions add their own action choices. Keep slime jumping/rolling or other creature motion specialized rather than forcing everything into the humanoid controller.

Acceptance: the skeleton reaches targets without sliding badly, attacks from sensible distances, stays on navigable ground and resumes navigation after interruption. Then expand weapons, enemies, turns, IK and optional traversal.

## Animation assets and folders

Created alongside existing assets; nothing was moved or renamed:

```text
Assets/_Game/
  Shared/Animations/Humanoid/
    Source/                       project-owned imported animation FBXs
    Clips/
      Locomotion/
      Combat/
      Reactions/
      Interactions/
    Controllers/
    Masks/
  Features/Player/Animations/
    Controllers/
    Clips/Locomotion/
    Overrides/
  Features/Enemies/Animations/
    Clips/Creatures/
    Overrides/
  Features/NPCs/Animations/
    Clips/
    Controllers/
    Overrides/
```

Existing enemy Controllers/Clips and Combat/Animations are retained. Shared full-body clips belong under Shared; player-only view clips belong under Player; creature-specific clips belong under Enemies. Overrides adapt a compatible shared controller to an actor's clip set; use separate controllers for meaningfully different movement, not overrides everywhere. Existing masks remain in place to preserve references. Keep purchased packages under ThirdParty and reference their imported clips rather than copying their entire libraries. Native DCC working files can live outside Assets; use Source for project-owned Unity imports. Remove .gitkeep files when a leaf folder gets its first real asset if desired.

Use descriptive names such as Humanoid_Walk_Forward_RM. RM means exported root travel. Naming is an aid, not a substitute for checking import settings. FBX subclips can stay inside their source file; extraction into separate .anim files is optional.

## Future multiplayer note

No network package or infrastructure is proposed now. Multiplayer will require additional work even with the responsibilities above separated.

Root motion proposes movement on one simulation authority. Remote clients normally display replicated actor movement and animate it; they must not also apply their local animation's root displacement to the same replicated root. Send position/rotation and compact action state such as attack ID, sequence, start tick and direction, rather than sending every bone or trusting each client to independently travel the same distance.

For responsive players, prediction runs local movement immediately; reconciliation corrects it to the server result; interpolation smooths other players. PvP should validate action requests, stamina, movement and damage on the authority, with a deliberate lag-compensation policy. A relayed connection solves connectivity, not these simulation rules.

Stock Animator evaluation is not automatically a rollback-safe simulation. Root motion that affects collision or attack reach must be available to the server and reproducible at simulation ticks. A future implementation could sample animation at ticks, or bake root trajectories and action timings into data that can be replayed while Animator remains the visual layer. This is a future adaptation, not work required for this single-player prototype.

When networking becomes real, compare a host-authoritative small co-op prototype with Unity Netcode for GameObjects and Relay against a prediction-oriented option such as Photon Fusion for more demanding PvP. NGO 3.1 documents anticipation rather than a complete built-in rollback/replay prediction system. Dedicated server authority is preferable when competitive fairness matters; a player-host can still cheat. Defer product choice, server hosting, matchmaking, persistence and dungeon-world synchronization until the desired multiplayer scope is concrete.

## Town blackout finding

GameUI/HUDCanvas/RenderTexture was saved active with a full-screen opaque RawImage referencing an old render texture. Town's PixelationEffect component is disabled and its image reference is empty; the player camera normally renders directly to the display. The GameUI prefab now saves that unused overlay inactive. This addresses a persistent prefab default, not a one-time renderer switch. No camera or URP rendering settings were changed. This finding is from serialized assets and source inspection; the current open Editor state was not inspected or changed.

## References

- [Unity: How Root Motion works](https://docs.unity3d.com/6000.0/Documentation/Manual/RootMotion.html)
- [Unity: OnAnimatorMove](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnAnimatorMove.html)
- [Unity: CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html)
- [AI Navigation: Coupling Animation and Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/CouplingAnimationAndNavigation.html)
- [Netcode for GameObjects 3.1: Client anticipation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@3.1/manual/advanced-topics/client-anticipation.html)
- [Photon Fusion: Animation synchronization](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/animation)
