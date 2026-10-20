# Character movement and animation plan

Status: proposal, 2026-10-20. Gameplay implementation has not started. Only animation folder scaffolding and the inactive default for the unused gameplay RenderTexture overlay were changed. No commits are authorized.

## Direction

Use animation-authored horizontal movement for grounded locomotion and committed combat actions. Route it through one character motor that owns collision-resolved movement. Code controls gravity, grounding, jumping, external forces, and when an action may move or rotate the actor. This is controlled root motion: animation proposes a displacement; gameplay decides how much is allowed.

Root motion is compatible with both camera views, but neither view requires it. The main benefit here is matching visible steps and attacks to movement. It does not automatically fix acceleration, collision handling, foot placement, or responsiveness.

Start with one humanoid character, a small animation set, and one attack. Validate these in both views before expanding the library or converting every enemy.

## What exists now

- PlayerController writes horizontal Rigidbody.linearVelocity directly each physics step. Movement has no acceleration/braking stage. Jumping uses an impulse; grounding is toggled by collision enter/exit with the Ground layer. Looking, movement, stamina, and FOV share this component.
- The player prefab currently has a capsule body and camera-relative Hands/weapon holders, with no full-body humanoid Animator. A suitable rigged body and compatible motion clips are prerequisites.
- PlayerCombatHandler delegates sword attacks to Sword. Sword plays its own Animator, chooses an attack randomly, and polls the idle clip name to end attacks. WeaponCollisionCheck enables damage at attack start. This is separate from body motion and needs explicit attack phases before full-body combat.
- SkeletonAI lets NavMeshAgent move and rotate the actor, derives animation speed from agent velocity, and rotates the Transform separately when facing the target. Several enemy prefabs have Apply Root Motion enabled already. That checkbox alone does not establish a coordinated animation-driven motor; the clips and hierarchy need auditing before conversion.
- Slime bosses have special Rigidbody impulses and apply forces to the player's Rigidbody. These callers must be adapted if the player switches to CharacterController. Nonhumanoid creatures can retain specialized locomotion where it suits them.

## How Unity root motion works

An in-place walk changes the pose while code supplies travel. A root-motion walk also contains an authored trajectory. Unity evaluates and blends the clips and exposes the displacement and rotation for that evaluation as Animator.deltaPosition and Animator.deltaRotation.

Handle these in OnAnimatorMove on the Animator's GameObject, forwarding them to the motor on the actor root. Apply the movement once. The model child should not accumulate a separate travel offset, and neither the old Rigidbody movement nor a NavMeshAgent should also write the actor position.

Root deltas are already displacement, not metres per second. Do not multiply deltaPosition by deltaTime again. CharacterController.Move accepts displacement and resolves collisions, but does not apply gravity; vertical velocity must be integrated by the motor. Use an update schedule appropriate to the chosen motor. If retaining a Rigidbody motor, synchronize Animator evaluation and physics movement instead of mixing render-frame Transform writes with physics velocity writes.

Recommended import conventions:

- Humanoid avatars for compatible human-shaped rigs; Generic with an appropriate root node for creatures. Retargeting still needs checking for proportions, feet and weapon grips.
- Consistent world scale, forward direction, grounded height, rig mapping, and root origin across clips. Keep actor/model transforms at unit scale when practical.
- Ground locomotion: retain XZ root translation, bake Y into pose and use motor gravity. Usually bake rotation for straight/strafe loops so gameplay controls facing.
- Idle: bake XZ and Y into pose to avoid accumulated drift.
- Attacks/dodges: retain intentional XZ movement. Retain root rotation only for actions allowed to own turning. Do not loop these clips.
- Turning clips may intentionally retain rotation. Jump takeoff/flight/landing start with code-driven vertical movement; fully authored traversal is a later exception.
- Check loop boundaries, feet alignment, strafe orientation and measured travel speed. Animation retargeting and root motion do not guarantee zero foot sliding.
- Animator must continue evaluating whenever its root motion drives gameplay, including when the body is hidden in first person or off camera. Do not let renderer-based culling silently stop a gameplay motor.

## Shared actor responsibilities

Keep a few focused components instead of a large character framework:

1. Player input or AI steering supplies movement/look intent and action requests.
2. CharacterMotor owns the collision capsule, grounding, gravity, slopes, steps and external motion. Prefer CharacterController for the first prototype because this game needs deliberate character control rather than a freely tumbling physics body. Root motion also works with Rigidbody; this is a project recommendation, not a Unity requirement.
3. CharacterAnimation maps movement/action state to Animator parameters and forwards root motion to the motor. It coordinates movement locks, turning limits and small speed corrections.
4. CharacterCombat owns action choice, stamina checks, phase timing, interruptions, damage windows and per-swing hit tracking. An action definition can hold the animation reference/state, movement policy, timing and damage data.
5. PlayerCamera handles first/third-person presentation and does not own the actor's attack state or world movement.

Suggested hierarchy:

```text
ActorRoot                 collision motor, stats, combat
  Visual                  Animator + root-motion forwarding component
    Skeleton / Mesh
    Hand sockets          equipped world weapons
  ViewRoot                stable eye/shoulder reference
    CameraRig             first/third-person camera
```

The Animator can remain on a child imported model. Its root-motion callback forwards movement to ActorRoot. Player and NPC input are different sources feeding the same movement/action logic. Controllers and clips can differ by rig and character type.

## Implementation sequence and acceptance

### 1. Establish one body and a small compatible motion set

Choose a humanoid player rig and verify the existing skeleton clips' rig/import compatibility. Minimum useful locomotion: idle, forward walk/run, backward walk and left/right strafe. Add one full-body sword attack, a hit reaction and a death animation. Starts/stops and dedicated turn clips improve quality later; they are not prerequisites for the first working prototype.

Measure clip travel distance and duration. Pick a camera-relative third-person exploration scheme and camera/aim-facing first-person scheme as prototype defaults. Combat can use facing/strafe movement in both views.

Acceptance: the previewed body travels at the intended scale, loops without drifting, and holds its weapon correctly.

### 2. Replace movement behind the existing gameplay integrations

Build one motor and connect it to the current input, stamina and PlayerModeController gates. Add acceleration/braking, input clamping, robust ground probing, slopes/steps, ground adhesion, modest air control and buffered jumping. Preserve save/load teleporting, inventory/pause/death stops, interactions, and stamina behavior. Replace Rigidbody-only external-force callers with a motor impulse/knockback entry point if using CharacterController.

Use directional blend trees for grounded root-motion locomotion. Keep ordinary turn control responsive; do not wait for an entire animation loop before accepting a new direction. Small root-motion corrections can help, but extreme warping will reintroduce foot sliding. Treat collision-shortened movement as normal: a wall prevents a lunge moving through it.

Acceptance: slopes, stairs, diagonal movement, jumping, walls, knockback, pause and save/load work. Compare walking/stopping feel at different frame rates. Body and collision capsule stay together.

### 3. Add the second view before expanding combat

Use one gameplay actor and one authoritative weapon/hit definition for both views. Start with a full-body rig, stable first-person camera reference, and local head visibility control. Do not directly parent the camera to the animated head: running/attacking head motion can make looking unpleasant. Eye pitch stays responsive and separate from full-body attack turns. Third person needs camera collision and deliberate body-facing rules.

Use separate first-person arm poses or presentation clips later if the shared body produces poor weapon framing. Those visuals must follow the same action clock, without generating another set of damage hits or root movement.

Acceptance: switching views does not teleport the player, restart attacks, alter reach, or move the collision capsule. Inspect weapon clipping and looking up/down.

### 4. Build one complete root-motion sword attack

Use explicit windup, active and recovery phases. Define when movement/turning are allowed, when the next input can be buffered, and which interruptions can cancel an action. Damage is active only during the strike, with each target hit at most once per swing. Prefer swept weapon queries between poses for fast swings rather than relying only on trigger enter events.

Animation events are useful for sound/VFX and initially marking phases, but gameplay must have explicit timing/state and cleanup on interruption, death or unequip. Do not infer combat completion from a clip-name string. Collision, stamina and damage rules stay in gameplay. Limited target-facing assistance can come later; start without target-snapping lunges or complex motion warping.

Acceptance: the feet/body and attack travel agree; walls stop the lunge; windup/recovery do not damage; repeated colliders do not multiply hits; interruption always closes the damage window. Both camera views have identical combat outcomes.

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
    Clips/FirstPerson/
    Controllers/
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

Use descriptive names such as Humanoid_Walk_Forward_RM, Humanoid_Sword_Attack01_RM and Player_FP_Sword_Attack01_IP. RM means exported root travel; IP means in-place. Naming is an aid, not a substitute for checking import settings. FBX subclips can stay inside their source file; extraction into separate .anim files is optional.

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
