# Game Design

## Feature documentation

Each feature has a Feature.md file at its root, split into Current, Agreed, and Unfinished sections.

- Current: what is implemented, including current values, controls, and layouts.
- Agreed: decisions made for the feature.
- Unfinished: missing behavior and known problems.

Temporary work does not automatically become an agreed design choice. Update feature documentation when feature work is completed. Update GameDesign.md when an agreed project-wide rule changes.

## Folders and structure

Game-owned content is under Assets/_Game.

Development tools that do not go into the game belong under Assets/_Development.

- Features/<Feature>: gameplay scripts, definitions, and gameplay prefabs.
- UI/<Area>: UI prefabs, presentation scripts, and artwork.
- World: environment assets and world objects.
- Scenes: game scenes.
- Prototyping: the test scene and content specific to its layout.
- Shared: assets used across multiple features.
- Settings: project-owned configuration and input assets.
- Editor: editor tools that apply across the project.

Third-party assets and packages remain separate from game-owned content.

Features belong to the game regardless of which scene currently uses them. Do not create separate prototype versions of gameplay systems.

## Prefabs, artwork, and Inspector editing

Artwork must be replaceable without rewriting gameplay.

Models, portraits, icons, materials, and UI artwork are assigned through editable references. Placeholder art uses the same gameplay systems as replacement art.

Use prefabs for reusable objects and editable visual layouts.

Code handles behavior, data updates, and visibility. Author fixed visual structure, styling, and positioning in prefabs rather than rebuilding them through scripts.

Expose useful tuning values in the Inspector. Avoid hardcoding values that are expected to be adjusted during development.

## Game hierarchy and UI

Keep important components and fixed UI visible in the authored hierarchy.

GameUI contains the gameplay UI, including:

- HUD
- Inventory
- Dialogue
- Menus
- Overlays and confirmation panels

Fixed panels are prefab instances already placed in the hierarchy. Opening and closing them normally shows or hides those instances.

Variable entries can be instantiated from prefabs, including:

- Inventory items
- Party member rows and tabs
- Dialogue choices
- Save entries

UI displays data and requests actions from gameplay systems. It should not maintain its own copy of gameplay state.

## Scenes and object lifetimes

Scenes place and configure reusable game features. All feature work builds toward the actual game.

The Prototyping scene uses the same player, NPC, interaction, inventory, dialogue, and UI systems as the game. Special scenes can opt out of feature behavior when needed. Those opt-outs must be configurable within the feature rather than hardcoded to a scene.

Scene-specific layouts and asset assignments are expected. They do not require separate implementations of the features placed there.

Scene names and boundaries can change while the game is in early development. Record the current setup in the scene-flow feature documentation.

Keep persistent session data separate from objects that belong to a scene. Do not make objects persistent simply to prevent their data from being lost.

Scene loading should have one clear path rather than several competing implementations.

Saving restrictions for the main game are not decided here.

## Characters and party

Game character definitions link to an existing Node Tree character.

- Node Tree has dialogue identity, names, and portraits.
- The game definition has game-specific character configuration.
- Each game definition must select its matching Node Tree character.
- Avoid duplicating names and portraits in game definitions.

Party portraits use the linked character's default portrait. Dialogue can use that character's mood portraits.

The party tracks membership, order, and leader. Companion movement and AI are separate from party management.

Characters have individual inventories and equipment. Inventory tabs only appear for characters currently in the party.

### Runtime character records

Keep one runtime record per character.

The record holds their current data. A scene character object connects to that record rather than becoming the only place the data exists.

Removing a companion from the party or unloading their scene object does not erase their belongings.

Gameplay systems update the record; UI reads it. Do not keep competing copies of the same data.

## Movement and camera

Movement is driven by code, with animation presenting that movement.

The character turns toward the movement direction rather than walking backward during normal movement.

Keep the character motor reusable by player and NPC controllers. Player input and NPC navigation supply movement intent.

Keep animation, foot placement, step presentation, and camera behavior separate from the movement rules.

Camera tuning values are editable in the Inspector. Third-person FOV is not a player-facing setting. Record current values in the feature documentation.

## Interaction and dialogue

Choose the nearest eligible interactable in front of the character.

Show the interaction action and its current input binding in the screen UI. Show the object's or character's name above it in world space.

The player's head looks slightly toward the interaction target without turning their whole body.

NPCs can turn toward the player and look at their face during conversation. Ordinary interactables, such as signs, do not turn toward the player.

Dialogue uses Node Tree and the shared GameUI.

Portrait placement follows the selected side. Portraits sit inside the dialogue box, with space reserved for text and response choices.

Node Tree handles which sentences can be revisited. Backtracking must not undo a dialogue choice.

Use the shared player mode system to prevent dialogue input from also triggering movement, attacks, or inventory actions. Record exact input bindings in the feature documentation.

## Inventory and equipment

The inventory includes the selected character's appearance, equipment, and bag.

Inventory dimensions are configurable in the Inspector. Record current dimensions, input bindings, and equipment slot arrangements in the feature documentation.

Item definitions describe an item. Owned item instances hold information specific to that copy. UI objects display those instances.

Equipment behavior must remain independent of its artwork. Replacing an equipment model should not change how equipping it works.

## Package boundaries

Keep Node Tree useful as a general dialogue package.

Game-specific character data and behavior belong in the game. Connect them through the required character link.

Prefer game-side integration over adding game-specific rules to the package.

Any necessary changes to the embedded package must be identifiable so they can be preserved or replaced when updating it.
