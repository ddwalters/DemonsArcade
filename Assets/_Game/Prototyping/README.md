# Prototyping yard

Open `Prototyping.unity` and press Play. The scene starts directly without a save profile.

- Existing player prefab and third-person camera, with the normal movement, jump, sprint and interaction bindings.
- Green box: 0.7 m, below the player's 1.1 m jump height. Orange box: 2.4 m, deliberately out of reach.
- Two sets of eight physical steps, each 0.15 m high and 0.75 m deep, joined by a raised walkway and a doorway.
- Axe and shield pickups, practice blocks, and boundary walls.
- Rowan uses the red X Bot humanoid, the player's locomotion Animator and CharacterMotor. A* Seeker calculates routes; CharacterMotor handles translation, turning, grounding and animation. Edit the child transforms under `NPC patrol route` to change the loop. The navigation grid scans on Play, including edits to the stairs and obstacles.
- Interact with Rowan from in front. The conversation pauses the player and patrol; the current interaction binding advances/skips text. Escape or gamepad east button closes it. The patrol resumes afterward.

Double-click `RowanDialogue.asset` to edit the conversation in Node Tree. The package is embedded under `Packages/com.hiileike.nodetree`, from https://github.com/ddwalters/Node-Tree-Package at revision `565ba6afe011b2b3499b5e3c4ddfdf3a4c8e84b2` (0.3.1). Its source is included so the scene does not depend on fetching the repository when opened.

The prototype scene is separate from Town and has not been added to the game's normal scene flow.
