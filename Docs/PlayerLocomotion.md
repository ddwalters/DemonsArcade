# Player locomotion prototype

This pass implements the imported player body, basic locomotion and first/third-person camera switching. Full-body combat, NPCs and enemy animation conversion remain later steps.

## Asset setup

The test body is `Assets/_Game/Features/Player/Male_yBot_Testing.fbx`. The alternative xBot remains untouched. The selected body and locomotion FBXs are imported as Humanoid with their own valid avatars so Unity can retarget motion onto the player.

The player controller asset is `Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller`. Grounded movement uses a directional blend tree:

| Input | Clip |
| --- | --- |
| Rest | CalmIdle |
| Forward | Walking |
| Backward | Walking Backwards |
| Left/right | Left Strafe Walking / Right Strafe Walking |
| Forward sprint | Running |
| Airborne | Falling Idle |

Idle and airborne clips have root travel baked into the pose. Walking/running retain horizontal root translation; vertical root motion and rotation are baked. Code controls facing and vertical motion. The other imported actions are configured as Humanoid for retargeted previews; their gameplay timing and movement policies are not yet connected.

The imported test body receives project-owned URP materials to avoid incompatible imported shaders.

## Runtime responsibilities

- `PlayerController` keeps input and sprint stamina. It supplies look input to the camera rig, and movement intent and jump requests to the motor. It retains the entry points used by saves, menus, settings and HUD code.
- `PlayerCameraRig` owns pitch, per-view FOV, view transitions, camera obstruction and local body/weapon visibility. Looking turns the player in both views; movement therefore remains forward/backward/strafe relative to facing. The camera does not freely orbit an independently facing character in this pass.
- `CharacterMotor` owns the CharacterController capsule and motion. It smooths animation input, probes the ground, handles gravity/jumps, projects movement onto walkable slopes, resolves walls/steps through CharacterController.Move, and applies explicit knockback.
- `CharacterRootMotion` lives beside the body's Animator. OnAnimatorMove forwards the frame's animation displacement to the motor. The motor applies it once; no Rigidbody or navigation component also moves the player.

Walking speed comes from the clip's authored travel and its blend weight, rather than the old `walkSpeed` velocity assignment. Sprint blends toward the running clip. The motor eases the two movement parameters with SmoothDamp: `movementBlendTime` defaults to 0.25 seconds and `stoppingBlendTime` to 0.14 seconds. These are response settings, not animation durations. Air steering has its own speed and acceleration because the airborne clip supplies no ground travel.

The ground tree uses Freeform Directional blending, which supports directional walking plus running along the same forward axis. Locomotion cycle offsets are aligned against the forward walk using sampled left/right foot-height phases, reducing opposing foot phases during direction blends. `Tools/Player/Refine Locomotion Blending` reapplies this baseline without rebuilding the controller or replacing the body. The blend tests check actual simultaneous clip weights during idle/walk and walk/run transitions.

Gravity and jump height are motor settings. Short jump buffering and coyote time make button timing forgiving. Jump uses the available falling pose for now; authored takeoff/landing sequences are deferred.

`StopMotion` clears movement/knockback/jump requests. Menus and death suppress horizontal movement; normal gravity still applies when game time advances. Pausing freezes time through the existing mode controller. `RestoreLocation` temporarily disables the collision capsule while teleporting so save/load and scene transitions can reposition safely.

The slime boss now applies knockback through CharacterMotor.AddImpulse instead of assuming the player has a Rigidbody. Other character types are unchanged.

## Prefab and camera

The existing `player.prefab` remains the source used by Town and Dungeon. Its old visible capsule, CapsuleCollider and dynamic Rigidbody are replaced by a CharacterController plus the imported `BodyModel` child. The root's identity and existing gameplay components remain in place. The model's feet are offset to the capsule bottom; the camera stays independent of the animated head.

The existing `PlayerCamera` and its AudioListener remain the single gameplay camera. `ViewPivot` is a new child at the previous eye location; both the camera and legacy `Hands` rig are children of that pivot. Pitch rotates the pivot. The camera pulls back in third person while Hands stays at the eye, preserving existing weapon travel and hitbox reach. The camera stays independent of the animated head.

Press **F5** or **right-stick click** to switch views, or choose **Camera view** in Settings from the main menu or pause menu. The Settings selector refreshes on opening and uses the same local preference as the hotkey. First person hides the local body through the camera's Player-layer mask. Third person shows the body and hides the legacy first-person weapon renderers without disabling their gameplay objects. Newly equipped weapons refresh that visibility. Full-body weapon attachment/attack presentation is still part of the upcoming combat pass.

Third person defaults to a slight right shoulder offset, 3.25 metres behind the pivot. View changes ease over time. A sphere probe retracts the camera immediately around solid geometry, including the floor; it eases back after clearance. An obstructed pivot falls back to the eye position. If clearance puts the camera inside the body, the body is hidden before it can fill the view. Trigger volumes and the Player/UI/Ignore Raycast layers do not obstruct the camera. Third-person pitch is limited to 60 degrees up and 70 down; first person retains the existing 89-degree limits. Settings provides separate first-person and third-person FOV sliders (60–110 degrees, default 80). Sprinting never changes FOV. Active-view slider changes apply immediately, including while paused; the other value is used on switching views. Values are local preferences (`Settings.FirstPersonFov` and `Settings.ThirdPersonFov`), retained across save/load and independent of character saves. Sensitivity still applies to the same camera.

View switching is blocked while inventory, pause, loot or death controls own input. The choice is stored locally as `Settings.ThirdPerson`, alongside the existing local settings, so save/load and new player instances retain it. It is not part of a character save. Teleport/save restoration resets pitch and camera placement against the new geometry.

Interaction still aims from the gameplay camera, but its five-metre reach is checked from the player's eye pivot. Moving the camera back cannot extend reach. An obstruction between the player and the selected object blocks interaction.

## Reusable animation test scene

Open `Assets/_Development/Scenes/PlayerLocomotionTest.unity` and press Play. It contains the actual player prefab, an external follow camera, a grid floor, a wall, a step platform, a slope, and labeled development controls. This scene is deliberately excluded from the game's build scene list.

Live movement uses the normal player input and actual root-motion motor. Escape releases the mouse so the development buttons can be clicked. Live movement/reset returns to ordinary controls. Walk/run demo buttons drive the same motor automatically. Previous/next clip buttons preview all 34 imported animations on yBot, with the existing sword mesh shown for the relevant weapon poses.

The **Camera: overview / player** button selects the development follow camera or the actual gameplay camera. In player view, use F5 to compare first/third person. Only one camera and AudioListener are enabled at a time. The animation showcase and individual clip previews return to the overview automatically.

Pose previews intentionally suppress world movement and damage. Their purpose is to review retargeting and pose quality. They do not implement root-motion combat, bow combat, death behavior, or door interactions. The caption distinguishes pose previews from live locomotion. Returning to live movement restores the original locomotion controller and motor.

The test-only script is `Assets/_Development/Prototypes/PlayerAnimationTest.cs`. Scene generation lives in `PlayerAnimationSceneSetup.cs`; the normal player prefab does not include this development interface or camera.

## Testing controls

Existing bindings remain: movement, look, sprint, jump, inventory and pause. F5/right-stick click toggles the view. Sprint now consumes stamina only when moving forward. Idle/walking/running can be inspected in the Animator parameters MoveX/MoveY; Grounded selects the ground/air state.

Useful Inspector settings on the player:

- CharacterMotor: movement/stopping blend response times, jump height, gravity, ground probe, air control and knockback braking.
- CharacterController: height/radius, slope limit, step offset and skin width.
- PlayerController: sensitivity and sprint stamina rate.
- PlayerCameraRig: third-person offset, transition/clearance return times, collision probe radius/padding and obstruction mask.

`Tools/Validation/Player Locomotion Assets` checks the saved prefab connections. `Tools/Validation/Player Locomotion (Play Mode)` uses an isolated temporary scene and character to exercise movement/collision without touching real saves. The setup command `Tools/Player/Rebuild Locomotion Setup` is an explicit asset-generation command; use it only when intentionally regenerating the baseline controller/body setup, since it replaces the generated controller states and body child.

`Tools/Validation/First and Third Person Cameras` checks actual F5 input, mode gating, visibility, wall/ground clearance, camera recovery, teleport and interaction reach/occlusion in the development scene. Camera checks restore the previous local view preference and Input System settings. Save/menu checks also verify Town display output and that save/load retains the local view preference.

Latest regression validation: 33 camera checks, 11 prefab checks, 58 player-feature checks, 42 save-data checks and 48 save/menu Play Mode checks passed (192 total). This includes real sprint input preserving each view's FOV, separate sliders from both Settings screens, immediate paused changes, and save/load persistence. The first/third-person baseline also passed a Windows build of the three enabled game scenes with 0 errors and the existing 16 warnings; the FOV follow-up was compiled and checked in Unity Play Mode. Temporary build and recording files were removed. Review images and validation summaries are under `Logs/PlayerCameraReview/` outside version control.

## Review boundaries and next steps

Validation completed: 11 saved-asset checks, 20 locomotion Play Mode checks, 58 player-feature checks, 42 save-data checks, and 36 save/menu Play Mode checks passed. The normal Windows build succeeded with 0 errors and 16 warnings. Temporary validation builds, frame sequences and recording scripts were removed after recording; the reusable scene, setup commands and regression checks remain.

Review recordings are saved locally under `Logs/PlayerAnimationReview/`: `Locomotion.mp4` (21 seconds), `ImportedAnimations.mp4` (34 labeled/chaptered previews, about 91 seconds), and `IndividualAnimationClips.zip` (one short video per imported animation). These media files are outside version control. Pose-preview root travel is held in place; the locomotion video demonstrates actual capsule movement.

The revised blending review is `SmoothBlending.mp4`: a 30-second continuous take with gradual speed/direction sweeps and the real clip contributions displayed. It uses the directional tree, eased motor parameters and aligned gait offsets described above. The latest pass has 134 successful checks: 11 saved-asset, 23 movement/blending, 58 player-feature and 42 save-data checks. The recording confirms mixed clips in 659 of its 900 frames without restarting the locomotion state. Evidence is in `Logs/PlayerAnimationReview/BlendValidation.txt`.

The extended review is `MovementJumpsAndDeaths.mp4` (about 71 seconds). In the test scene, **Movement + death showcase** replays it: eased walk/run/direction blends, running/standing/backward jumps, walk start/stop and a walking turn, a step-down, a staged fall and landing, sword draw/run/jump attack/slash/sheathing, then all three death reactions. Actions crossfade from the preceding state. Deaths do not loop; they hold their terminal pose. Brief fades separate scenario resets.

This choreography uses the separate development-only `AnimationShowcase.controller`, `PlayerAnimationShowcase` and `ShowcaseRootMotion`. The normal gameplay controller/prefab does not reference them. Grounded locomotion still uses the real motor. Complete authored action clips retain horizontal root travel constrained by the capsule, with vertical motion baked into their pose for review. The separate staged fall uses development-only gravity and capsule contact to cue the landing. The white step is a visual reference for the imported step-down clip, with collision disabled. This does **not** wire authored jump timing, combat damage, or animated death/respawn into the game. Those remain reviewable future work; the live Space jump still uses the motor's existing physical arc and falling pose.

`Tools/Player/Configure Animation Showcase` rebuilds the test controller and connects the test scene. It changes the action-clip import settings to non-looping (except combat idle and sword run), bakes vertical travel into the pose, and retains horizontal travel. Walking turns retain root rotation. `Build Animation Test Scene` also configures the showcase. These are explicit commands, never automatic scene mutations.

The legacy first-person sword animations and damage behavior are retained, not converted to the full body. There is no networking, foot IK, motion warping, gameplay start/stop animation, authored gameplay jump takeoff/landing or root-motion combat yet.

After reviewing the movement foundation:

1. Review first/third-person view feel in Town and the test scene; tune camera offsets and response settings if needed.
2. Build one full-body sword attack with windup/active/recovery phases, collision-constrained root travel, stamina, input buffering and per-swing hit tracking.
3. Integrate one skeleton and one NPC with the shared movement/action pattern, coordinating navigation with root motion.
4. Expand and polish the animation set once those examples feel good.
