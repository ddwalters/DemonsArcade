# Prototyping yard

Open Prototyping.unity and press Play. The scene starts directly without a save profile.

- Existing player prefab and third-person camera, with the normal movement, jump, sprint and interaction bindings.
- Green box: 0.7 m, below the player's 1.1 m jump height. Orange box: 2.4 m, deliberately out of reach.
- Two sets of eight physical steps, each 0.15 m high and 0.75 m deep, joined by a raised walkway and a doorway.
- Axe and shield pickups, practice blocks, and boundary walls.
- Nine colored armor spheres sit on the west-side floor in three rows: Winter Hat, Winter Coat, Winter Boots; Scout Cap, Scout Coat, Scout Boots; Sun Hat, Sun Coat, Heavy Vest. Walk up and interact with one to add its item definition to the inventory. The sphere is a world-only placeholder; the inventory shows the item's normal icon and stats.
- Winter Gear is Common and gives Cold Resistance on each piece plus Slip Resistance for the 3-piece set. Scout pieces are Uncommon/Rare and give Stamina Recovery or Attack; the 3-piece Scout Gear set adds Attack. Sun Hat is Rare and Sun Coat is Epic, both add Heat Resistance; the 2-piece Sun Ward set adds Fire Resistance. Legendary Heavy Vest has +4 Defense and +1 Attack, with -1 Stamina Recovery.
- Rowan uses the red X Bot humanoid, the player's locomotion Animator and CharacterMotor. A* Seeker calculates routes; CharacterMotor handles translation, turning, grounding and animation. Edit the child transforms under NPC patrol route to change the loop. The navigation grid scans on Play, including edits to the stairs and obstacles.
- Interact with Rowan from in front. The conversation pauses the player and patrol; the current interaction binding advances/skips text. Escape or gamepad east button closes it. The patrol resumes afterward.

Double-click RowanDialogue.asset to edit the conversation in Node Tree. The package is embedded under Packages/com.hiileike.nodetree at upstream revision 703f103811eb543f9338dc130538af8dfb81f952 (0.4.0). Its source is included so the scene does not depend on fetching the repository when opened.

The prototype scene is separate from Town and has not been added to the game's normal scene flow.
